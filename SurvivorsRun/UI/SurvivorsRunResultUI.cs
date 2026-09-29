using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SHIN
{
    /// <summary>Life 0 패배 결과. 다시하기 / 로비.</summary>
    public class SurvivorsRunResultUI : UIBase
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _detailText;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _lobbyButton;

        private SurvivorsRunManager _manager;
        private bool _built;

        public override void OnShow()
        {
            EnsureBuilt();
            SurvivorsRunUiBuild.ApplyKoreanFontRecursive(transform);
        }

        public void SetupDefeat(SurvivorsRunManager manager)
        {
            EnsureBuilt();
            _manager = manager;
            if (_titleText != null)
                _titleText.text = "패배";
            if (_detailText != null && manager?.PlayerInfo != null)
            {
                var info = manager.PlayerInfo;
                _detailText.text = $"Lv.{info.Level}  ·  Kills {info.KillCount}";
            }
        }

        private void EnsureBuilt()
        {
            if (_built)
                return;
            _built = true;

            SurvivorsRunUiBuild.StretchFull(gameObject);
            if (_retryButton != null && _lobbyButton != null)
            {
                Wire();
                return;
            }

            SurvivorsRunUiBuild.AddImage(gameObject, new Color(0f, 0f, 0f, 0.7f), raycast: true);

            var panel = SurvivorsRunUiBuild.Child(transform, "Panel");
            var panelRt = panel.GetComponent<RectTransform>();
            SurvivorsRunUiBuild.SetAnchored(panelRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 320f));
            SurvivorsRunUiBuild.AddImage(panel, new Color(0.14f, 0.1f, 0.12f, 0.96f), raycast: true);

            var titleGo = SurvivorsRunUiBuild.Child(panel.transform, "Title");
            SurvivorsRunUiBuild.SetAnchored(titleGo.GetComponent<RectTransform>(),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -36f), new Vector2(400f, 56f));
            _titleText = SurvivorsRunUiBuild.AddText(titleGo, "패배", 40f, TextAlignmentOptions.Center);

            var detailGo = SurvivorsRunUiBuild.Child(panel.transform, "Detail");
            SurvivorsRunUiBuild.SetAnchored(detailGo.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 24f), new Vector2(400f, 40f));
            _detailText = SurvivorsRunUiBuild.AddText(detailGo, string.Empty, 24f, TextAlignmentOptions.Center);

            _retryButton = CreateActionButton(panel.transform, "Retry", "다시하기", new Vector2(-110f, -90f));
            _lobbyButton = CreateActionButton(panel.transform, "Lobby", "로비", new Vector2(110f, -90f));
            Wire();
        }

        private void Wire()
        {
            if (_retryButton != null)
            {
                _retryButton.onClick.RemoveAllListeners();
                _retryButton.onClick.AddListener(() => _manager?.RetryAfterDefeat());
            }

            if (_lobbyButton != null)
            {
                _lobbyButton.onClick.RemoveAllListeners();
                _lobbyButton.onClick.AddListener(() => _manager?.ExitToLobby());
            }
        }

        private static Button CreateActionButton(Transform parent, string name, string label, Vector2 pos)
        {
            var go = SurvivorsRunUiBuild.Child(parent, name);
            SurvivorsRunUiBuild.SetAnchored(go.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                pos, new Vector2(180f, 56f));
            var button = SurvivorsRunUiBuild.AddButton(go, new Color(0.3f, 0.28f, 0.4f, 1f));
            var labelGo = SurvivorsRunUiBuild.Child(go.transform, "Label");
            SurvivorsRunUiBuild.StretchFull(labelGo);
            SurvivorsRunUiBuild.AddText(labelGo, label, 26f, TextAlignmentOptions.Center);
            return button;
        }
    }
}
