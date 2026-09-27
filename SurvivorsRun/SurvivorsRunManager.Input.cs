using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// SurvivorsRun 입력: ActionMap 전환.
    /// 고정 디펜스에서는 Move 폴링을 쓰지 않는다. (타겟 클릭·스킬 버튼은 이후 추가)
    /// </summary>
    public partial class SurvivorsRunManager
    {
        private bool _playerControlEnabled;
        private InputManager _inputManager;

        private void Update()
        {
            TickPlayerInput();
        }

        private void SetPlayerControlEnabled(bool enabled)
        {
            _playerControlEnabled = enabled;
        }

        private void TickPlayerInput()
        {
            // 이동 입력은 비활성. 클릭/버튼 인풋은 이후 단계에서 연결한다.
            if (!_playerControlEnabled || _playerUnit == null || _playerUnit.IsDead)
                return;

            if (_timeScale <= 0f)
                return;
        }

        private void EnableInputMap(string mapName)
        {
            var gameManager = GameManager.Instance;
            _inputManager = gameManager != null ? gameManager.InputManager : null;
            if (_inputManager == null)
            {
                Debug.LogError("[SurvivorsRunManager] InputManager가 없어 입력을 전환할 수 없습니다.");
                return;
            }

            _inputManager.EnableMap(mapName);
        }

        private void RestoreLobbyInputMap()
        {
            var gameManager = GameManager.Instance;
            var inputManager = gameManager != null ? gameManager.InputManager : null;
            if (inputManager == null)
                return;

            if (inputManager.CurrentMap == InputManager.ActionMapName.SurvivorsRun ||
                inputManager.CurrentMap == InputManager.ActionMapName.UI)
            {
                inputManager.EnableMap(InputManager.ActionMapName.UI);
            }
        }
    }
}
