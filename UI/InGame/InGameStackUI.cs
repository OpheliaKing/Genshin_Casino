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

        public void SetGold(int gold)
        {
            if (_goldValueText == null)
                return;

            if (!_hasDisplayedGold)
            {
                _hasDisplayedGold = true;
                _displayedGold = gold;
                _goldValueText.text = gold.ToString();
                return;
            }

            if (_displayedGold == gold)
                return;

            _goldTween?.Kill();
            _goldTween = DOTween
                .To(() => _displayedGold, value =>
                {
                    _displayedGold = value;
                    _goldValueText.text = value.ToString();
                }, gold, GoldTweenDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

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
            if (iconPath.Contains("/") || iconPath.Contains("\\"))
                sprite = await resourceManager.LoadAsync<Sprite>(iconPath);

            if (sprite == null && !string.IsNullOrEmpty(atlasAddress))
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
