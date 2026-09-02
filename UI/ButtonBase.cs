using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SHIN
{
    /// <summary>
    /// Unity UI Button 확장. 클릭 시 Addressables 경로의 SE를 재생한다.
    /// </summary>
    public class ButtonBase : Button
    {
        [SerializeField] private string _buttonSoundPath = PublicVariable.Address.SeUiClick;

        public string ButtonSoundPath
        {
            get => _buttonSoundPath;
            set => _buttonSoundPath = value;
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && IsActive() && IsInteractable())
                PlayClickSound();

            base.OnPointerClick(eventData);
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            if (IsActive() && IsInteractable())
                PlayClickSound();

            base.OnSubmit(eventData);
        }

        private void PlayClickSound()
        {
            var path = string.IsNullOrWhiteSpace(_buttonSoundPath)
                ? PublicVariable.Address.SeUiClick
                : _buttonSoundPath.Trim();

            if (string.IsNullOrEmpty(path))
                return;

            GameManager.Instance?.SoundManager?.PlaySe(path);
        }
    }
}
