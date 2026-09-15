using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 접촉(돌진) 패턴.
    /// 트리거 겹침 중 플레이어에게 <see cref="TryAttackTarget"/>을 시도한다.
    /// (쿨다운·데미지는 Attack/Combat이 처리)
    /// </summary>
    public class SurvivorsRunContactPattern : SurvivorsRunAttackPatternBase
    {
        public override SURVIVORSRUN_ATTACK_PATTERN Pattern => SURVIVORSRUN_ATTACK_PATTERN.CONTACT;

        /// <summary>Contact는 자체 추적 Tick이 없고, 충돌 콜백으로 공격한다.</summary>
        protected override void Tick(float dt)
        {
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!IsPatternEnabled || other == null)
                return;

            var target = ResolveUnit(other);
            if (target == null || target.UnitType != SURVIVORSRUN_UNIT_TYPE.PLAYER)
                return;

            TryAttackTarget(target);
        }

        public bool TryAttackTarget(SurvivorsRunUnitBase target)
        {
            if (!IsPatternEnabled)
                return false;

            EnsureBindings();
            if (Attack == null)
                return false;

            return Attack.TryAttack(target);
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
