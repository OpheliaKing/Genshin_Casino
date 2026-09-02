using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace SHIN
{
    public class OpponentSelectItem : MonoBehaviour
    {
        [SerializeField]
        private Image _opponentImage;

        [SerializeField]
        private ButtonBase _button;

        [SerializeField]
        private TextMeshProUGUI _nameLabel;

        private OpponentData _data;
        private Action<OpponentData> _onClicked;

        private void Awake()
        {
            EnsureRefs();
            if (_button != null)
                _button.onClick.AddListener(OnClick);
        }

        public Task BindAsync(OpponentData data, Action<OpponentData> onClicked)
        {
            EnsureRefs();
            _data = data;
            _onClicked = onClicked;
            ApplyName(data);
            return ApplySpriteAsync(data);
        }

        private void ApplyName(OpponentData data)
        {
            if (_nameLabel == null)
                return;

            _nameLabel.text = data != null && !string.IsNullOrEmpty(data.name)
                ? data.name
                : string.Empty;
            _nameLabel.raycastTarget = false;
        }

        private async Task ApplySpriteAsync(OpponentData data)
        {
            if (_opponentImage == null || data == null)
                return;

            if (string.IsNullOrEmpty(data.atlasAddress) || string.IsNullOrEmpty(data.spriteName))
            {
                Debug.LogWarning("[OpponentSelectItem] atlasAddress 또는 spriteName이 비어 있습니다.");
                return;
            }

            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager == null)
            {
                Debug.LogError("[OpponentSelectItem] ResourceManager가 없습니다.");
                return;
            }

            var atlas = await resourceManager.LoadAsync<SpriteAtlas>(data.atlasAddress);
            if (this == null || _data != data)
                return;

            if (atlas == null)
            {
                Debug.LogError($"[OpponentSelectItem] 아틀라스 로드 실패: {data.atlasAddress}");
                return;
            }

            var sprite = atlas.GetSprite(data.spriteName);
            if (sprite == null)
            {
                Debug.LogWarning($"[OpponentSelectItem] 스프라이트를 찾지 못했습니다: {data.spriteName}");
                return;
            }

            _opponentImage.sprite = sprite;
        }

        private void EnsureRefs()
        {
            if (_button == null)
                _button = GetComponent<ButtonBase>();

            if (_button == null)
                _button = gameObject.AddComponent<ButtonBase>();

            if (_opponentImage == null)
            {
                var mask = transform.Find("Mask/Image");
                if (mask != null)
                    _opponentImage = mask.GetComponent<Image>();
            }

            if (_nameLabel == null)
            {
                var namePanel = transform.Find("NamePanel");
                if (namePanel != null)
                    _nameLabel = namePanel.GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        private void OnClick()
        {
            if (_data == null)
                return;

            _onClicked?.Invoke(_data);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClick);
        }
    }
}
