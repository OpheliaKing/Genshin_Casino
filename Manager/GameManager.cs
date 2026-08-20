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

            InGameManager.StartMatch(opponentData);
        }
    }
}
