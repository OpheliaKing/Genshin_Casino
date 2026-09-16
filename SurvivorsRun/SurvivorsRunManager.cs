using System.Threading.Tasks;
using UnityEngine;

namespace SHIN
{
    public partial class SurvivorsRunManager : MonoBehaviour
    {
        private float _timeScale = 1f;
        private SurvivorsRunCombat _combat;
        private SurvivorsRunCharacterSelectUI _characterSelectUI;
        private SurvivorsRunUnitData _selectedCharacter;
        private GameObject _playerInstance;
        private SurvivorsRunUnitBase _playerUnit;

        public float TimeScale => _timeScale;
        public SurvivorsRunCombat Combat => _combat;
        public SurvivorsRunUnitData SelectedCharacter => _selectedCharacter;
        public SurvivorsRunUnitBase PlayerUnit => _playerUnit;

        private void Awake()
        {
            _combat = new SurvivorsRunCombat(this);
        }

        public void SetTimeScale(float timeScale)
        {
            _timeScale = Mathf.Max(0f, timeScale);
        }

        /// <summary>
        /// 세션 진입 시 호출. 캐릭터 선택 UI를 띄우고 데이터를 채운다.
        /// </summary>
        public Task InitAsync()
        {
            return InitializeSessionAsync();
        }

        /// <summary>동기 진입용. 가능하면 InitAsync를 await 할 것.</summary>
        public void Init()
        {
            _ = InitAsync();
        }

        private async Task InitializeSessionAsync()
        {
            SetPlayerControlEnabled(false);
            EnableInputMap(InputManager.ActionMapName.UI);
            await ShowCharacterSelectAsync();
        }

        private async Task ShowCharacterSelectAsync()
        {
            var uiManager = GameManager.Instance?.UIManager;
            if (uiManager == null)
            {
                Debug.LogError("[SurvivorsRunManager] UIManager가 없습니다.");
                return;
            }

            var shown = new TaskCompletionSource<SurvivorsRunCharacterSelectUI>();
            uiManager.Show(PublicVariable.Address.SurvivorsRunCharacterSelectUI, ui =>
            {
                shown.TrySetResult(ui as SurvivorsRunCharacterSelectUI);
            });

            var selectUI = await shown.Task;
            if (selectUI == null)
            {
                Debug.LogError("[SurvivorsRunManager] SurvivorsRunCharacterSelectUI Show에 실패했습니다.");
                return;
            }

            _characterSelectUI = selectUI;
            await selectUI.SetupAsync(this);
        }

        public void OnCharacterSelected(SurvivorsRunUnitData data)
        {
            if (data == null)
                return;

            _ = OnCharacterSelectedAsync(data);
        }

        private async Task OnCharacterSelectedAsync(SurvivorsRunUnitData data)
        {
            _selectedCharacter = data;
            SetPlayerControlEnabled(false);

            var uiManager = GameManager.Instance?.UIManager;
            if (uiManager != null && _characterSelectUI != null)
            {
                uiManager.Close(_characterSelectUI, restoreVisibleStack: false);
                _characterSelectUI = null;
            }

            // 맵 → 플레이어(맵 스폰 위치) → 조작/적 스폰
            EnableInputMap(InputManager.ActionMapName.SurvivorsRun);
            await EnsureMapAsync();
            await SpawnSelectedCharacterAsync(data);

            if (_playerUnit != null)
            {
                BindCameraToPlayer();
                SetPlayerControlEnabled(true);
                await StartEnemySpawningAsync();
            }
        }

        private async Task SpawnSelectedCharacterAsync(SurvivorsRunUnitData data)
        {
            if (data == null)
                return;

            if (string.IsNullOrWhiteSpace(data.UnitPrefabPath))
            {
                Debug.LogError($"[SurvivorsRunManager] 프리팹 경로가 비어 있습니다: {data.UnitId}");
                return;
            }

            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager == null)
            {
                Debug.LogError("[SurvivorsRunManager] ResourceManager가 없습니다.");
                return;
            }

            ReleasePlayerInstance();

            var instance = await resourceManager.InstantiateAsync(
                data.UnitPrefabPath.Trim(),
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
                Debug.LogError($"[SurvivorsRunManager] 캐릭터 프리팹 생성 실패: {data.UnitPrefabPath}");
                return;
            }

            instance.transform.position = GetPlayerSpawnWorldPosition();
            _playerInstance = instance;

            _playerUnit = instance.GetComponent<SurvivorsRunUnitBase>();
            if (_playerUnit == null)
                _playerUnit = instance.GetComponentInChildren<SurvivorsRunUnitBase>(true);

            if (_playerUnit != null)
            {
                _playerUnit.BindManager(this);
                _playerUnit.Setup(
                    data.UnitId,
                    SURVIVORSRUN_UNIT_TYPE.PLAYER,
                    data.UnitHP,
                    Mathf.RoundToInt(data.UnitAttack),
                    data.UnitSpeed,
                    attackSpeed: 1f);
                _playerUnit.transform.position = ClampToMap(_playerUnit.transform.position);
                EnsurePlayerItemController(_playerUnit);
            }
            else
            {
                Debug.LogWarning("[SurvivorsRunManager] 생성된 프리팹에 SurvivorsRunUnitBase가 없습니다.");
            }

            instance.SetActive(true);
        }

        private static void EnsurePlayerItemController(SurvivorsRunUnitBase playerUnit)
        {
            if (playerUnit == null)
                return;

            var controller = playerUnit.GetComponent<SurvivorsRunPlayerItemController>();
            if (controller == null)
                controller = playerUnit.gameObject.AddComponent<SurvivorsRunPlayerItemController>();

            controller.BindOwner(playerUnit);
        }

        private void ReleasePlayerInstance()
        {
            if (_playerUnit != null)
            {
                var itemController = _playerUnit.GetComponent<SurvivorsRunPlayerItemController>();
                itemController?.ClearAll();
            }

            StopCameraFollow();
            SetPlayerControlEnabled(false);

            if (_playerInstance == null)
            {
                _playerUnit = null;
                return;
            }

            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager != null)
                resourceManager.ReleaseInstance(_playerInstance);
            else
                Destroy(_playerInstance);

            _playerInstance = null;
            _playerUnit = null;
        }

        private void OnDestroy()
        {
            ReleaseAllEnemies();
            ReleasePlayerInstance();
            ReleaseMapInstance();
            RestoreLobbyInputMap();
        }
    }
}
