using System.Threading.Tasks;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 공격 타겟 지정. Combat(데미지)과 분리한다.
    /// <see cref="PublicVariable.Address.SurvivorsRunTargetObject"/> 프리팹을 적 자식으로 붙인다.
    /// 로컬 오프셋은 TargetObject 프리팹의 SurvivorsRunTargetingObject 값을 그대로 쓴다.
    /// </summary>
    public partial class SurvivorsRunManager
    {
        [Header("Targeting")]
        [SerializeField]
        [Tooltip("비우면 PublicVariable SurvivorsRunTargetObject 주소를 쓴다.")]
        private string _targetingObjectAddress = PublicVariable.Address.SurvivorsRunTargetObject;

        private SurvivorsRunEnemyBase _attackTarget;
        private SurvivorsRunTargetingObject _targetingObject;
        private GameObject _targetingObjectInstance;
        private Transform _targetingHome;
        private bool _targetingLoadInFlight;

        /// <summary>유효한 우선 공격 대상. 없거나 사망·비활성이면 null.</summary>
        public SurvivorsRunUnitBase AttackTarget => GetValidAttackTarget();

        public SurvivorsRunTargetingObject TargetingObject => _targetingObject;

        public void SetAttackTarget(SurvivorsRunEnemyBase enemy)
        {
            if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy)
            {
                ClearAttackTarget();
                return;
            }

            _attackTarget = enemy;

            if (_targetingObject != null)
            {
                _targetingObject.AttachTo(enemy);
                Debug.Log($"[SurvivorsRun] AttackTarget → {enemy.Tid ?? enemy.name}");
                return;
            }

            _ = SetAttackTargetWhenReadyAsync(enemy);
        }

        public void ClearAttackTarget()
        {
            _attackTarget = null;
            if (_targetingObject != null)
                _targetingObject.Detach();
        }

        private SurvivorsRunUnitBase GetValidAttackTarget()
        {
            if (_attackTarget == null)
                return null;

            if (_attackTarget.IsDead || !_attackTarget.gameObject.activeInHierarchy)
            {
                ClearAttackTarget();
                return null;
            }

            return _attackTarget;
        }

        /// <summary>누수·제거 시 타겟이면 해제.</summary>
        public void ClearAttackTargetIfMatch(SurvivorsRunUnitBase unit)
        {
            if (unit == null || _attackTarget == null)
                return;

            if (_attackTarget == unit)
                ClearAttackTarget();
        }

        private async Task SetAttackTargetWhenReadyAsync(SurvivorsRunEnemyBase enemy)
        {
            await EnsureTargetingObjectAsync();
            if (this == null || enemy == null || enemy.IsDead)
                return;

            if (_attackTarget != enemy)
                return;

            _targetingObject?.AttachTo(enemy);
            Debug.Log($"[SurvivorsRun] AttackTarget → {enemy.Tid ?? enemy.name}");
        }

        private async Task EnsureTargetingObjectAsync()
        {
            if (_targetingObject != null)
            {
                _targetingObject.BindHome(_targetingHome != null ? _targetingHome : transform);
                return;
            }

            if (_targetingLoadInFlight)
            {
                while (_targetingLoadInFlight && this != null && _targetingObject == null)
                    await Task.Yield();
                return;
            }

            _targetingLoadInFlight = true;
            try
            {
                _targetingHome = transform;

                var existing = GetComponentInChildren<SurvivorsRunTargetingObject>(true);
                if (existing != null)
                {
                    _targetingObject = existing;
                    _targetingObjectInstance = existing.gameObject;
                    _targetingObject.BindHome(_targetingHome);
                    _targetingObject.Detach();
                    return;
                }

                var address = string.IsNullOrWhiteSpace(_targetingObjectAddress)
                    ? PublicVariable.Address.SurvivorsRunTargetObject
                    : _targetingObjectAddress.Trim();

                var resourceManager = GameManager.Instance?.ResourceManager;
                if (resourceManager == null)
                {
                    Debug.LogError("[SurvivorsRunManager] ResourceManager가 없어 TargetObject를 생성할 수 없습니다.");
                    return;
                }

                var instance = await resourceManager.InstantiateAsync(
                    address,
                    parent: transform,
                    startInactive: true);

                if (this == null)
                {
                    if (instance != null)
                        resourceManager.ReleaseInstance(instance);
                    return;
                }

                if (instance == null)
                {
                    Debug.LogError($"[SurvivorsRunManager] TargetObject 생성 실패: {address}");
                    return;
                }

                _targetingObjectInstance = instance;
                _targetingObject = instance.GetComponent<SurvivorsRunTargetingObject>();
                if (_targetingObject == null)
                    _targetingObject = instance.AddComponent<SurvivorsRunTargetingObject>();

                _targetingObject.BindHome(_targetingHome);
                _targetingObject.Detach();
            }
            finally
            {
                _targetingLoadInFlight = false;
            }
        }

        private void ReleaseTargetingObject()
        {
            ClearAttackTarget();

            if (_targetingObjectInstance == null)
            {
                _targetingObject = null;
                return;
            }

            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager != null)
                resourceManager.ReleaseInstance(_targetingObjectInstance);
            else
                Destroy(_targetingObjectInstance);

            _targetingObjectInstance = null;
            _targetingObject = null;
        }
    }
}
