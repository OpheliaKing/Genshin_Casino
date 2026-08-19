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

        [SerializeField] private Image _image;
        [SerializeField] private List<TextMeshProUGUI> _labels = new();

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

            var text = _faceUp ? _card.DisplayName : string.Empty;
            var color = _card.Suit is CardSuit.Hearts or CardSuit.Diamonds
                ? new Color(0.75f, 0.12f, 0.12f)
                : Color.black;

            if (_labels == null)
                return;

            for (var i = 0; i < _labels.Count; i++)
            {
                var label = _labels[i];
                if (label == null)
                    continue;

                label.enabled = _faceUp;
                label.text = text;
                label.color = color;
            }
        }

        private void EnsureLabels()
        {
            _labels ??= new List<TextMeshProUGUI>();
            _labels.RemoveAll(label => label == null);
            if (_labels.Count > 0)
                return;

            var existing = GetComponentsInChildren<TextMeshProUGUI>(true);
            for (var i = 0; i < existing.Length; i++)
                _labels.Add(existing[i]);

            if (_labels.Count > 0)
                return;

            _labels.Add(CreateLabel("Label", Quaternion.identity));
            _labels.Add(CreateLabel("LabelInverted", Quaternion.Euler(0f, 0f, 180f)));
        }

        private TextMeshProUGUI CreateLabel(string name, Quaternion rotation)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8f, 8f);
            rect.offsetMax = new Vector2(-8f, -8f);
            rect.localRotation = rotation;

            var label = go.AddComponent<TextMeshProUGUI>();
            label.fontSize = 28;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.raycastTarget = false;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
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
