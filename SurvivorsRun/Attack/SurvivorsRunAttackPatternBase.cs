using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 공격 패턴 실행 베이스.
    /// 피해 적용은 항상 <see cref="SurvivorsRunAttack"/> → Combat으로 위임한다.
    /// </summary>
    [RequireComponent(typeof(SurvivorsRunUnitBase))]
    [RequireComponent(typeof(SurvivorsRunAttack))]
    public abstract class SurvivorsRunAttackPatternBase : MonoBehaviour
    {
        private SurvivorsRunUnitBase _owner;
        private SurvivorsRunAttack _attack;
        private bool _patternEnabled = true;

        public abstract SURVIVORSRUN_ATTACK_PATTERN Pattern { get; }

        protected SurvivorsRunUnitBase Owner => _owner;
        protected SurvivorsRunAttack Attack => _attack;
        public bool IsPatternEnabled => _patternEnabled && isActiveAndEnabled;

        protected virtual void Awake()
        {
            EnsureBindings();
        }

        public virtual void Setup(SurvivorsRunUnitBase owner, SurvivorsRunAttack attack)
        {
            _owner = owner != null ? owner : GetComponent<SurvivorsRunUnitBase>();
            _attack = attack != null ? attack : GetComponent<SurvivorsRunAttack>();

            if (_attack != null && _owner != null)
                _attack.BindOwner(_owner);
        }

        public virtual void SetPatternEnabled(bool enabled)
        {
            _patternEnabled = enabled;
            this.enabled = enabled;
            if (!enabled)
                OnPatternDisabled();
            else
                OnPatternEnabled();
        }

        protected virtual void Update()
        {
            if (!CanTick())
                return;

            var scale = _owner.Manager != null ? _owner.Manager.TimeScale : 1f;
            Tick(Time.deltaTime * scale);
        }

        protected bool CanTick()
        {
            if (!_patternEnabled || !isActiveAndEnabled)
                return false;

            EnsureBindings();
            if (_owner == null || _attack == null || _owner.IsDead)
                return false;

            return true;
        }

        protected void EnsureBindings()
        {
            if (_owner == null)
                _owner = GetComponent<SurvivorsRunUnitBase>();

            if (_attack == null)
                _attack = GetComponent<SurvivorsRunAttack>();

            if (_attack != null && _owner != null)
                _attack.BindOwner(_owner);
        }

        /// <summary>패턴별 매 프레임/주기 로직.</summary>
        protected abstract void Tick(float dt);

        protected virtual void OnPatternEnabled()
        {
        }

        protected virtual void OnPatternDisabled()
        {
        }
    }
}
