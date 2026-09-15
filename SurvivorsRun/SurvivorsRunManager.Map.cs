using System.Threading.Tasks;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// MapSO 로드 후 맵 프리팹 생성. clamp·스폰은 <see cref="SurvivorsRunMap"/>가 소유한다.
    /// </summary>
    public partial class SurvivorsRunManager
    {
        [SerializeField] private string _mapSoAddress = PublicVariable.Address.SurvivorsRunMapSO;
        [SerializeField]
        [Tooltip("비우면 MapSO의 GetDefaultMap()을 쓴다. 선택 규칙 붙이기 전 임시 고정용.")]
        private string _forcedMapId;

        private SurvivorsRunMapSO _mapSo;
        private SurvivorsRunMapData _selectedMapData;
        private SurvivorsRunMap _activeMap;
        private GameObject _mapInstance;

        public SurvivorsRunMapSO MapSo => _mapSo;
        public SurvivorsRunMapData SelectedMapData => _selectedMapData;
        public SurvivorsRunMap ActiveMap => _activeMap;

        /// <summary>
        /// MapSO를 로드하고, 선택된 맵 프리팹을 생성한다.
        /// 이미 세션 자식에 Map이 있으면 그걸 쓰고 SO만 맞춰 둔다.
        /// </summary>
        public async Task EnsureMapAsync()
        {
            if (_activeMap != null)
                return;

            await EnsureMapSoAsync();

            _activeMap = GetComponentInChildren<SurvivorsRunMap>(true);
            if (_activeMap != null)
            {
                _mapInstance = _activeMap.gameObject;
                _selectedMapData ??= ResolveMapData();
                return;
            }

            var mapData = ResolveMapData();
            if (mapData == null)
            {
                Debug.LogError("[SurvivorsRunManager] 생성할 맵 데이터를 찾지 못했습니다.");
                return;
            }

            if (string.IsNullOrWhiteSpace(mapData.MapPrefabPath))
            {
                Debug.LogError($"[SurvivorsRunManager] 맵 프리팹 경로가 비어 있습니다: {mapData.MapId}");
                return;
            }

            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager == null)
            {
                Debug.LogError("[SurvivorsRunManager] ResourceManager가 없어 맵을 생성할 수 없습니다.");
                return;
            }

            _selectedMapData = mapData;

            var instance = await resourceManager.InstantiateAsync(
                mapData.MapPrefabPath.Trim(),
                parent: transform,
                startInactive: false);

            if (this == null)
            {
                if (instance != null)
                    resourceManager.ReleaseInstance(instance);
                return;
            }

            if (instance == null)
            {
                Debug.LogError($"[SurvivorsRunManager] 맵 생성 실패: {mapData.MapPrefabPath}");
                _selectedMapData = null;
                return;
            }

            _mapInstance = instance;
            _activeMap = instance.GetComponent<SurvivorsRunMap>();
            if (_activeMap == null)
                _activeMap = instance.GetComponentInChildren<SurvivorsRunMap>(true);

            if (_activeMap == null)
            {
                Debug.LogError($"[SurvivorsRunManager] 맵 프리팹에 SurvivorsRunMap이 없습니다: {mapData.MapId}");
                resourceManager.ReleaseInstance(instance);
                _mapInstance = null;
                _selectedMapData = null;
            }
        }

        private async Task EnsureMapSoAsync()
        {
            if (_mapSo != null)
                return;

            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager == null)
            {
                Debug.LogError("[SurvivorsRunManager] ResourceManager가 없어 MapSO를 로드할 수 없습니다.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_mapSoAddress))
            {
                Debug.LogError("[SurvivorsRunManager] MapSO 주소가 비어 있습니다.");
                return;
            }

            _mapSo = await resourceManager.LoadAsync<SurvivorsRunMapSO>(_mapSoAddress.Trim());
            if (_mapSo == null)
                Debug.LogError($"[SurvivorsRunManager] SurvivorsRunMapSO 로드 실패: {_mapSoAddress}");
        }

        /// <summary>선택 규칙 자리. 지금은 forcedMapId → DefaultMap.</summary>
        private SurvivorsRunMapData ResolveMapData()
        {
            if (_mapSo == null)
                return null;

            if (!string.IsNullOrWhiteSpace(_forcedMapId))
                return _mapSo.GetByMapId(_forcedMapId.Trim());

            return _mapSo.GetDefaultMap();
        }

        public Vector3 GetPlayerSpawnWorldPosition()
        {
            if (_activeMap != null)
                return _activeMap.GetPlayerSpawnWorldPosition();

            return transform.position;
        }

        private void ReleaseMapInstance()
        {
            if (_mapInstance == null)
            {
                _activeMap = null;
                _selectedMapData = null;
                return;
            }

            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager != null)
                resourceManager.ReleaseInstance(_mapInstance);
            else
                Destroy(_mapInstance);

            _mapInstance = null;
            _activeMap = null;
            _selectedMapData = null;
        }
    }
}
