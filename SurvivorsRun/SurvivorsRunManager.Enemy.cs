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
        [SerializeField] private Camera _runCamera;
        [SerializeField] private Transform _enemyRoot;
        [SerializeField]
        [Tooltip("우측 스폰 시 카메라/맵 오른쪽에서 얼마나 밖으로 둘지.")]
        private float _rightSpawnPadding = 1.5f;
        [SerializeField]
        [Tooltip("우측 스폰 Y 랜덤 시 화면 상하 inset.")]
        private float _rightSpawnVerticalInset = 0.5f;

        private SurvivorsRunEnemySO _enemySo;
        private SurvivorsRunEnemySpawner _enemySpawner;
        private readonly List<SurvivorsRunUnitBase> _activeEnemies = new();

        public SurvivorsRunEnemySO EnemySo => _enemySo;
        public IReadOnlyList<SurvivorsRunUnitBase> ActiveEnemies => _activeEnemies;
        public int ActiveEnemyCount => CountAliveEnemies();

        private void EnsureEnemyRuntimeRefs()
        {
            if (_runCamera == null)
                _runCamera = GetComponentInChildren<Camera>(true);

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
            // 좌측 누수를 위해 X min clamp는 스폰 시에도 강제하지 않는다. Y만 맞춤.
            position = ClampEnemyMarchPosition(position);

            // 비활성 생성 + 월드 좌표를 Instantiate 시점에 넣어 원점 플래시를 막는다.
            var instance = await resourceManager.InstantiateAsync(
                data.UnitPrefabPath.Trim(),
                parent: _enemyRoot,
                startInactive: true,
                worldPosition: position);

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

            var enemy = instance.GetComponent<SurvivorsRunEnemyBase>();
            if (enemy == null)
                enemy = instance.GetComponentInChildren<SurvivorsRunEnemyBase>(true);

            if (enemy == null)
            {
                Debug.LogError(
                    $"[SurvivorsRunManager] 적 프리팹에 SurvivorsRunEnemyBase가 없습니다: {data.UnitPrefabPath}");
                resourceManager.ReleaseInstance(instance);
                return null;
            }

            enemy.BindManager(this);
            enemy.Setup(
                data.UnitId,
                SURVIVORSRUN_UNIT_TYPE.ENEMY,
                data.UnitHP,
                Mathf.RoundToInt(data.UnitAttack),
                data.UnitSpeed,
                attackSpeed: 1f);
            enemy.ResetMarchState();

            var loadout = instance.GetComponent<SurvivorsRunEnemyLoadoutController>();
            if (loadout == null)
                loadout = instance.AddComponent<SurvivorsRunEnemyLoadoutController>();
            loadout.Setup(data.EnemyLoadout);

            instance.SetActive(true);
            _activeEnemies.Add(enemy);
            return enemy;
        }

        public async Task<SurvivorsRunUnitBase> SpawnEnemyAsync(string unitId, Vector3 position)
        {
            await EnsureEnemySoAsync();
            return await SpawnEnemyAsync(GetEnemyData(unitId), position);
        }

        /// <summary>
        /// 고정 디펜스 스폰: 화면/맵 오른쪽 + Y 랜덤.
        /// </summary>
        public bool TryGetEnemySpawnPosition(out Vector3 position)
        {
            position = default;
            EnsureEnemyRuntimeRefs();

            var cam = _runCamera != null ? _runCamera : Camera.main;
            if (cam == null && _activeMap == null)
                return false;

            float minY;
            float maxY;
            float spawnX;

            if (cam != null)
            {
                GetCameraHalfExtents(cam, out var halfW, out var halfH);
                var camPos = cam.transform.position;
                var inset = Mathf.Max(0f, _rightSpawnVerticalInset);
                minY = camPos.y - halfH + inset;
                maxY = camPos.y + halfH - inset;
                if (minY > maxY)
                {
                    minY = camPos.y - halfH;
                    maxY = camPos.y + halfH;
                }

                spawnX = camPos.x + halfW + Mathf.Max(0.1f, _rightSpawnPadding);
            }
            else
            {
                _activeMap.GetClampLocal(out var minLocal, out var maxLocal);
                var minWorld = _activeMap.transform.TransformPoint(new Vector3(minLocal.x, minLocal.y, 0f));
                var maxWorld = _activeMap.transform.TransformPoint(new Vector3(maxLocal.x, maxLocal.y, 0f));
                minY = Mathf.Min(minWorld.y, maxWorld.y);
                maxY = Mathf.Max(minWorld.y, maxWorld.y);
                spawnX = Mathf.Max(minWorld.x, maxWorld.x) + Mathf.Max(0.1f, _rightSpawnPadding);
            }

            if (_activeMap != null)
            {
                _activeMap.GetClampLocal(out var minLocal, out var maxLocal);
                var mapMin = _activeMap.transform.TransformPoint(new Vector3(minLocal.x, minLocal.y, 0f));
                var mapMax = _activeMap.transform.TransformPoint(new Vector3(maxLocal.x, maxLocal.y, 0f));
                var mapMinY = Mathf.Min(mapMin.y, mapMax.y);
                var mapMaxY = Mathf.Max(mapMin.y, mapMax.y);
                minY = Mathf.Max(minY, mapMinY);
                maxY = Mathf.Min(maxY, mapMaxY);
                if (minY > maxY)
                {
                    minY = mapMinY;
                    maxY = mapMaxY;
                }
            }

            var y = Random.Range(minY, maxY);
            position = new Vector3(spawnX, y, 0f);
            position = ClampEnemyMarchPosition(position);
            // Clamp가 X를 맵 max 안으로 당기면 화면 안 스폰이 될 수 있어, 우측은 다시 밖으로 둔다.
            if (cam != null)
            {
                GetCameraHalfExtents(cam, out var halfW, out _);
                var rightEdge = cam.transform.position.x + halfW + Mathf.Max(0.1f, _rightSpawnPadding);
                if (position.x < rightEdge)
                    position.x = rightEdge;
            }

            return true;
        }

        /// <summary>
        /// 행진 적 위치: Y(및 X max)만 제한. X min은 누수를 위해 막지 않는다.
        /// </summary>
        public Vector3 ClampEnemyMarchPosition(Vector3 position)
        {
            if (_activeMap == null)
                return ClampToMap(position);

            _activeMap.GetClampLocal(out var minLocal, out var maxLocal);
            var local = _activeMap.transform.InverseTransformPoint(position);
            local.y = Mathf.Clamp(local.y, minLocal.y, maxLocal.y);
            local.x = Mathf.Min(local.x, maxLocal.x);
            local.z = 0f;
            return _activeMap.transform.TransformPoint(local);
        }

        /// <summary>맵 왼쪽 경계(로컬 min X) 이하면 누수.</summary>
        public bool HasEnemyLeaked(Vector3 worldPosition)
        {
            if (_activeMap == null)
                return false;

            _activeMap.GetClampLocal(out var minLocal, out _);
            var local = _activeMap.transform.InverseTransformPoint(worldPosition);
            return local.x <= minLocal.x;
        }

        /// <summary>적이 왼쪽을 통과했을 때 라이프 감소 후 제거.</summary>
        public void NotifyEnemyLeaked(SurvivorsRunUnitBase enemy)
        {
            if (enemy == null)
                return;

            var life = _playerInfo.LoseLife(1);
            Debug.Log($"[SurvivorsRun] 적 누수 → Life={life} (enemy={enemy.Tid})");

            ClearAttackTargetIfMatch(enemy);
            _activeEnemies.Remove(enemy);

            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager != null)
                resourceManager.ReleaseInstance(enemy.gameObject);
            else if (enemy.gameObject != null)
                Destroy(enemy.gameObject);

            if (life <= 0)
            {
                StopEnemySpawning();
                Debug.Log("[SurvivorsRun] Life 0 — 세션 패배(UI는 이후 연결).");
            }
        }

        public Vector3 ClampToMap(Vector3 position)
        {
            if (_activeMap != null)
                return _activeMap.ClampPosition(position);

            return position;
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
    }
}
