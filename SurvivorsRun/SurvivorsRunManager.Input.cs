using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// SurvivorsRun 입력: ActionMap 전환 + 플레이어 Move 폴링.
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
            if (!_playerControlEnabled || _playerUnit == null || _playerUnit.IsDead)
                return;

            if (_timeScale <= 0f)
                return;

            if (_inputManager == null)
                _inputManager = GameManager.Instance?.InputManager;

            if (_inputManager == null)
                return;

            var move = _inputManager.ReadValue<Vector2>(
                InputManager.ActionMapName.SurvivorsRun,
                InputManager.SurvivorsRunAction.Move);

            _playerUnit.Move(move);
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
