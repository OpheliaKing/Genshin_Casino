using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 적 SO 로드, 스폰/해제, 맵 clamp, 카메라 밖 스폰 좌표.
    /// </summary>
    public partial class SurvivorsRunManager
    {
        [SerializeField] private SurvivorsRunMapBounds _mapBounds;
        [SerializeField] private Camera _runCamera;
        [SerializeField] private Transform _enemyRoot;
        [SerializeField] private float _spawnOutsidePadding = 1.5f;
        [SerializeField] private float _spawnRingThickness = 3f;
        [SerializeField] private int _spawnPositionMaxAttempts = 12;

        private SurvivorsRunEnemySO _enemySo;
        private SurvivorsRunEnemySpawner _enemySpawner;
        private readonly List<SurvivorsRunUnitBase> _activeEnemies = new();

        public SurvivorsRunEnemySO EnemySo => _enemySo;
        public SurvivorsRunMapBounds MapBounds => _mapBounds;
        public IReadOnlyList<SurvivorsRunUnitBase> ActiveEnemies => _activeEnemies;
        public int ActiveEnemyCount => CountAliveEnemies();

        private void EnsureEnemyRuntimeRefs()
        {
            if (_runCamera == null)
                _runCamera = GetComponentInChildren<Camera>(true);

            if (_mapBounds == null)
                _mapBounds = FindFirstObjectByType<SurvivorsRunMapBounds>();

            if (_mapBounds == null)
            {
                _mapBounds = gameObject.AddComponent<SurvivorsRunMapBounds>();
                Debug.LogWarning("[SurvivorsRunManager] MapBounds가 없어 세션에 기본 범위를 추가했습니다. 맵에 SurvivorsRunMapBounds를 두는 것을 권장합니다.");
            }

            if (_enemyRoot == null)
            {
                var root = transform.Find("Enemies");
                if (root == null)
                {
                    var go = new GameObject("Enemies");
                    go.transform.SetParent(transform, false);
                    _enemyRoot = go.transform;
                }
                else
                {
                    _enemyRoot = root;
                }
            }

            if (_enemySpawner == null)
            {
                _enemySpawner = GetComponent<SurvivorsRunEnemySpawner>();
                if (_enemySpawner == null)
                    _enemySpawner = gameObject.AddComponent<SurvivorsRunEnemySpawner>();
            }

            _enemySpawner.Bind(this);
        }

        private async Task EnsureEnemySoAsync()
        {
            if (_enemySo != null)
                return;

            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager == null)
            {
                Debug.LogError("[SurvivorsRunManager] ResourceManager가 없어 EnemySO를 로드할 수 없습니다.");
                return;
            }

            _enemySo = await resourceManager.LoadAsync<SurvivorsRunEnemySO>(
                PublicVariable.Address.SurvivorsRunEnemySO);

            if (_enemySo == null)
                Debug.LogError("[SurvivorsRunManager] SurvivorsRunEnemySO 로드에 실패했습니다.");
        }

        private async Task StartEnemySpawningAsync()
        {
            EnsureEnemyRuntimeRefs();
            await EnsureEnemySoAsync();
            if (_enemySo == null)
                return;

            _enemySpawner.SetSpawningEnabled(true);
        }

        private void StopEnemySpawning()
        {
            if (_enemySpawner != null)
                _enemySpawner.SetSpawningEnabled(false);
        }

        public SurvivorsRunUnitData GetEnemyData(string unitId)
        {
            return _enemySo != null ? _enemySo.GetByUnitId(unitId) : null;
        }

        public SurvivorsRunUnitData GetDefaultEnemyData()
        {
            if (_enemySo?.EnemyList == null || _enemySo.EnemyList.Count == 0)
                return null;

            for (var i = 0; i < _enemySo.EnemyList.Count; i++)
            {
                if (_enemySo.EnemyList[i] != null)
                    return _enemySo.EnemyList[i];
            }

            return null;
        }

        public async Task<SurvivorsRunUnitBase> SpawnEnemyAsync(SurvivorsRunUnitData data, Vector3 position)
        {
            if (data == null)
                return null;

            if (string.IsNullOrWhiteSpace(data.UnitPrefabPath))
            {
                Debug.LogError($"[SurvivorsRunManager] 적 프리팹 경로가 비어 있습니다: {data.UnitId}");
                return null;
            }

            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager == null)
            {
                Debug.LogError("[SurvivorsRunManager] ResourceManager가 없습니다.");
                return null;
            }

            EnsureEnemyRuntimeRefs();
            position = ClampToMap(position);

            var instance = await resourceManager.InstantiateAsync(
                data.UnitPrefabPath.Trim(),
                parent: _enemyRoot,
                startInactive: false);

            if (this == null)
            {
                if (instance != null)
                    resourceManager.ReleaseInstance(instance);
                return null;
            }

            if (instance == null)
            {
                Debug.LogError($"[SurvivorsRunManager] 적 프리팹 생성 실패: {data.UnitPrefabPath}");
                return null;
            }

            instance.transform.position = position;

            var unit = instance.GetComponent<SurvivorsRunUnitBase>();
            if (unit == null)
                unit = instance.GetComponentInChildren<SurvivorsRunUnitBase>(true);

            if (unit == null)
            {
                Debug.LogWarning("[SurvivorsRunManager] 적 프리팹에 SurvivorsRunUnitBase가 없습니다.");
                resourceManager.ReleaseInstance(instance);
                return null;
            }

            unit.BindManager(this);
            unit.Setup(
                data.UnitId,
                SURVIVORSRUN_UNIT_TYPE.ENEMY,
                data.UnitHP,
                Mathf.RoundToInt(data.UnitAttack),
                data.UnitSpeed,
                attackSpeed: 1f);

            var loadout = instance.GetComponent<SurvivorsRunEnemyLoadoutController>();
            if (loadout == null)
                loadout = instance.AddComponent<SurvivorsRunEnemyLoadoutController>();
            loadout.Setup(data.EnemyLoadout);

            _activeEnemies.Add(unit);
            return unit;
        }

        public async Task<SurvivorsRunUnitBase> SpawnEnemyAsync(string unitId, Vector3 position)
        {
            await EnsureEnemySoAsync();
            return await SpawnEnemyAsync(GetEnemyData(unitId), position);
        }

        public bool TryGetSpawnPositionOutsideCamera(out Vector3 position)
        {
            position = default;
            EnsureEnemyRuntimeRefs();

            if (_playerUnit == null)
                return false;

            var cam = _runCamera != null ? _runCamera : Camera.main;
            if (cam == null)
                return false;

            var origin = _playerUnit.transform.position;
            GetCameraHalfExtents(cam, out var halfW, out var halfH);

            var minRadius = Mathf.Max(halfW, halfH) + Mathf.Max(0f, _spawnOutsidePadding);
            var maxRadius = minRadius + Mathf.Max(0.1f, _spawnRingThickness);

            for (var i = 0; i < _spawnPositionMaxAttempts; i++)
            {
                var angle = Random.Range(0f, Mathf.PI * 2f);
                var radius = Random.Range(minRadius, maxRadius);
                var candidate = origin + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                candidate = ClampToMap(candidate);

                if (IsOutsideCameraView(candidate, cam, _spawnOutsidePadding * 0.25f))
                {
                    position = candidate;
                    return true;
                }
            }

            return false;
        }

        public Vector3 ClampToMap(Vector3 position)
        {
            EnsureEnemyRuntimeRefs();
            return _mapBounds != null ? _mapBounds.ClampPosition(position) : position;
        }

        public void PruneInactiveEnemies()
        {
            var resourceManager = GameManager.Instance?.ResourceManager;

            for (var i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = _activeEnemies[i];
                if (enemy != null && !enemy.IsDead && enemy.gameObject.activeInHierarchy)
                    continue;

                if (enemy != null)
                {
                    if (resourceManager != null)
                        resourceManager.ReleaseInstance(enemy.gameObject);
                    else if (enemy.gameObject != null)
                        Destroy(enemy.gameObject);
                }

                _activeEnemies.RemoveAt(i);
            }
        }

        private void ReleaseAllEnemies()
        {
            StopEnemySpawning();

            var resourceManager = GameManager.Instance?.ResourceManager;
            for (var i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = _activeEnemies[i];
                if (enemy == null)
                    continue;

                if (resourceManager != null)
                    resourceManager.ReleaseInstance(enemy.gameObject);
                else
                    Destroy(enemy.gameObject);
            }

            _activeEnemies.Clear();
        }

        private int CountAliveEnemies()
        {
            var count = 0;
            for (var i = 0; i < _activeEnemies.Count; i++)
            {
                var enemy = _activeEnemies[i];
                if (enemy != null && !enemy.IsDead && enemy.gameObject.activeInHierarchy)
                    count++;
            }

            return count;
        }

        private static void GetCameraHalfExtents(Camera cam, out float halfW, out float halfH)
        {
            if (cam.orthographic)
            {
                halfH = cam.orthographicSize;
                halfW = halfH * cam.aspect;
                return;
            }

            // SurvivorsRun은 직교 카메라 기준. 원근이면 대략 z=10 평면 가정.
            var dist = Mathf.Abs(cam.transform.position.z);
            halfH = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * dist;
            halfW = halfH * cam.aspect;
        }

        private static bool IsOutsideCameraView(Vector3 worldPos, Camera cam, float padding)
        {
            GetCameraHalfExtents(cam, out var halfW, out var halfH);
            var local = worldPos - cam.transform.position;
            return Mathf.Abs(local.x) > halfW + padding || Mathf.Abs(local.y) > halfH + padding;
        }
    }
}
