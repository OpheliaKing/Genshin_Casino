using System.Collections.Generic;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 유닛에게 피해를 주는 히트박스.
    /// 피격 대상은 <see cref="PublicVariable.Layer.Unit"/> 레이어 콜라이더만 인정한다.
    /// (공격용 DamageObject 콜라이더는 Unit이 아니어야 피격 판정과 분리된다.)
    /// 충돌 시 Owner·진영은 <see cref="SurvivorsRunCombat"/>이 최종 판정한다.
    /// 동일 대상은 <see cref="_hitCooldown"/> 동안 재타격하지 않는다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class SurvivorsRunDamageObject : MonoBehaviour
    {
        private const float DefaultColliderRadius = 0.4f;

        [SerializeField] private int _damage = 1;
        [SerializeField] private float _hitCooldown = 0.5f;
        [SerializeField] private bool _damageEnabled = true;

        private SurvivorsRunUnitBase _owner;
        private readonly Dictionary<SurvivorsRunUnitBase, float> _hitCooldowns = new();
        private readonly List<SurvivorsRunUnitBase> _pruneBuffer = new();

        private static int _unitLayer = int.MinValue;
        private static bool _unitLayerMissingLogged;

        public SurvivorsRunUnitBase Owner => _owner;
        public int Damage => _damage;
        public float HitCooldown => _hitCooldown;
        public bool IsDamageEnabled => _damageEnabled;

        private void Reset()
        {
            EnsurePhysicsComponents();
        }

        private void Awake()
        {
            EnsurePhysicsComponents();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsurePhysicsComponents();
        }
#endif

        /// <summary>
        /// DamageObject 추가 시 Rigidbody2D·CircleCollider2D가 없으면 붙이고,
        /// 트리거/Kinematic 기본값을 맞춘다.
        /// </summary>
        public void EnsurePhysicsComponents()
        {
            var body = GetComponent<Rigidbody2D>();
            if (body == null)
                body = gameObject.AddComponent<Rigidbody2D>();

            body.bodyType = RigidbodyType2D.Kinematic;
            body.simulated = true;
            body.useFullKinematicContacts = true;
            body.gravityScale = 0f;

            var col = GetComponent<CircleCollider2D>();
            if (col == null)
                col = gameObject.AddComponent<CircleCollider2D>();

            col.isTrigger = true;
            if (col.radius <= 0f)
                col.radius = DefaultColliderRadius;
        }

        public void Setup(SurvivorsRunUnitBase owner, int damage, float hitCooldown)
        {
            EnsurePhysicsComponents();
            _owner = owner;
            _damage = Mathf.Max(0, damage);
            _hitCooldown = Mathf.Max(0f, hitCooldown);
            _hitCooldowns.Clear();
        }

        public void SetDamageEnabled(bool enabled)
        {
            _damageEnabled = enabled;
            if (!enabled)
                _hitCooldowns.Clear();
        }

        public void SetDamage(int damage)
        {
            _damage = Mathf.Max(0, damage);
        }

        public void SetHitCooldown(float hitCooldown)
        {
            _hitCooldown = Mathf.Max(0f, hitCooldown);
        }

        private void Update()
        {
            if (_hitCooldowns.Count == 0)
                return;

            var dt = GetDeltaTime();
            _pruneBuffer.Clear();
            _pruneBuffer.AddRange(_hitCooldowns.Keys);

            for (var i = 0; i < _pruneBuffer.Count; i++)
            {
                var unit = _pruneBuffer[i];
                if (unit == null || unit.IsDead || !_hitCooldowns.TryGetValue(unit, out var remaining))
                {
                    _hitCooldowns.Remove(unit);
                    continue;
                }

                remaining -= dt;
                if (remaining <= 0f)
                    _hitCooldowns.Remove(unit);
                else
                    _hitCooldowns[unit] = remaining;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryHitCollider(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryHitCollider(other);
        }

        private void TryHitCollider(Collider2D other)
        {
            if (!_damageEnabled || other == null || _damage <= 0)
                return;

            if (_owner == null || _owner.IsDead)
                return;

            // 피격 판정(Unit)만 인정. 공격용 히트박스·이펙트 콜라이더는 제외.
            if (!IsUnitHurtbox(other))
                return;

            var target = ResolveUnit(other);
            if (target == null || target.IsDead || target == _owner)
                return;

            if (_hitCooldowns.ContainsKey(target))
                return;

            var combat = _owner.Manager?.Combat;
            if (combat == null)
                return;

            if (!combat.TryApplyDamage(target, _damage, _owner))
                return;

            if (_hitCooldown > 0f)
                _hitCooldowns[target] = _hitCooldown;
        }

        private float GetDeltaTime()
        {
            var scale = _owner != null && _owner.Manager != null
                ? _owner.Manager.TimeScale
                : 1f;
            return Time.deltaTime * scale;
        }

        private static bool IsUnitHurtbox(Collider2D other)
        {
            var unitLayer = GetUnitLayer();
            if (unitLayer < 0)
                return false;

            return other.gameObject.layer == unitLayer;
        }

        private static int GetUnitLayer()
        {
            if (_unitLayer != int.MinValue)
                return _unitLayer;

            _unitLayer = LayerMask.NameToLayer(PublicVariable.Layer.Unit);
            if (_unitLayer < 0 && !_unitLayerMissingLogged)
            {
                _unitLayerMissingLogged = true;
                Debug.LogError(
                    $"[SurvivorsRunDamageObject] '{PublicVariable.Layer.Unit}' 레이어가 없습니다. TagManager를 확인하세요.");
            }

            return _unitLayer;
        }

        private static SurvivorsRunUnitBase ResolveUnit(Collider2D other)
        {
            var unit = other.GetComponent<SurvivorsRunUnitBase>();
            if (unit != null)
                return unit;

            return other.GetComponentInParent<SurvivorsRunUnitBase>();
        }
    }
}
