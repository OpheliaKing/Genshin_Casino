using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SHIN
{
    /// <summary>
    /// 레벨업 시 아이템 3선택. TimeScale=0 상태에서 연다.
    /// </summary>
    public class SurvivorsRunLevelUpUI : UIBase
    {
        private const int ChoiceCount = 3;

        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private Transform _choiceRoot;

        private SurvivorsRunManager _manager;
        private readonly List<Button> _choiceButtons = new();
        private readonly List<TextMeshProUGUI> _choiceLabels = new();
        private readonly List<SurvivorsRunItemData> _choices = new();
        private bool _built;
        private bool _picked;

        public override void OnShow()
        {
            EnsureBuilt();
            SurvivorsRunUiBuild.ApplyKoreanFontRecursive(transform);
            _picked = false;
        }

        public void Setup(SurvivorsRunManager manager, IReadOnlyList<SurvivorsRunItemData> choices)
        {
            EnsureBuilt();
            _manager = manager;
            _choices.Clear();
            _picked = false;

            if (_titleText != null)
                _titleText.text = "레벨 업! 보상을 선택";

            for (var i = 0; i < ChoiceCount; i++)
            {
                SurvivorsRunItemData data = null;
                if (choices != null && i < choices.Count)
                    data = choices[i];
                if (i < _choices.Count)
                    _choices[i] = data;
                else
                    _choices.Add(data);

                if (i >= _choiceButtons.Count)
                    continue;

                var label = _choiceLabels[i];
                var button = _choiceButtons[i];
                if (button != null)
                    button.gameObject.SetActive(data != null);
                if (label != null && data != null)
                {
                    var stackHint = string.IsNullOrEmpty(data.Name) ? data.Tid : data.Name;
                    label.text = $"{stackHint}\n{data.Description}";
                }
            }
        }

        private void OnChoiceClicked(int index)
        {
            if (_picked || _manager == null)
                return;
            if (index < 0 || index >= _choices.Count || _choices[index] == null)
                return;

            _picked = true;
            _manager.ConfirmLevelUpChoice(_choices[index]);
        }

        private void EnsureBuilt()
        {
            if (_built)
                return;
            _built = true;

            SurvivorsRunUiBuild.StretchFull(gameObject);
            if (_choiceRoot != null && _choiceButtons.Count > 0)
                return;

            SurvivorsRunUiBuild.AddImage(gameObject, new Color(0f, 0f, 0f, 0.65f), raycast: true);

            var panel = SurvivorsRunUiBuild.Child(transform, "Panel");
            var panelRt = panel.GetComponent<RectTransform>();
            SurvivorsRunUiBuild.SetAnchored(panelRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 420f));
            SurvivorsRunUiBuild.AddImage(panel, new Color(0.12f, 0.12f, 0.18f, 0.95f), raycast: true);

            var titleGo = SurvivorsRunUiBuild.Child(panel.transform, "Title");
            var titleRt = titleGo.GetComponent<RectTransform>();
            SurvivorsRunUiBuild.SetAnchored(titleRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(800f, 48f));
            _titleText = SurvivorsRunUiBuild.AddText(titleGo, "레벨 업! 보상을 선택", 34f, TextAlignmentOptions.Center);

            _choiceRoot = SurvivorsRunUiBuild.Child(panel.transform, "Choices").transform;
            var rootRt = _choiceRoot.GetComponent<RectTransform>();
            SurvivorsRunUiBuild.SetAnchored(rootRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(920f, 280f));
            var layout = _choiceRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            _choiceButtons.Clear();
            _choiceLabels.Clear();
            for (var i = 0; i < ChoiceCount; i++)
            {
                var idx = i;
                var card = SurvivorsRunUiBuild.Child(_choiceRoot, $"Choice_{i}");
                var cardRt = card.GetComponent<RectTransform>();
                cardRt.sizeDelta = new Vector2(280f, 240f);
                var button = SurvivorsRunUiBuild.AddButton(card, new Color(0.22f, 0.25f, 0.38f, 1f));
                button.onClick.AddListener(() => OnChoiceClicked(idx));

                var labelGo = SurvivorsRunUiBuild.Child(card.transform, "Label");
                SurvivorsRunUiBuild.StretchFull(labelGo);
                var labelRt = labelGo.GetComponent<RectTransform>();
                labelRt.offsetMin = new Vector2(12f, 12f);
                labelRt.offsetMax = new Vector2(-12f, -12f);
                var label = SurvivorsRunUiBuild.AddText(labelGo, string.Empty, 22f, TextAlignmentOptions.Center);
                label.enableWordWrapping = true;

                _choiceButtons.Add(button);
                _choiceLabels.Add(label);
            }
        }
    }
}
