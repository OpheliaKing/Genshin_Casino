using System;
using System.Threading.Tasks;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 타이틀 이후 메인(로비) 화면. 메뉴 버튼 등은 이후 연결.
    /// </summary>
    public class MainUI : UIBase
    {
        private bool _transitionStarted;

        public override void OnShow()
        {
            _transitionStarted = false;
        }

        public void OnClickInGameStartButton()
        {
            _ = TransitionFromMainAsync(EnterOpponentSelectAsync);
        }

        public void OnClickSurvivorsRunButton()
        {
            Debug.Log("OnClickSurvivorsRunButton");
            _ = TransitionFromMainAsync(EnterSurvivorsRunAsync);
        }

        /// <summary>
        /// 미니게임 공통: 페이드 아웃 → midAction → 페이드 인.
        /// </summary>
        private async Task TransitionFromMainAsync(Func<GameManager, UIManager, Task> midAction)
        {
            if (_transitionStarted)
                return;

            _transitionStarted = true;

            var gameManager = GameManager.Instance;
            var uiManager = gameManager?.UIManager;
            if (uiManager == null || midAction == null)
            {
                _transitionStarted = false;
                Debug.LogError("[MainUI] UIManager가 없거나 midAction이 null입니다.");
                return;
            }

            try
            {
                await uiManager.FadeTransitionAsync(async () =>
                {
                    await midAction(gameManager, uiManager);
                });
            }
            finally
            {
                if (this != null)
                    _transitionStarted = false;
            }
        }

        private async Task EnterOpponentSelectAsync(GameManager gameManager, UIManager uiManager)
        {
            var resourceManager = gameManager.ResourceManager;
            if (resourceManager != null)
                await resourceManager.PreloadLabelAsync(PublicVariable.Label.OpponentSelect);

            uiManager.Close(this, restoreVisibleStack: false);

            var shown = new TaskCompletionSource<OpponentSelectUI>();
            uiManager.Show(PublicVariable.Address.OpponentSelectUI, ui =>
            {
                shown.TrySetResult(ui as OpponentSelectUI);
            });

            var selectUI = await shown.Task;
            if (selectUI != null)
                await selectUI.WaitUntilReadyAsync();
        }

        private async Task EnterSurvivorsRunAsync(GameManager gameManager, UIManager uiManager)
        {
            var resourceManager = gameManager.ResourceManager;
            if (resourceManager == null)
            {
                Debug.LogError("[MainUI] ResourceManager가 없습니다.");
                return;
            }

            await resourceManager.PreloadLabelAsync(PublicVariable.Label.Survivors);

            uiManager.Close(this, restoreVisibleStack: false);

            var session = await resourceManager.InstantiateAsync(
                PublicVariable.Address.SurvivorsRunSession,
                parent: null,
                startInactive: false);

            if (session == null)
            {
                Debug.LogError("[MainUI] SurvivorsRunSession 생성에 실패했습니다.");
                return;
            }

            var manager = session.GetComponent<SurvivorsRunManager>();
            if (manager == null)
                manager = session.GetComponentInChildren<SurvivorsRunManager>(true);

            if (manager == null)
            {
                Debug.LogError("[MainUI] SurvivorsRunManager가 없습니다.");
                return;
            }

            await manager.InitAsync();
        }
    }
}
