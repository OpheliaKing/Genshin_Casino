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
        private bool _bound;
        private int _applyVersion;

        private static Sprite _frontSprite;
        private static Sprite _backSprite;
        private static bool _spritesReady;
        private static Task _spriteLoadTask;

        private void Awake()
        {
            if (_image == null)
                _image = GetComponent<Image>();

            EnsureLabels();
            SetContentVisible(false);
        }

        public void Bind(PokerCard card, bool faceUp)
        {
            _ = BindAsync(card, faceUp);
        }

        public async Task BindAsync(PokerCard card, bool faceUp)
        {
            _card = card;
            _faceUp = faceUp;
            _bound = true;
            var version = ++_applyVersion;
            SetContentVisible(false);

            await EnsureSpritesAsync();
            if (this == null || version != _applyVersion)
                return;

            ApplyVisual();
        }

        public void SetFaceUp(bool faceUp)
        {
            if (!_bound)
                return;

            _faceUp = faceUp;
            _ = ApplyFaceStateAsync();
        }

        public async Task SetFaceUpAsync(bool faceUp)
        {
            if (!_bound)
                return;

            _faceUp = faceUp;
            await ApplyFaceStateAsync();
        }

        private async Task ApplyFaceStateAsync()
        {
            var version = ++_applyVersion;
            SetContentVisible(false);

            await EnsureSpritesAsync();
            if (this == null || version != _applyVersion)
                return;

            // 뒷면이 아직 없으면 한 번 더 강제 로드
            if (!_faceUp && _backSprite == null)
            {
                InvalidateSpriteCache();
                await EnsureSpritesAsync();
                if (this == null || version != _applyVersion)
                    return;
            }

            ApplyVisual();
        }

        private void ApplyVisual()
        {
            if (_image == null)
                return;

            if (_faceUp)
            {
                if (_frontSprite == null)
                {
                    SetContentVisible(false);
                    return;
                }

                _image.sprite = _frontSprite;
            }
            else
            {
                // 뒷면 없으면 절대 앞면으로 대체하지 않음 (빈 앞면처럼 보임)
                if (_backSprite == null)
                {
                    SetContentVisible(false);
                    Debug.LogWarning("[CardObject] 뒷면 스프라이트가 없어 상대 카드를 숨깁니다.");
                    return;
                }

                _image.sprite = _backSprite;
            }

            var color = _card.Suit is CardSuit.Hearts or CardSuit.Diamonds
                ? new Color(0.82f, 0.12f, 0.14f)
                : new Color(0.08f, 0.08f, 0.1f);

            var showFace = _faceUp;
            var cornerText = showFace ? _card.CornerText : string.Empty;
            var suitText = showFace ? _card.SuitSymbol : string.Empty;

            if (_cornerLabels != null)
            {
                for (var i = 0; i < _cornerLabels.Count; i++)
                {
                    var label = _cornerLabels[i];
                    if (label == null)
                        continue;

                    label.text = cornerText;
                    label.color = color;
                    label.fontSize = CornerFontSize;
                    label.enabled = showFace;
                }
            }

            if (_centerSuit != null)
            {
                _centerSuit.text = suitText;
                _centerSuit.color = color;
                _centerSuit.fontSize = CenterSuitFontSize;
                _centerSuit.enabled = showFace;
            }

            SetContentVisible(true);
        }

        private void SetContentVisible(bool visible)
        {
            if (_image != null)
                _image.enabled = visible;

            if (_cornerLabels != null)
            {
                for (var i = 0; i < _cornerLabels.Count; i++)
                {
                    if (_cornerLabels[i] != null)
                        _cornerLabels[i].enabled = visible && _faceUp;
                }
            }

            if (_centerSuit != null)
                _centerSuit.enabled = visible && _faceUp;
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

        public static Task PreloadSpritesAsync()
        {
            return EnsureSpritesAsync();
        }

        private static void InvalidateSpriteCache()
        {
            _spritesReady = false;
            _spriteLoadTask = null;
            _frontSprite = null;
            _backSprite = null;
        }

        private static Task EnsureSpritesAsync()
        {
            if (_spritesReady && _frontSprite != null && _backSprite != null)
                return Task.CompletedTask;

            if (_spriteLoadTask != null)
                return _spriteLoadTask;

            _spriteLoadTask = LoadSpritesAsync();
            return _spriteLoadTask;
        }

        private static async Task LoadSpritesAsync()
        {
            try
            {
                var resourceManager = GameManager.Instance?.ResourceManager;
                if (resourceManager == null)
                    return;

                var atlas = await resourceManager.LoadAsync<SpriteAtlas>(PublicVariable.Address.InGameAtlas);
                if (atlas == null)
                {
                    Debug.LogError("[CardObject] InGameAtlas 로드 실패");
                    return;
                }

                _frontSprite = ResolveSprite(atlas, FrontSpriteKey);
                _backSprite = ResolveSprite(atlas, BackSpriteKey);

                if (_frontSprite == null)
                    Debug.LogWarning($"[CardObject] 앞면 스프라이트 없음: {FrontSpriteKey}");
                if (_backSprite == null)
                    Debug.LogWarning($"[CardObject] 뒷면 스프라이트 없음: {BackSpriteKey}");

                _spritesReady = _frontSprite != null && _backSprite != null;
            }
            finally
            {
                // 실패 시 다음 호출에서 재시도 가능하도록
                if (!_spritesReady)
                    _spriteLoadTask = null;
            }
        }

        private static Sprite ResolveSprite(SpriteAtlas atlas, string exactName)
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

                var name = sprite.name.Replace("(Clone)", string.Empty).Trim();
                if (name == exactName)
                    return sprite;
            }

            // 파일명 끝부분 매칭 (구 이름/패킹 잔여 대비)
            for (var i = 0; i < sprites.Length; i++)
            {
                var sprite = sprites[i];
                if (sprite == null)
                    continue;

                var name = sprite.name.Replace("(Clone)", string.Empty).Trim();
                if (name.EndsWith(exactName) || name.StartsWith(exactName))
                    return sprite;
            }

            return null;
        }
    }
}
