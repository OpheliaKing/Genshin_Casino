using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 유일한 싱글톤. 하위 매니저는 프로퍼티 접근 시 EnsureManager로 준비한다.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [SerializeField] private ResourceManager _resourceManager;
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private InGameManager _inGameManager;
        [SerializeField] private SoundManager _soundManager;

        private PlayerData _playerData;
        private Task<PlayerData> _playerDataLoadTask;

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

        public InGameManager InGameManager
        {
            get
            {
                ManagerBase.EnsureManager(transform, ref _inGameManager);
                return _inGameManager;
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

        public PlayerData PlayerData => _playerData;

        public Task<PlayerData> EnsurePlayerDataAsync()
        {
            if (_playerData != null)
                return Task.FromResult(_playerData);

            if (_playerDataLoadTask != null)
                return _playerDataLoadTask;

            _playerDataLoadTask = LoadPlayerDataAsync();
            return _playerDataLoadTask;
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
            TestShowOpponentSelectUI();
        }

        private void TestShowOpponentSelectUI()
        {
            UIManager.Show(PublicVariable.Address.OpponentSelectUI);
        }

        public void GameStart(OpponentData opponentData)
        {
            if (opponentData == null)
            {
                Debug.LogError("[GameManager] GameStart opponentData가 없습니다.");
                return;
            }

            _ = GameStartAsync(opponentData);
        }

        private async Task GameStartAsync(OpponentData opponentData)
        {
            await EnsurePlayerDataAsync();
            if (this == null)
                return;

            // Versus 연출과 인게임/캐릭터 프리로드를 병렬로 진행
            var preloadTask = PreloadMatchResourcesAsync(opponentData);

            var versusReady = new TaskCompletionSource<VersusUI>();
            UIManager.Show(PublicVariable.Address.VersusUI, ui =>
            {
                if (ui is VersusUI versusUI)
                    versusReady.TrySetResult(versusUI);
                else
                    versusReady.TrySetResult(null);
            });

            var versusUI = await versusReady.Task;
            if (this == null)
                return;

            if (versusUI == null)
            {
                Debug.LogError("[GameManager] VersusUI를 찾지 못했습니다.");
                await preloadTask;
                if (this == null)
                    return;

                await UIManager.FadeTransitionAsync(async () =>
                {
                    await InGameManager.EnterMatchAsync(opponentData);
                });
                if (this == null)
                    return;
                await InGameManager.BeginGameplayAsync();
                return;
            }

            var introDone = new TaskCompletionSource<bool>();
            versusUI.Begin(opponentData, () => introDone.TrySetResult(true));

            await introDone.Task;
            if (this == null)
                return;

            // 연출이 먼저 끝나도 프리로드 완료까지 Versus 유지
            await preloadTask;
            if (this == null)
                return;

            var versusToClose = versusUI;
            await UIManager.FadeTransitionAsync(async () =>
            {
                UIManager.Close(versusToClose, restoreVisibleStack: false);
                await InGameManager.EnterMatchAsync(opponentData);
            });
            if (this == null)
                return;
            await InGameManager.BeginGameplayAsync();
        }

        private async Task PreloadMatchResourcesAsync(OpponentData opponentData)
        {
            var playerData = await EnsurePlayerDataAsync();
            if (this == null)
                return;

            var uiPreload = UIManager.PreloadInGameUIAsync();
            var opponentPreload = PreloadOpponentAsync(opponentData);
            var voicePreload = PreloadMatchVoicesAsync(playerData, opponentData);
            await Task.WhenAll(uiPreload, opponentPreload, voicePreload);
        }

        private async Task PreloadMatchVoicesAsync(PlayerData playerData, OpponentData opponentData)
        {
            var soundManager = SoundManager;
            if (soundManager == null)
                return;

            var addresses = new List<string>
            {
                PublicVariable.Address.AnnouncerShowdown,
                PublicVariable.Address.AnnouncerWin,
                PublicVariable.Address.AnnouncerLose,
                PublicVariable.Address.InGameBgm
            };
            playerData?.CollectVoiceAddresses(addresses);
            opponentData?.CollectVoiceAddresses(addresses);

            await soundManager.PreloadAsync(addresses);
        }

        private async Task PreloadOpponentAsync(OpponentData opponentData)
        {
            if (opponentData == null)
                return;

            var resourceManager = ResourceManager;
            if (resourceManager == null)
                return;

            var tasks = new List<Task>();

            if (!string.IsNullOrEmpty(opponentData.modelPath))
                tasks.Add(resourceManager.LoadAsync<GameObject>(opponentData.modelPath));

            var atlasAddress = !string.IsNullOrEmpty(opponentData.atlasAddress)
                ? opponentData.atlasAddress
                : PublicVariable.Address.CharacterAtlas;
            if (!string.IsNullOrEmpty(atlasAddress))
                tasks.Add(resourceManager.LoadAsync<UnityEngine.U2D.SpriteAtlas>(atlasAddress));

            if (tasks.Count == 0)
                return;

            await Task.WhenAll(tasks);
        }
    }
}
