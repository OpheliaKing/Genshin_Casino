using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SHIN
{
    public class SurvivorsRunCharacterSelectItem : MonoBehaviour
    {
        [FormerlySerializedAs("charImage")]
        [SerializeField]
        private Image _charImage;

        [FormerlySerializedAs("charNameText")]
        [SerializeField]
        private TextMeshProUGUI _charNameText;

        [SerializeField]
        private ButtonBase _button;

        private SurvivorsRunUnitData _data;
        private Action<SurvivorsRunUnitData> _onClicked;

        private void Awake()
        {
            EnsureRefs();
            if (_button != null)
                _button.onClick.AddListener(OnClick);
        }

        public Task BindAsync(SurvivorsRunUnitData data, Action<SurvivorsRunUnitData> onClicked)
        {
            EnsureRefs();
            _data = data;
            _onClicked = onClicked;

            if (_charNameText != null)
            {
                _charNameText.text = data != null && !string.IsNullOrEmpty(data.UnitName)
                    ? data.UnitName
                    : string.Empty;
                _charNameText.raycastTarget = false;
            }

            return ApplySpriteAsync(data);
        }

        private async Task ApplySpriteAsync(SurvivorsRunUnitData data)
        {
            if (_charImage == null || data == null)
                return;

            if (string.IsNullOrEmpty(data.UnitSpritePath))
                return;

            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager == null)
            {
                Debug.LogError("[SurvivorsRunCharacterSelectItem] ResourceManager가 없습니다.");
                return;
            }

            var sprite = await resourceManager.LoadAsync<Sprite>(data.UnitSpritePath);
            if (this == null || _data != data)
                return;

            if (sprite == null)
            {
                Debug.LogWarning($"[SurvivorsRunCharacterSelectItem] 스프라이트 로드 실패: {data.UnitSpritePath}");
                return;
            }

            _charImage.sprite = sprite;
        }

        private void EnsureRefs()
        {
            if (_button == null)
                _button = GetComponent<ButtonBase>();

            if (_button == null)
                _button = gameObject.AddComponent<ButtonBase>();

            if (_charImage == null)
                _charImage = GetComponentInChildren<Image>(true);

            if (_charNameText == null)
                _charNameText = GetComponentInChildren<TextMeshProUGUI>(true);
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
