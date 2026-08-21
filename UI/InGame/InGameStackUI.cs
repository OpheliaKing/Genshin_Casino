using System.IO;
using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace SHIN
{
    public class InGameStackUI : MonoBehaviour
    {
        private const float GoldTweenDuration = 0.45f;

        [SerializeField] private Image _charIcon;
        [SerializeField] private TextMeshProUGUI _goldValueText;

        private int _displayedGold;
        private bool _hasDisplayedGold;
        private Tween _goldTween;

        private void Awake()
        {
            if (_goldValueText == null)
                _goldValueText = GetComponentInChildren<TextMeshProUGUI>(true);

            // 프리팹 기본값(1000 등)이 첫 프레임에 보이지 않도록
            if (_goldValueText != null && !_hasDisplayedGold)
                _goldValueText.text = FormatGold(0);
        }

        public void SetGold(int gold)
        {
            if (_goldValueText == null)
                return;

            if (!_hasDisplayedGold)
            {
                _hasDisplayedGold = true;
                _displayedGold = gold;
                _goldValueText.text = FormatGold(gold);
                return;
            }

            if (_displayedGold == gold)
                return;

            _goldTween?.Kill();
            _goldTween = DOTween
                .To(() => _displayedGold, value =>
                {
                    _displayedGold = value;
                    _goldValueText.text = FormatGold(value);
                }, gold, GoldTweenDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private static string FormatGold(int gold) => $"{gold} G";

        public async Task SetIconAsync(string iconPath, string atlasAddress = null)
        {
            if (_charIcon == null)
                return;

            if (string.IsNullOrEmpty(iconPath))
            {
                _charIcon.sprite = null;
                return;
            }

            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager == null)
            {
                Debug.LogError("[InGameStackUI] ResourceManager가 없습니다.");
                return;
            }

            Sprite sprite = null;

            // 아틀라스가 있으면 스프라이트 이름(또는 파일명)으로 조회. Art 경로는 Addressable 키가 아님.
            if (!string.IsNullOrEmpty(atlasAddress))
            {
                var atlas = await resourceManager.LoadAsync<SpriteAtlas>(atlasAddress);
                if (this == null)
                    return;

                if (atlas != null)
                {
                    sprite = atlas.GetSprite(iconPath);
                    if (sprite == null)
                        sprite = atlas.GetSprite(Path.GetFileNameWithoutExtension(iconPath));
                }
            }

            if (sprite == null && !iconPath.Contains("/") && !iconPath.Contains("\\"))
                sprite = await resourceManager.LoadAsync<Sprite>(iconPath);

            if (this == null)
                return;

            if (sprite == null)
            {
                Debug.LogWarning($"[InGameStackUI] 아이콘 로드 실패: {iconPath}");
                return;
            }

            _charIcon.sprite = sprite;
        }

        private void OnDestroy()
        {
            _goldTween?.Kill();
            _goldTween = null;
        }
    }
}
