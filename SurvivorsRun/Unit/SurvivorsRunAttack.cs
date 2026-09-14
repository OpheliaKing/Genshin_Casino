using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 유닛 공격 실행기 베이스.
    /// 공격 판정이 나면 <see cref="SurvivorsRunCombat"/>으로 피해 적용을 위임한다.
    /// </summary>
    [RequireComponent(typeof(SurvivorsRunUnitBase))]
    public class SurvivorsRunAttack : MonoBehaviour
    {
        private SurvivorsRunUnitBase _owner;
        private float _cooldownRemaining;
        private bool _attackEnabled = true;

        public SurvivorsRunUnitBase Owner => _owner;
        public bool IsAttackEnabled => _attackEnabled;
        public bool IsOnCooldown => _cooldownRemaining > 0f;
        public float CooldownRemaining => _cooldownRemaining;

        private void Awake()
        {
            if (_owner == null)
                _owner = GetComponent<SurvivorsRunUnitBase>();
        }

        public void BindOwner(SurvivorsRunUnitBase owner)
        {
            _owner = owner != null ? owner : GetComponent<SurvivorsRunUnitBase>();
        }

        public void SetAttackEnabled(bool enabled)
        {
            _attackEnabled = enabled;
        }

        public void ResetCooldown()
        {
            _cooldownRemaining = 0f;
        }

        protected virtual void Update()
        {
            if (_cooldownRemaining <= 0f)
                return;

            var scale = _owner != null && _owner.Manager != null
                ? _owner.Manager.TimeScale
                : 1f;
            _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - Time.deltaTime * scale);
        }

        /// <summary>
        /// 공격 가능 여부(활성·생존·쿨다운·타겟·Combat 연결).
        /// 진영 규칙은 Combat에서 최종 판단한다.
        /// </summary>
        public virtual bool CanAttack(SurvivorsRunUnitBase target)
        {
            if (!_attackEnabled || _owner == null || _owner.IsDead)
                return false;

            if (IsOnCooldown)
                return false;

            if (target == null || target.IsDead || target == _owner)
                return false;

            if (_owner.Manager?.Combat == null)
                return false;

            return true;
        }

        /// <summary>
        /// 공격 판정 진입점. damage 미지정 시 Owner 공격력을 쓴다.
        /// </summary>
        public bool TryAttack(SurvivorsRunUnitBase target)
        {
            if (_owner == null)
                return false;

            return TryAttack(target, _owner.Attack);
        }

        /// <summary>
        /// 실제 공격 처리. CanAttack → Combat → 쿨다운 → 히트 훅.
        /// </summary>
        public bool TryAttack(SurvivorsRunUnitBase target, int damage)
        {
            if (!CanAttack(target))
                return false;

            if (!_owner.Manager.Combat.TryApplyDamage(target, damage, _owner))
                return false;

            BeginCooldown();
            OnAttackHit(target);
            return true;
        }

        protected void BeginCooldown()
        {
            var attackSpeed = _owner != null ? _owner.AttackSpeed : 1f;
            _cooldownRemaining = attackSpeed > 0f ? 1f / attackSpeed : 0f;
        }

        /// <summary>피해 적용 성공 후 훅(이펙트·사운드 등).</summary>
        protected virtual void OnAttackHit(SurvivorsRunUnitBase target)
        {
        }
    }

    public enum SURVIVORSRUN_ATTACK_TYPEP
    {
        NONE,
        RANGE,
        MELEE,
    }
}
