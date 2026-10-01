using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SHIN
{
    /// <summary>
    /// 유일한 싱글톤. 하위 매니저는 프로퍼티 접근 시 EnsureManager로 준비한다.
    /// 포커 등 모드 세션은 여기에 캐시하지 않는다.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [SerializeField] private ResourceManager _resourceManager;
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private SoundManager _soundManager;
        [SerializeField] private InputManager _inputManager;
        /// <summary>전역 Input Actions. Addressables 없이 직접 참조.</summary>
        [SerializeField] private InputActionAsset _inputActions;
        /// <summary>타이틀 BGM Addressables 주소. 비어 있으면 재생하지 않는다.</summary>
        [SerializeField] private string _titleBgmAddress = "";

        private PlayerData _playerData;
        private Task<PlayerData> _playerDataLoadTask;
        private bool _bootStarted;
        private bool _transitionToMainStarted;

        public ResourceManager ResourceManager
        {
            get
            {
                ManagerBase.EnsureManager(transform, ref _resourceManager);
                return _resourceManager;
            }
        }

        public UIManager UIManager
        {
            get
            {
                ManagerBase.EnsureManager(transform, ref _uiManager);
                return _uiManager;
            }
        }

        public SoundManager SoundManager
        {
            get
            {
                ManagerBase.EnsureManager(transform, ref _soundManager);
                return _soundManager;
            }
        }

        public InputManager InputManager
        {
            get
            {
                ManagerBase.EnsureManager(transform, ref _inputManager);
                if (_inputManager != null && _inputActions != null)
                    _inputManager.SetActionsAsset(_inputActions);
                return _inputManager;
            }
        }

        public PlayerData PlayerData => _playerData;

        public event System.Action<int> GoldChanged;

        public Task<PlayerData> EnsurePlayerDataAsync()
        {
            if (_playerData != null)
                return Task.FromResult(_playerData);

            if (_playerDataLoadTask != null)
                return _playerDataLoadTask;

            _playerDataLoadTask = LoadPlayerDataAsync();
            return _playerDataLoadTask;
        }

        /// <summary>공용 칩(haveGold) 변경 후 디스크 세이브.</summary>
        public int AddGold(int delta)
        {
            if (_playerData == null || delta == 0)
                return _playerData != null ? _playerData.haveGold : 0;

            _playerData.haveGold = Mathf.Max(0, _playerData.haveGold + delta);
            GoldChanged?.Invoke(_playerData.haveGold);
            SavePlayerProgress();
            return _playerData.haveGold;
        }

        public int SetGold(int value)
        {
            if (_playerData == null)
                return 0;

            _playerData.haveGold = Mathf.Max(0, value);
            GoldChanged?.Invoke(_playerData.haveGold);
            SavePlayerProgress();
            return _playerData.haveGold;
        }

        private void SavePlayerProgress()
        {
            if (_playerData == null)
                return;

            try
            {
                var path = GetPlayerSavePath();
                var save = new PlayerProgressSave
                {
                    haveGold = _playerData.haveGold
                };
                var json = JsonUtility.ToJson(save, prettyPrint: true);
                System.IO.File.WriteAllText(path, json);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[GameManager] 세이브 실패: {e.Message}");
            }
        }

        private void ApplyPlayerProgressSave()
        {
            if (_playerData == null)
                return;

            try
            {
                var path = GetPlayerSavePath();
                if (!System.IO.File.Exists(path))
                    return;

                var json = System.IO.File.ReadAllText(path);
                var save = JsonUtility.FromJson<PlayerProgressSave>(json);
                if (save == null)
                    return;

                _playerData.haveGold = Mathf.Max(0, save.haveGold);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[GameManager] 세이브 로드 실패: {e.Message}");
            }
        }

        private static string GetPlayerSavePath()
        {
            return System.IO.Path.Combine(Application.persistentDataPath, "player_progress.json");
        }

        [System.Serializable]
        private class PlayerProgressSave
        {
            public int haveGold;
        }

        private async Task<PlayerData> LoadPlayerDataAsync()
        {
            try
            {
                var resourceManager = ResourceManager;
                if (resourceManager == null)
                    return null;

                var so = await resourceManager.LoadAsync<PlayerDataSO>(PublicVariable.Address.PlayerDataSO);
                if (so == null || so.Player == null)
                {
                    Debug.LogError("[GameManager] PlayerDataSO 로드에 실패했습니다.");
                    return null;
                }

                _playerData = so.Player;
                ApplyPlayerProgressSave();
                return _playerData;
            }
            finally
            {
                if (_playerData == null)
                    _playerDataLoadTask = null;
            }
        }

        private void Start()
        {
            ApplyBootBlackout();
            if (_bootStarted)
                return;

            _bootStarted = true;
            _ = BootAsync();
        }

        /// <summary>
        /// 첫 프레임 플래시를 줄이기 위해 카메라 클리어를 검정으로 맞춘다.
        /// </summary>
        private void ApplyBootBlackout()
        {
            var cam = Camera.main;
            if (cam == null)
                return;

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
        }

        private async Task BootAsync()
        {
            // 부팅 시 UI ActionMap 준비
            var inputManager = InputManager;
            inputManager?.EnableMap(SHIN.InputManager.ActionMapName.UI);

            await UIManager.FadeToAsync(1f, 0f);
            if (this == null)
                return;

            await PreloadBootResourcesAsync();
            if (this == null)
                return;

            var startReady = new TaskCompletionSource<StartUI>();
            UIManager.Show(PublicVariable.Address.StartUI, ui =>
            {
                startReady.TrySetResult(ui as StartUI);
            });

            var startUI = await startReady.Task;
            if (this == null)
                return;

            TryPlayTitleBgm();

            await UIManager.FadeInAsync();
            if (this == null)
                return;

            startUI?.SetInputEnabled(true);
        }

        private async Task PreloadBootResourcesAsync()
        {
            var resourceManager = ResourceManager;
            if (resourceManager == null)
                return;

            var tasks = new List<Task>
            {
                resourceManager.PreloadLabelAsync(PublicVariable.Label.Preload),
                resourceManager.LoadAsync<GameObject>(PublicVariable.Address.StartUI),
                resourceManager.LoadAsync<GameObject>(PublicVariable.Address.MainUI),
                resourceManager.LoadAsync<GameObject>(PublicVariable.Address.FadeUI),
                EnsurePlayerDataAsync()
            };

            if (!string.IsNullOrWhiteSpace(_titleBgmAddress))
                tasks.Add(SoundManager.PreloadAsync(new[] { _titleBgmAddress.Trim() }));

            await Task.WhenAll(tasks);
        }

        private void TryPlayTitleBgm()
        {
            if (string.IsNullOrWhiteSpace(_titleBgmAddress))
                return;

            SoundManager.PlayBgm(_titleBgmAddress.Trim());
        }

        /// <summary>StartUI에서 클릭/터치 시 MainUI로 전환한다.</summary>
        public void GoToMainFromStart()
        {
            if (_transitionToMainStarted)
                return;

            _transitionToMainStarted = true;
            _ = GoToMainFromStartAsync();
        }

        private async Task GoToMainFromStartAsync()
        {
            await UIManager.FadeTransitionAsync(async () =>
            {
                var current = UIManager.Current;
                if (current != null)
                    UIManager.Close(current, restoreVisibleStack: false);

                var mainReady = new TaskCompletionSource<bool>();
                UIManager.Show(PublicVariable.Address.MainUI, _ => mainReady.TrySetResult(true));
                await mainReady.Task;
            });
        }
    }
}
