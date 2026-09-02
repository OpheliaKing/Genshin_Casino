using System;
using TMPro;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 인게임 액션 버튼. 클릭음은 ButtonBase가 담당한다.
    /// </summary>
    public class InGameButton : MonoBehaviour
    {
        [SerializeField] private ButtonBase _button;
        [SerializeField] private TextMeshProUGUI _label;

        public bool Interactable
        {
            get => _button != null && _button.interactable;
            set
            {
                if (_button != null)
                    _button.interactable = value;
            }
        }

        private void Awake()
        {
            EnsureRefs();
        }

        public void SetLabel(string text)
        {
            EnsureRefs();
            if (_label != null)
                _label.text = text;
        }

        public void AddClickListener(Action onClick)
        {
            EnsureRefs();
            if (_button == null || onClick == null)
                return;

            _button.onClick.AddListener(() => onClick());
        }

        public void RemoveAllClickListeners()
        {
            EnsureRefs();
            _button?.onClick.RemoveAllListeners();
        }

        private void EnsureRefs()
        {
            if (_button == null)
                _button = GetComponent<ButtonBase>();

            if (_label == null)
                _label = GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }
}
