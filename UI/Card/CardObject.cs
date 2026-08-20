using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace SHIN
{
    public class CardObject : MonoBehaviour
    {
        private const string FrontSpriteKey = "sprite_card_base_001";
        private const string BackSpriteKey = "sprite_card_base_002";
        private const float CornerFontSize = 36f;
        private const float CenterSuitFontSize = 64f;

        [SerializeField] private Image _image;
        [SerializeField] private List<TextMeshProUGUI> _cornerLabels = new();
        [SerializeField] private TextMeshProUGUI _centerSuit;

        private PokerCard _card;
        private bool _faceUp;
        private static Sprite _frontSprite;
        private static Sprite _backSprite;
        private static bool _spritesLoaded;

        private void Awake()
        {
            if (_image == null)
                _image = GetComponent<Image>();

            EnsureLabels();
        }

        public void Bind(PokerCard card, bool faceUp)
        {
            _card = card;
            _faceUp = faceUp;
            _ = ApplyAsync();
        }

        public void SetFaceUp(bool faceUp)
        {
            _faceUp = faceUp;
            RefreshVisual();
        }

        private async Task ApplyAsync()
        {
            await EnsureSpritesAsync();
            if (this == null)
                return;

            RefreshVisual();
        }

        private void RefreshVisual()
        {
            if (_image != null)
                _image.sprite = _faceUp ? (_frontSprite != null ? _frontSprite : _image.sprite) : (_backSprite != null ? _backSprite : _image.sprite);

            var color = _card.Suit is CardSuit.Hearts or CardSuit.Diamonds
                ? new Color(0.82f, 0.12f, 0.14f)
                : new Color(0.08f, 0.08f, 0.1f);

            var cornerText = _faceUp ? _card.CornerText : string.Empty;
            var suitText = _faceUp ? _card.SuitSymbol : string.Empty;

            if (_cornerLabels != null)
            {
                for (var i = 0; i < _cornerLabels.Count; i++)
                {
                    var label = _cornerLabels[i];
                    if (label == null)
                        continue;

                    label.enabled = _faceUp;
                    label.text = cornerText;
                    label.color = color;
                    label.fontSize = CornerFontSize;
                }
            }

            if (_centerSuit != null)
            {
                _centerSuit.enabled = _faceUp;
                _centerSuit.text = suitText;
                _centerSuit.color = color;
                _centerSuit.fontSize = CenterSuitFontSize;
            }
        }

        private void EnsureLabels()
        {
            _cornerLabels ??= new List<TextMeshProUGUI>();
            _cornerLabels.RemoveAll(label => label == null);

            if (_cornerLabels.Count == 0)
            {
                var existing = GetComponentsInChildren<TextMeshProUGUI>(true);
                for (var i = 0; i < existing.Length; i++)
                {
                    if (existing[i] != null && existing[i] != _centerSuit && existing[i].name != "CenterSuit")
                        _cornerLabels.Add(existing[i]);
                }
            }

            if (_cornerLabels.Count == 0)
            {
                _cornerLabels.Add(CreateCornerLabel("Label", Quaternion.identity));
                _cornerLabels.Add(CreateCornerLabel("LabelInverted", Quaternion.Euler(0f, 0f, 180f)));
            }

            if (_centerSuit == null)
            {
                var center = transform.Find("CenterSuit");
                if (center != null)
                    _centerSuit = center.GetComponent<TextMeshProUGUI>();
            }

            if (_centerSuit == null)
                _centerSuit = CreateCenterSuitLabel();

            for (var i = 0; i < _cornerLabels.Count; i++)
                ConfigureCornerLabel(_cornerLabels[i]);

            ConfigureCenterSuitLabel(_centerSuit);
        }

        private TextMeshProUGUI CreateCornerLabel(string name, Quaternion rotation)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(10f, 10f);
            rect.offsetMax = new Vector2(-10f, -10f);
            rect.localRotation = rotation;

            var label = go.AddComponent<TextMeshProUGUI>();
            ConfigureCornerLabel(label);
            return label;
        }

        private TextMeshProUGUI CreateCenterSuitLabel()
        {
            var go = new GameObject("CenterSuit");
            go.transform.SetParent(transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(120f, 120f);
            rect.anchoredPosition = Vector2.zero;

            var label = go.AddComponent<TextMeshProUGUI>();
            ConfigureCenterSuitLabel(label);
            return label;
        }

        private static void ConfigureCornerLabel(TextMeshProUGUI label)
        {
            if (label == null)
                return;

            label.fontSize = CornerFontSize;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.lineSpacing = -20f;
            label.fontStyle = FontStyles.Bold;
        }

        private static void ConfigureCenterSuitLabel(TextMeshProUGUI label)
        {
            if (label == null)
                return;

            label.fontSize = CenterSuitFontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.fontStyle = FontStyles.Bold;
        }

        private static async Task EnsureSpritesAsync()
        {
            if (_spritesLoaded)
                return;

            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager == null)
                return;

            var atlas = await resourceManager.LoadAsync<SpriteAtlas>(PublicVariable.Address.InGameAtlas);
            if (atlas == null)
                return;

            _frontSprite = FindSprite(atlas, FrontSpriteKey, "001");
            _backSprite = FindSprite(atlas, BackSpriteKey, "002");
            _spritesLoaded = _frontSprite != null || _backSprite != null;
        }

        private static Sprite FindSprite(SpriteAtlas atlas, string exactName, string fallbackToken)
        {
            var exact = atlas.GetSprite(exactName);
            if (exact != null)
                return exact;

            var sprites = new Sprite[atlas.spriteCount];
            atlas.GetSprites(sprites);
            for (var i = 0; i < sprites.Length; i++)
            {
                var sprite = sprites[i];
                if (sprite == null)
                    continue;

                var name = sprite.name.Replace("(Clone)", string.Empty);
                if (name.Contains(fallbackToken))
                    return sprite;
            }

            return null;
        }
    }
}
