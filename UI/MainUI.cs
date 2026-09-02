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
            if (_transitionStarted)
                return;

            _transitionStarted = true;
            _ = OpenOpponentSelectAsync();
        }

        private async Task OpenOpponentSelectAsync()
        {
            var gameManager = GameManager.Instance;
            var uiManager = gameManager?.UIManager;
            if (uiManager == null)
            {
                _transitionStarted = false;
                Debug.LogError("[MainUI] UIManager가 없습니다.");
                return;
            }

            await uiManager.FadeTransitionAsync(async () =>
            {
                var resourceManager = gameManager?.ResourceManager;
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
            });

            if (this != null)
                _transitionStarted = false;
        }
    }
}
