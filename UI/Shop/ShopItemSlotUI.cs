using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SHIN
{
    /// <summary>
    /// 상점 상품 한 칸. 프리팹 연결 또는 런타임 빌드.
    /// </summary>
    public class ShopItemSlotUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _descText;
        [SerializeField] private TextMeshProUGUI _priceText;
        [SerializeField] private TextMeshProUGUI _stateText;
        [SerializeField] private Button _buyButton;
        [SerializeField] private Image _background;

        private ShopUI _owner;
        private ShopEntryView _entry;
        private bool _built;

        public string Tid => _entry != null ? _entry.Tid : null;

        public void Bind(ShopUI owner, ShopEntryView entry)
        {
            EnsureBuilt();
            _owner = owner;
            _entry = entry;
            Refresh();
        }

        public void Refresh()
        {
            EnsureBuilt();
            if (_entry == null)
                return;

            if (_nameText != null)
                _nameText.text = string.IsNullOrWhiteSpace(_entry.DisplayName) ? _entry.Tid : _entry.DisplayName;
            if (_descText != null)
                _descText.text = _entry.Description ?? string.Empty;

            if (_priceText != null)
                _priceText.text = _entry.Owned ? "-" : $"칩 {_entry.Price}";

            if (_stateText != null)
            {
                if (_entry.Owned)
                    _stateText.text = "보유중";
                else if (!_entry.CanAfford)
                    _stateText.text = "칩 부족";
                else
                    _stateText.text = "구매 가능";
            }

            if (_buyButton != null)
            {
                _buyButton.interactable = !_entry.Owned && _entry.CanAfford;
                _buyButton.onClick.RemoveAllListeners();
                _buyButton.onClick.AddListener(OnClickBuy);
            }
        }

        private void OnClickBuy()
        {
            if (_owner == null || _entry == null)
                return;

            _owner.RequestBuy(_entry);
        }

        public void EnsureBuilt()
        {
            if (_built)
                return;

            if (_nameText == null || _buyButton == null)
                BuildRuntime();

            SurvivorsRunUiBuild.ApplyKoreanFontRecursive(transform);
            _built = true;
        }

        private void BuildRuntime()
        {
            var root = gameObject;
            var bg = SurvivorsRunUiBuild.AddImage(root, new Color(0.12f, 0.14f, 0.2f, 0.92f), raycast: true);
            _background = bg;

            var nameGo = SurvivorsRunUiBuild.Child(transform, "Name");
            SurvivorsRunUiBuild.SetAnchored(
                nameGo.GetComponent<RectTransform>(),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -8f), new Vector2(-16f, 36f));
            _nameText = SurvivorsRunUiBuild.AddText(nameGo, "상품", 26f, TextAlignmentOptions.Left);

            var descGo = SurvivorsRunUiBuild.Child(transform, "Desc");
            SurvivorsRunUiBuild.SetAnchored(
                descGo.GetComponent<RectTransform>(),
                new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -8f), new Vector2(-16f, -44f));
            _descText = SurvivorsRunUiBuild.AddText(descGo, "설명", 18f, TextAlignmentOptions.TopLeft);
            _descText.enableWordWrapping = true;
            _descText.color = new Color(0.85f, 0.88f, 0.95f, 0.9f);

            var priceGo = SurvivorsRunUiBuild.Child(transform, "Price");
            SurvivorsRunUiBuild.SetAnchored(
                priceGo.GetComponent<RectTransform>(),
                new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f),
                new Vector2(12f, 10f), new Vector2(-8f, 34f));
            _priceText = SurvivorsRunUiBuild.AddText(priceGo, "칩 0", 22f, TextAlignmentOptions.Left);
            _priceText.color = new Color(1f, 0.92f, 0.45f, 1f);

            var stateGo = SurvivorsRunUiBuild.Child(transform, "State");
            SurvivorsRunUiBuild.SetAnchored(
                stateGo.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-120f, 10f), new Vector2(-12f, 34f));
            _stateText = SurvivorsRunUiBuild.AddText(stateGo, "", 18f, TextAlignmentOptions.Right);

            var buyGo = SurvivorsRunUiBuild.Child(transform, "Buy");
            SurvivorsRunUiBuild.SetAnchored(
                buyGo.GetComponent<RectTransform>(),
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-108f, 8f), new Vector2(-12f, 44f));
            SurvivorsRunUiBuild.AddButton(buyGo, new Color(0.25f, 0.55f, 0.4f, 1f));
            var buyLabel = SurvivorsRunUiBuild.Child(buyGo.transform, "Label");
            SurvivorsRunUiBuild.StretchFull(buyLabel);
            SurvivorsRunUiBuild.AddText(buyLabel, "구매", 20f, TextAlignmentOptions.Center);
            _buyButton = buyGo.GetComponent<Button>();
        }
    }
}
