using System.Collections.Generic;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 유닛에게 피해를 주는 히트박스.
    /// 충돌 시 Owner·진영은 <see cref="SurvivorsRunCombat"/>이 최종 판정한다.
    /// 동일 대상은 <see cref="_hitCooldown"/> 동안 재타격하지 않는다.
    /// </summary>
    public class SurvivorsRunDamageObject : MonoBehaviour
    {
        [SerializeField] private int _damage = 1;
        [SerializeField] private float _hitCooldown = 0.5f;
        [SerializeField] private bool _damageEnabled = true;

        private SurvivorsRunUnitBase _owner;
        private readonly Dictionary<SurvivorsRunUnitBase, float> _hitCooldowns = new();
        private readonly List<SurvivorsRunUnitBase> _pruneBuffer = new();

        public SurvivorsRunUnitBase Owner => _owner;
        public int Damage => _damage;
        public float HitCooldown => _hitCooldown;
        public bool IsDamageEnabled => _damageEnabled;

        public void Setup(SurvivorsRunUnitBase owner, int damage, float hitCooldown)
        {
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

        private static SurvivorsRunUnitBase ResolveUnit(Collider2D other)
        {
            var unit = other.GetComponent<SurvivorsRunUnitBase>();
            if (unit != null)
                return unit;

            return other.GetComponentInParent<SurvivorsRunUnitBase>();
        }
    }
}
