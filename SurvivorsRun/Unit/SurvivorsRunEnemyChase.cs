using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 플레이어를 향해 이동하는 기본 적 AI.
    /// </summary>
    [RequireComponent(typeof(SurvivorsRunUnitBase))]
    public class SurvivorsRunEnemyChase : MonoBehaviour
    {
        [SerializeField] private bool _chaseEnabled = true;
        [SerializeField] private float _defaultMoveSpeed = 2.25f;
        [SerializeField] private int _defaultHp = 30;
        [SerializeField] private int _defaultAttack = 5;
        [SerializeField] private float _defaultAttackSpeed = 1f;
        [SerializeField] private float _stopDistance = 0.05f;

        private SurvivorsRunUnitBase _owner;

        private void Awake()
        {
            _owner = GetComponent<SurvivorsRunUnitBase>();
        }

        private void Start()
        {
            EnsureOwnerReady();
        }

        private void Update()
        {
            if (!_chaseEnabled)
                return;

            EnsureOwnerReady();
            if (_owner == null || _owner.IsDead)
                return;

            if (_owner.UnitType != SURVIVORSRUN_UNIT_TYPE.ENEMY)
                return;

            var player = _owner.Manager?.PlayerUnit;
            if (player == null || player.IsDead)
                return;

            var delta = (Vector2)(player.transform.position - _owner.transform.position);
            if (delta.sqrMagnitude <= _stopDistance * _stopDistance)
                return;

            _owner.Move(delta);
            if (_owner.Manager != null)
                _owner.transform.position = _owner.Manager.ClampToMap(_owner.transform.position);
        }

        public void SetChaseEnabled(bool enabled)
        {
            _chaseEnabled = enabled;
        }

        private void EnsureOwnerReady()
        {
            if (_owner == null)
                _owner = GetComponent<SurvivorsRunUnitBase>();

            if (_owner == null)
                return;

            if (_owner.Manager == null)
            {
                var manager = GetComponentInParent<SurvivorsRunManager>();
                if (manager == null)
                    manager = FindFirstObjectByType<SurvivorsRunManager>();

                if (manager != null)
                    _owner.BindManager(manager);
            }

            // 스폰 파이프라인 전에 씬에 둔 적도 테스트 가능하게 기본 Setup
            if (_owner.UnitType == SURVIVORSRUN_UNIT_TYPE.NONE)
            {
                var tid = string.IsNullOrEmpty(_owner.Tid) ? gameObject.name : _owner.Tid;
                var moveSpeed = _owner.MoveSpeed > 0f ? _owner.MoveSpeed : _defaultMoveSpeed;
                _owner.Setup(
                    tid,
                    SURVIVORSRUN_UNIT_TYPE.ENEMY,
                    _defaultHp,
                    _defaultAttack,
                    moveSpeed,
                    _defaultAttackSpeed);
            }
            else if (_owner.MoveSpeed <= 0f)
            {
                _owner.SetMoveSpeed(_defaultMoveSpeed);
            }
        }
    }
}
