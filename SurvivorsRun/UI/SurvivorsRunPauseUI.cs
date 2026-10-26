using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SHIN
{
    /// <summary>일시정지. 재개 / 포기(로비).</summary>
    public class SurvivorsRunPauseUI : UIBase
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _quitButton;

        private SurvivorsRunManager _manager;
        private bool _built;

        public override void OnShow()
        {
            EnsureBuilt();
            SurvivorsRunUiBuild.ApplyKoreanFontRecursive(transform);
        }

        public void Setup(SurvivorsRunManager manager)
        {
            EnsureBuilt();
            _manager = manager;
            if (_titleText != null)
                _titleText.text = "일시정지";
        }

        private void EnsureBuilt()
        {
            if (_built)
                return;
            _built = true;

            SurvivorsRunUiBuild.StretchFull(gameObject);
            if (_resumeButton != null && _quitButton != null)
            {
                Wire();
                return;
            }

            SurvivorsRunUiBuild.AddImage(gameObject, new Color(0f, 0f, 0f, 0.55f), raycast: true);

            var panel = SurvivorsRunUiBuild.Child(transform, "Panel");
            SurvivorsRunUiBuild.SetAnchored(panel.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(420f, 260f));
            SurvivorsRunUiBuild.AddImage(panel, new Color(0.12f, 0.12f, 0.16f, 0.96f), raycast: true);

            var titleGo = SurvivorsRunUiBuild.Child(panel.transform, "Title");
            SurvivorsRunUiBuild.SetAnchored(titleGo.GetComponent<RectTransform>(),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -36f), new Vector2(300f, 48f));
            _titleText = SurvivorsRunUiBuild.AddText(titleGo, "일시정지", 36f, TextAlignmentOptions.Center);

            _resumeButton = CreateActionButton(panel.transform, "Resume", "재개", new Vector2(0f, 10f));
            _quitButton = CreateActionButton(panel.transform, "Quit", "포기", new Vector2(0f, -70f));
            Wire();
        }

        private void Wire()
        {
            if (_resumeButton != null)
            {
                _resumeButton.onClick.RemoveAllListeners();
                _resumeButton.onClick.AddListener(() => _manager?.ClosePauseUi());
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.RemoveAllListeners();
                _quitButton.onClick.AddListener(() => _manager?.ExitToLobby());
            }
        }

        private static Button CreateActionButton(Transform parent, string name, string label, Vector2 pos)
        {
            var go = SurvivorsRunUiBuild.Child(parent, name);
            SurvivorsRunUiBuild.SetAnchored(go.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                pos, new Vector2(220f, 52f));
            var button = SurvivorsRunUiBuild.AddButton(go, new Color(0.28f, 0.3f, 0.42f, 1f));
            var labelGo = SurvivorsRunUiBuild.Child(go.transform, "Label");
            SurvivorsRunUiBuild.StretchFull(labelGo);
            SurvivorsRunUiBuild.AddText(labelGo, label, 26f, TextAlignmentOptions.Center);
            return button;
        }
    }
}
