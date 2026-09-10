using UnityEngine;
using UnityEngine.InputSystem;

namespace SHIN
{
    /// <summary>
    /// 전역 입력. ActionMap 전환은 여기로 모은다. (.inputactions는 Addressables 없이 직접 참조)
    /// </summary>
    public class InputManager : ManagerBase
    {
        public static class ActionMapName
        {
            public const string UI = "UI";
            public const string BS = "BS";
            public const string SurvivorsRun = "SurvivorsRun";
            public const string Poker = "Poker";
        }

        [SerializeField] private InputActionAsset _actions;

        private InputActionAsset _runtimeActions;
        private string _currentMap;
        private bool _initialized;

        public InputActionAsset Actions => _runtimeActions != null ? _runtimeActions : _actions;
        public string CurrentMap => _currentMap;
        public bool HasActions => Actions != null;

        private void Awake()
        {
            // GameManager가 SetActionsAsset으로 넣을 수 있으므로, 있을 때만 즉시 초기화
            if (_actions != null)
                TryInitialize();
        }

        /// <summary>
        /// GameManager 등에서 에셋 참조를 넘겨줄 때 사용.
        /// </summary>
        public void SetActionsAsset(InputActionAsset actionsAsset)
        {
            if (actionsAsset == null)
                return;

            if (_actions == actionsAsset && _initialized)
                return;

            _actions = actionsAsset;
            _initialized = false;
            TryInitialize();
        }

        public void EnableMap(string mapName)
        {
            TryInitialize();
            if (_runtimeActions == null || string.IsNullOrWhiteSpace(mapName))
                return;

            mapName = mapName.Trim();
            var found = false;
            foreach (var map in _runtimeActions.actionMaps)
            {
                if (map.name == mapName)
                {
                    map.Enable();
                    found = true;
                }
                else
                {
                    map.Disable();
                }
            }

            if (!found)
            {
                Debug.LogWarning($"[InputManager] ActionMap을 찾을 수 없습니다: {mapName}");
                return;
            }

            _currentMap = mapName;
        }

        public InputActionMap GetMap(string mapName)
        {
            TryInitialize();
            if (_runtimeActions == null || string.IsNullOrWhiteSpace(mapName))
                return null;

            return _runtimeActions.FindActionMap(mapName.Trim(), throwIfNotFound: false);
        }

        public InputAction GetAction(string actionPath)
        {
            TryInitialize();
            if (_runtimeActions == null || string.IsNullOrWhiteSpace(actionPath))
                return null;

            return _runtimeActions.FindAction(actionPath.Trim(), throwIfNotFound: false);
        }

        public InputAction GetAction(string mapName, string actionName)
        {
            var map = GetMap(mapName);
            if (map == null || string.IsNullOrWhiteSpace(actionName))
                return null;

            return map.FindAction(actionName.Trim(), throwIfNotFound: false);
        }

        public TValue ReadValue<TValue>(string mapName, string actionName)
            where TValue : struct
        {
            var action = GetAction(mapName, actionName);
            if (action == null)
                return default;

            return action.ReadValue<TValue>();
        }

        private void TryInitialize()
        {
            if (_initialized)
                return;

            if (_actions == null)
            {
                Debug.LogError("[InputManager] InputActionAsset이 없습니다. GameManager 또는 InputManager에 PlayerInput.inputactions를 할당하세요.");
                return;
            }

            if (_runtimeActions != null)
            {
                _runtimeActions.Disable();
                Destroy(_runtimeActions);
                _runtimeActions = null;
            }

            // 원본 에셋을 런타임에 Enable/Disable하지 않도록 복사본 사용
            _runtimeActions = Instantiate(_actions);
            _runtimeActions.name = _actions.name + " (Runtime)";
            _initialized = true;
            EnableMap(ActionMapName.UI);
        }

        private void OnDestroy()
        {
            if (_runtimeActions == null)
                return;

            _runtimeActions.Disable();
            Destroy(_runtimeActions);
            _runtimeActions = null;
        }

#if UNITY_EDITOR
        private void Reset()
        {
            if (_actions != null)
                return;

            _actions = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/Input/PlayerInput.inputactions");
        }
#endif
    }
}
