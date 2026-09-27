using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 고정 디펜스용 적 AI: 왼쪽(-X)으로 일정하게 전진한다.
    /// 맵 왼쪽 경계를 넘으면 라이프 누수로 처리한다.
    /// </summary>
    [RequireComponent(typeof(SurvivorsRunUnitBase))]
    public class SurvivorsRunEnemyMarch : MonoBehaviour
    {
        [SerializeField] private bool _marchEnabled = true;
        [SerializeField] private Vector2 _marchDirection = Vector2.left;

        private SurvivorsRunUnitBase _owner;
        private bool _leaked;

        private void Awake()
        {
            _owner = GetComponent<SurvivorsRunUnitBase>();
        }

        private void Update()
        {
            if (!_marchEnabled || _leaked)
                return;

            if (_owner == null)
                _owner = GetComponent<SurvivorsRunUnitBase>();

            if (_owner == null || _owner.IsDead)
                return;

            if (_owner.UnitType != SURVIVORSRUN_UNIT_TYPE.ENEMY)
                return;

            var manager = _owner.Manager;
            if (manager == null)
                return;

            if (manager.TimeScale <= 0f)
                return;

            var dir = _marchDirection.sqrMagnitude > 0.0001f
                ? _marchDirection
                : Vector2.left;

            _owner.Move(dir);
            var pos = manager.ClampEnemyMarchPosition(_owner.transform.position);
            _owner.transform.position = pos;

            if (manager.HasEnemyLeaked(pos))
            {
                _leaked = true;
                manager.NotifyEnemyLeaked(_owner);
            }
        }

        public void SetMarchEnabled(bool enabled)
        {
            _marchEnabled = enabled;
        }
    }
}
