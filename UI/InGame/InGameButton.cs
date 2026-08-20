using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SHIN
{
    public class InGameButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
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
                _button = GetComponent<Button>();

            if (_label == null)
                _label = GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }
}
