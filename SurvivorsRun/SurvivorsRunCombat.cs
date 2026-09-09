namespace SHIN
{
    /// <summary>
    /// SurvivorsRun 전투 판정 진입점. 피해 적용은 여기로 모은다.
    /// </summary>
    public class SurvivorsRunCombat
    {
        private readonly SurvivorsRunManager _manager;

        public SurvivorsRunCombat(SurvivorsRunManager manager)
        {
            _manager = manager;
        }

        public SurvivorsRunManager Manager => _manager;

        /// <summary>
        /// attacker가 target에게 damage를 적용한다. 환경 피해 등은 attacker를 null로 둔다.
        /// </summary>
        public bool TryApplyDamage(SurvivorsRunUnitBase target, int damage, SurvivorsRunUnitBase attacker = null)
        {
            if (target == null || target.IsDead)
                return false;

            if (damage <= 0)
                return false;

            if (attacker != null && !CanDamage(attacker, target))
                return false;

            return target.TakeDamage(damage, attacker);
        }

        public bool TryApplyDamageFromAttackStat(SurvivorsRunUnitBase attacker, SurvivorsRunUnitBase target)
        {
            if (attacker == null)
                return false;

            return TryApplyDamage(target, attacker.Attack, attacker);
        }

        private static bool CanDamage(SurvivorsRunUnitBase attacker, SurvivorsRunUnitBase target)
        {
            if (attacker.UnitType == SURVIVORSRUN_UNIT_TYPE.PLAYER &&
                target.UnitType == SURVIVORSRUN_UNIT_TYPE.ENEMY)
                return true;

            if (attacker.UnitType == SURVIVORSRUN_UNIT_TYPE.ENEMY &&
                target.UnitType == SURVIVORSRUN_UNIT_TYPE.PLAYER)
                return true;

            return false;
        }
    }
}
