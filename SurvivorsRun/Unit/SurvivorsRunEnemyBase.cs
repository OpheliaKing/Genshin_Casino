using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 적 유닛. HP·이동 등은 <see cref="SurvivorsRunUnitBase"/>,
    /// 행진(좌측 전진)·누수·클릭 타겟 통보는 여기서 담당한다.
    /// </summary>
    public class SurvivorsRunEnemyBase : SurvivorsRunUnitBase
    {
        [SerializeField] private bool _marchEnabled = true;
        [SerializeField] private Vector2 _marchDirection = Vector2.left;

        private bool _leaked;

        private void Update()
        {
            TickMarch();
        }

        private void TickMarch()
        {
            if (!_marchEnabled || _leaked || IsDead)
                return;

            if (UnitType != SURVIVORSRUN_UNIT_TYPE.ENEMY)
                return;

            var manager = Manager;
            if (manager == null || manager.TimeScale <= 0f)
                return;

            var dir = _marchDirection.sqrMagnitude > 0.0001f
                ? _marchDirection
                : Vector2.left;

            Move(dir);
            var pos = manager.ClampEnemyMarchPosition(transform.position);
            transform.position = pos;

            if (manager.HasEnemyLeaked(pos))
            {
                _leaked = true;
                manager.NotifyEnemyLeaked(this);
            }
        }

        public void SetMarchEnabled(bool enabled)
        {
            _marchEnabled = enabled;
        }

        /// <summary>스폰·재사용 시 누수 플래그를 초기화한다.</summary>
        public void ResetMarchState()
        {
            _leaked = false;
        }

        /// <summary>
        /// 플레이어가 이 적을 선택했을 때. Manager에 공격 타겟을 등록한다.
        /// </summary>
        public void NotifyPlayerClick()
        {
            if (IsDead)
                return;

            Manager?.SetAttackTarget(this);
        }

        protected override void OnDied(SurvivorsRunUnitBase killer)
        {
            Manager?.ClearAttackTargetIfMatch(this);
            base.OnDied(killer);
            Manager?.DespawnEnemy(this);
        }
    }
}
