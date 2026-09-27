using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SHIN
{
    /// <summary>
    /// SurvivorsRun 입력: ActionMap 전환 + 적 클릭 타겟 지정.
    /// Player Settings가 Input System Only이므로 UnityEngine.Input 대신 Input System API를 쓴다.
    /// </summary>
    public partial class SurvivorsRunManager
    {
        private bool _playerControlEnabled;
        private InputManager _inputManager;

        private void Update()
        {
            TickTargetClick();
        }

        private void SetPlayerControlEnabled(bool enabled)
        {
            _playerControlEnabled = enabled;
        }

        /// <summary>
        /// PC 클릭 / 모바일 터치 공통. 적을 누르면 Manager 타겟으로 지정한다.
        /// </summary>
        private void TickTargetClick()
        {
            if (_playerUnit == null || _playerUnit.IsDead)
                return;

            if (_timeScale <= 0f)
                return;

            if (_playerInfo != null && _playerInfo.Life <= 0)
                return;

            if (!TryGetPointerDownScreenPosition(out var screenPos))
                return;

            if (IsPointerOverUi())
                return;

            EnsureEnemyRuntimeRefs();
            var cam = _runCamera != null ? _runCamera : Camera.main;
            if (cam == null)
                return;

            var screen = new Vector3(screenPos.x, screenPos.y, Mathf.Abs(cam.transform.position.z));
            var world = cam.ScreenToWorldPoint(screen);
            world.z = 0f;

            var enemy = FindEnemyAtWorldPoint(world);
            if (enemy != null)
                enemy.NotifyPlayerClick();
            else
                ClearAttackTarget();
        }

        private static bool TryGetPointerDownScreenPosition(out Vector2 screenPos)
        {
            screenPos = default;

            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var primary = touchscreen.primaryTouch;
                if (primary.press.wasPressedThisFrame)
                {
                    screenPos = primary.position.ReadValue();
                    return true;
                }
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                screenPos = mouse.position.ReadValue();
                return true;
            }

            return false;
        }

        private static bool IsPointerOverUi()
        {
            if (EventSystem.current == null)
                return false;

            // Input System UI 모듈: 인자 없는 호출이 현재 포인터 기준으로 동작한다.
            return EventSystem.current.IsPointerOverGameObject();
        }

        private static SurvivorsRunEnemyBase FindEnemyAtWorldPoint(Vector3 world)
        {
            var hits = Physics2D.OverlapPointAll(world);
            SurvivorsRunEnemyBase found = null;
            var unitLayer = LayerMask.NameToLayer(PublicVariable.Layer.Unit);

            for (var i = 0; i < hits.Length; i++)
            {
                var col = hits[i];
                if (col == null)
                    continue;

                var enemy = col.GetComponent<SurvivorsRunEnemyBase>();
                if (enemy == null)
                    enemy = col.GetComponentInParent<SurvivorsRunEnemyBase>();

                if (enemy == null || enemy.IsDead)
                    continue;

                if (unitLayer >= 0 && col.gameObject.layer == unitLayer)
                    return enemy;

                found ??= enemy;
            }

            return found;
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
