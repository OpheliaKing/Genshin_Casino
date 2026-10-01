using UnityEngine;
using UnityEngine.Tilemaps;

namespace SHIN
{
    /// <summary>
    /// SurvivorsRun 맵 프리팹 루트.
    /// 이동 clamp 범위와 플레이어 스폰 위치를 소유한다.
    /// </summary>
    public class SurvivorsRunMap : MonoBehaviour
    {
        public enum ClampSource
        {
            ManualLocal,
            TilemapBounds,
        }

        [Header("Clamp")]
        [SerializeField] private ClampSource _clampSource = ClampSource.ManualLocal;
        [SerializeField] private Vector2 _clampMinLocal = new(-20f, -20f);
        [SerializeField] private Vector2 _clampMaxLocal = new(20f, 20f);
        [SerializeField] private Tilemap _boundsTilemap;
        [SerializeField] private Vector2 _tilemapPadding = Vector2.zero;

        [Header("Spawn")]
        [SerializeField] private Transform _playerSpawnPoint;
        [SerializeField]
        [Tooltip("적 스폰·행진 Y 로컬 min. 도로 레인에 맞출 것.")]
        private float _enemySpawnYMinLocal = -1.5f;
        [SerializeField]
        [Tooltip("적 스폰·행진 Y 로컬 max.")]
        private float _enemySpawnYMaxLocal = 1.5f;

        public ClampSource Source => _clampSource;
        public Transform PlayerSpawnPoint => _playerSpawnPoint;
        public float EnemySpawnYMinLocal => Mathf.Min(_enemySpawnYMinLocal, _enemySpawnYMaxLocal);
        public float EnemySpawnYMaxLocal => Mathf.Max(_enemySpawnYMinLocal, _enemySpawnYMaxLocal);

        public Vector2 ClampMinLocal
        {
            get
            {
                EnsureClampCache();
                return _clampMinLocal;
            }
        }

        public Vector2 ClampMaxLocal
        {
            get
            {
                EnsureClampCache();
                return _clampMaxLocal;
            }
        }

        private void Awake()
        {
            EnsureSpawnPoint();
            if (_clampSource == ClampSource.TilemapBounds)
                RefreshClampFromTilemap();
        }

        private void OnValidate()
        {
            if (_clampMinLocal.x > _clampMaxLocal.x)
                (_clampMinLocal.x, _clampMaxLocal.x) = (_clampMaxLocal.x, _clampMinLocal.x);
            if (_clampMinLocal.y > _clampMaxLocal.y)
                (_clampMinLocal.y, _clampMaxLocal.y) = (_clampMaxLocal.y, _clampMinLocal.y);
            if (_enemySpawnYMinLocal > _enemySpawnYMaxLocal)
                (_enemySpawnYMinLocal, _enemySpawnYMaxLocal) = (_enemySpawnYMaxLocal, _enemySpawnYMinLocal);
        }

        /// <summary>월드 좌표를 맵 clamp 안으로 제한한다.</summary>
        public Vector3 ClampPosition(Vector3 worldPosition)
        {
            EnsureClampCache();

            var local = transform.InverseTransformPoint(worldPosition);
            local.x = Mathf.Clamp(local.x, _clampMinLocal.x, _clampMaxLocal.x);
            local.y = Mathf.Clamp(local.y, _clampMinLocal.y, _clampMaxLocal.y);
            local.z = 0f;
            return transform.TransformPoint(local);
        }

        /// <summary>
        /// 카메라 중심을 맵 안으로 제한한다.
        /// halfW/halfH만큼 안쪽으로 줄여 화면 가장자리가 clamp를 넘지 않게 한다.
        /// 맵이 화면보다 작으면 맵 중심에 고정한다.
        /// </summary>
        public Vector3 ClampCameraCenter(Vector3 desiredWorldCenter, float halfW, float halfH)
        {
            EnsureClampCache();

            halfW = Mathf.Max(0f, halfW);
            halfH = Mathf.Max(0f, halfH);

            var local = transform.InverseTransformPoint(desiredWorldCenter);
            var minX = _clampMinLocal.x + halfW;
            var maxX = _clampMaxLocal.x - halfW;
            var minY = _clampMinLocal.y + halfH;
            var maxY = _clampMaxLocal.y - halfH;

            if (minX > maxX)
                local.x = (_clampMinLocal.x + _clampMaxLocal.x) * 0.5f;
            else
                local.x = Mathf.Clamp(local.x, minX, maxX);

            if (minY > maxY)
                local.y = (_clampMinLocal.y + _clampMaxLocal.y) * 0.5f;
            else
                local.y = Mathf.Clamp(local.y, minY, maxY);

            var world = transform.TransformPoint(local);
            world.z = desiredWorldCenter.z;
            return world;
        }

        public bool ContainsWorldPosition(Vector3 worldPosition)
        {
            EnsureClampCache();
            var local = transform.InverseTransformPoint(worldPosition);
            return local.x >= _clampMinLocal.x && local.x <= _clampMaxLocal.x &&
                   local.y >= _clampMinLocal.y && local.y <= _clampMaxLocal.y;
        }

        /// <summary>플레이어 스폰 월드 좌표. 스폰 포인트가 없으면 맵 원점.</summary>
        public Vector3 GetPlayerSpawnWorldPosition()
        {
            EnsureSpawnPoint();
            if (_playerSpawnPoint != null)
                return _playerSpawnPoint.position;

            return transform.position;
        }

        /// <summary>적 스폰용 Y 로컬 구간.</summary>
        public void GetEnemySpawnYLocal(out float minY, out float maxY)
        {
            minY = EnemySpawnYMinLocal;
            maxY = EnemySpawnYMaxLocal;
        }

        /// <summary>적 스폰 Y 랜덤 (월드). 구간이 비면 맵 clamp Y 중앙.</summary>
        public float GetRandomEnemySpawnYWorld()
        {
            var minY = EnemySpawnYMinLocal;
            var maxY = EnemySpawnYMaxLocal;
            var localY = Mathf.Approximately(minY, maxY)
                ? minY
                : Random.Range(minY, maxY);
            var world = transform.TransformPoint(new Vector3(0f, localY, 0f));
            return world.y;
        }

        /// <summary>적 스폰 Y 구간 중앙 (월드). 보스 스폰용.</summary>
        public float GetEnemySpawnCenterYWorld()
        {
            var localY = (EnemySpawnYMinLocal + EnemySpawnYMaxLocal) * 0.5f;
            return transform.TransformPoint(new Vector3(0f, localY, 0f)).y;
        }

        /// <summary>월드 Y를 적 스폰 레인 로컬 Y로 클램프한 월드 좌표.</summary>
        public Vector3 ClampEnemySpawnY(Vector3 worldPosition)
        {
            var local = transform.InverseTransformPoint(worldPosition);
            local.y = Mathf.Clamp(local.y, EnemySpawnYMinLocal, EnemySpawnYMaxLocal);
            local.z = 0f;
            return transform.TransformPoint(local);
        }

        /// <summary>맵 clamp 영역의 월드 중심. 카메라 고정용.</summary>
        public Vector3 GetClampCenterWorld(float worldZ = 0f)
        {
            EnsureClampCache();
            var local = new Vector3(
                (_clampMinLocal.x + _clampMaxLocal.x) * 0.5f,
                (_clampMinLocal.y + _clampMaxLocal.y) * 0.5f,
                0f);
            var world = transform.TransformPoint(local);
            world.z = worldZ;
            return world;
        }

        /// <summary>맵 clamp 로컬 min/max (읽기용).</summary>
        public void GetClampLocal(out Vector2 minLocal, out Vector2 maxLocal)
        {
            EnsureClampCache();
            minLocal = _clampMinLocal;
            maxLocal = _clampMaxLocal;
        }

        /// <summary>자식 Tilemap cellBounds로 수동 clamp를 채운다.</summary>
        [ContextMenu("Refresh Clamp From Tilemap")]
        public void RefreshClampFromTilemap()
        {
            var tilemap = _boundsTilemap != null
                ? _boundsTilemap
                : GetComponentInChildren<Tilemap>(true);

            if (tilemap == null)
            {
                Debug.LogWarning($"[SurvivorsRunMap] Tilemap을 찾지 못했습니다: {name}", this);
                return;
            }

            _boundsTilemap = tilemap;
            var bounds = tilemap.localBounds;
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            // 타일맵 로컬 AABB 네 모서리를 맵 루트 로컬로 변환
            var corners = new Vector3[]
            {
                new(bounds.min.x, bounds.min.y, 0f),
                new(bounds.min.x, bounds.max.y, 0f),
                new(bounds.max.x, bounds.min.y, 0f),
                new(bounds.max.x, bounds.max.y, 0f),
            };

            for (var i = 0; i < corners.Length; i++)
            {
                var world = tilemap.transform.TransformPoint(corners[i]);
                var local = transform.InverseTransformPoint(world);
                min = Vector2.Min(min, new Vector2(local.x, local.y));
                max = Vector2.Max(max, new Vector2(local.x, local.y));
            }

            _clampMinLocal = min - _tilemapPadding;
            _clampMaxLocal = max + _tilemapPadding;
        }

        private void EnsureClampCache()
        {
            if (_clampSource == ClampSource.TilemapBounds && Application.isPlaying)
            {
                // 런타임 첫 접근 시 타일맵이 늦게 준비될 수 있어 한 번 갱신
                if (_boundsTilemap == null)
                    RefreshClampFromTilemap();
            }
        }

        private void EnsureSpawnPoint()
        {
            if (_playerSpawnPoint != null)
                return;

            var existing = transform.Find("PlayerSpawn");
            if (existing != null)
            {
                _playerSpawnPoint = existing;
                return;
            }

            var go = new GameObject("PlayerSpawn");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            _playerSpawnPoint = go.transform;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var min = _clampMinLocal;
            var max = _clampMaxLocal;
            var bl = transform.TransformPoint(new Vector3(min.x, min.y, 0f));
            var br = transform.TransformPoint(new Vector3(max.x, min.y, 0f));
            var tr = transform.TransformPoint(new Vector3(max.x, max.y, 0f));
            var tl = transform.TransformPoint(new Vector3(min.x, max.y, 0f));

            Gizmos.color = new Color(0.2f, 0.85f, 0.45f, 0.9f);
            Gizmos.DrawLine(bl, br);
            Gizmos.DrawLine(br, tr);
            Gizmos.DrawLine(tr, tl);
            Gizmos.DrawLine(tl, bl);

            if (_playerSpawnPoint != null)
            {
                Gizmos.color = new Color(0.95f, 0.75f, 0.2f, 0.95f);
                Gizmos.DrawWireSphere(_playerSpawnPoint.position, 0.35f);
            }

            // 적 스폰 Y 레인
            var yMin = EnemySpawnYMinLocal;
            var yMax = EnemySpawnYMaxLocal;
            var left = min.x;
            var right = max.x;
            var bl2 = transform.TransformPoint(new Vector3(left, yMin, 0f));
            var br2 = transform.TransformPoint(new Vector3(right, yMin, 0f));
            var tr2 = transform.TransformPoint(new Vector3(right, yMax, 0f));
            var tl2 = transform.TransformPoint(new Vector3(left, yMax, 0f));
            Gizmos.color = new Color(1f, 0.35f, 0.25f, 0.85f);
            Gizmos.DrawLine(bl2, br2);
            Gizmos.DrawLine(br2, tr2);
            Gizmos.DrawLine(tr2, tl2);
            Gizmos.DrawLine(tl2, bl2);
        }
#endif
    }
}
