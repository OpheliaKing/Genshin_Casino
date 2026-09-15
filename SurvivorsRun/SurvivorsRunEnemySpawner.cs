using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 맵/세션에 하나 두는 적 스포너.
    /// 주기적으로 Manager에 카메라 밖+clamp 스폰을 요청한다.
    /// </summary>
    public class SurvivorsRunEnemySpawner : MonoBehaviour
    {
        [SerializeField] private bool _spawningEnabled;
        [SerializeField] private float _spawnInterval = 1.25f;
        [SerializeField] private int _maxAliveEnemies = 40;
        [SerializeField] private string _defaultEnemyUnitId = "enemy_001";

        private SurvivorsRunManager _manager;
        private float _spawnCooldown;
        private bool _spawnInFlight;

        public void Bind(SurvivorsRunManager manager)
        {
            _manager = manager;
        }

        public void SetSpawningEnabled(bool enabled)
        {
            _spawningEnabled = enabled;
            if (enabled)
                _spawnCooldown = 0f;
        }

        private void Update()
        {
            if (!_spawningEnabled || _manager == null)
                return;

            if (_manager.TimeScale <= 0f)
                return;

            if (_manager.PlayerUnit == null || _manager.PlayerUnit.IsDead)
                return;

            _manager.PruneInactiveEnemies();

            _spawnCooldown -= Time.deltaTime * _manager.TimeScale;
            if (_spawnCooldown > 0f || _spawnInFlight)
                return;

            if (_manager.ActiveEnemyCount >= _maxAliveEnemies)
                return;

            _spawnCooldown = Mathf.Max(0.05f, _spawnInterval);
            _ = SpawnOneAsync();
        }

        private async System.Threading.Tasks.Task SpawnOneAsync()
        {
            if (_spawnInFlight || _manager == null)
                return;

            _spawnInFlight = true;
            try
            {
                if (!_manager.TryGetSpawnPositionOutsideCamera(out var position))
                    return;

                var data = ResolveEnemyData();
                if (data == null)
                {
                    Debug.LogWarning("[SurvivorsRunEnemySpawner] 스폰할 적 데이터가 없습니다.");
                    return;
                }

                await _manager.SpawnEnemyAsync(data, position);
            }
            finally
            {
                _spawnInFlight = false;
            }
        }

        private SurvivorsRunUnitData ResolveEnemyData()
        {
            if (!string.IsNullOrWhiteSpace(_defaultEnemyUnitId))
            {
                var byId = _manager.GetEnemyData(_defaultEnemyUnitId);
                if (byId != null)
                    return byId;
            }

            return _manager.GetDefaultEnemyData();
        }
    }
}
