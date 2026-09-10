using System.Threading.Tasks;
using UnityEngine;

namespace SHIN
{
    public class SurvivorsRunManager : MonoBehaviour
    {
        private float _timeScale = 1f;
        private SurvivorsRunCombat _combat;
        private SurvivorsRunCharacterSelectUI _characterSelectUI;
        private SurvivorsRunUnitData _selectedCharacter;

        public float TimeScale => _timeScale;
        public SurvivorsRunCombat Combat => _combat;
        public SurvivorsRunUnitData SelectedCharacter => _selectedCharacter;

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
            // 캐릭터 선택은 UI 조작이므로 UI ActionMap 사용
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

            _selectedCharacter = data;
            EnableInputMap(InputManager.ActionMapName.SurvivorsRun);

            var uiManager = GameManager.Instance?.UIManager;
            if (uiManager != null && _characterSelectUI != null)
            {
                uiManager.Close(_characterSelectUI, restoreVisibleStack: false);
                _characterSelectUI = null;
            }

            // 이후 플레이어 스폰 등은 여기서 이어가면 된다.
            Debug.Log($"[SurvivorsRunManager] 캐릭터 선택: {data.UnitId} / {data.UnitName}");
        }

        private static void EnableInputMap(string mapName)
        {
            var gameManager = GameManager.Instance;
            var inputManager = gameManager != null ? gameManager.InputManager : null;
            if (inputManager == null)
            {
                Debug.LogError("[SurvivorsRunManager] InputManager가 없어 입력을 전환할 수 없습니다.");
                return;
            }

            inputManager.EnableMap(mapName);
        }

        private void OnDestroy()
        {
            var gameManager = GameManager.Instance;
            var inputManager = gameManager != null ? gameManager.InputManager : null;
            if (inputManager == null)
                return;

            if (inputManager.CurrentMap == InputManager.ActionMapName.SurvivorsRun ||
                inputManager.CurrentMap == InputManager.ActionMapName.UI)
            {
                // 로비로 돌아갈 때 UI 맵으로 복귀
                inputManager.EnableMap(InputManager.ActionMapName.UI);
            }
        }
    }
}
