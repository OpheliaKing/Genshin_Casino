using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 맵/세션에 하나 두는 적 스포너.
    /// 스폰 텀·이속 압박은 <see cref="SurvivorsRunEnemyPressureSettings"/>로 조절한다.
    /// </summary>
    public class SurvivorsRunEnemySpawner : MonoBehaviour
    {
        [SerializeField] private bool _spawningEnabled;

        [SerializeField]
        private SurvivorsRunEnemyPressureSettings _pressureSettings = new();

        [SerializeField] private string _defaultEnemyUnitId = "enemy_001";

        private SurvivorsRunManager _manager;
        private float _spawnCooldown;
        private bool _spawnInFlight;

        public SurvivorsRunEnemyPressureSettings PressureSettings => _pressureSettings;

        public void Bind(SurvivorsRunManager manager)
        {
            _manager = manager;
        }

        public void SetSpawningEnabled(bool enabled)
        {
            _spawningEnabled = enabled;
            if (!enabled || _manager == null)
                return;

            _manager.ResetEnemyPressure(_pressureSettings);
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

            if (_manager.PlayerInfo != null && _manager.PlayerInfo.Life <= 0)
                return;

            var dt = Time.deltaTime * _manager.TimeScale;
            _manager.TickEnemyPressure(dt);

            _manager.PruneInactiveEnemies();

            _spawnCooldown -= dt;
            if (_spawnCooldown > 0f || _spawnInFlight)
                return;

            var pressure = _manager.EnemyPressure;
            if (pressure == null)
                return;

            if (_manager.ActiveEnemyCount >= pressure.MaxAliveEnemies)
                return;

            _spawnCooldown = Mathf.Max(0.05f, pressure.SpawnInterval);
            _ = SpawnOneAsync();
        }

        private async System.Threading.Tasks.Task SpawnOneAsync()
        {
            if (_spawnInFlight || _manager == null)
                return;

            _spawnInFlight = true;
            try
            {
                if (!_manager.TryGetEnemySpawnPosition(out var position))
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
