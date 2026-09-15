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

        public ClampSource Source => _clampSource;
        public Transform PlayerSpawnPoint => _playerSpawnPoint;

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
        }
#endif
    }
}
