using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SHIN
{
    public class DialogUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private float _hideDelayMin = 2f;
        [SerializeField] private float _hideDelayMax = 3f;

        private Coroutine _hideRoutine;

        private void Awake()
        {
            EnsureRefs();
        }

        public void Show(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                Hide();
                return;
            }

            EnsureRefs();
            if (_text != null)
                _text.text = message.Trim();

            gameObject.SetActive(true);
            RebuildLayout();
            RestartHideTimer();
        }

        public void Hide()
        {
            StopHideTimer();

            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        private void RestartHideTimer()
        {
            StopHideTimer();
            _hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private void StopHideTimer()
        {
            if (_hideRoutine == null)
                return;

            StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }

        private IEnumerator HideAfterDelay()
        {
            var delay = Mathf.Max(0.05f, Random.Range(_hideDelayMin, _hideDelayMax));
            yield return new WaitForSecondsRealtime(delay);
            _hideRoutine = null;
            Hide();
        }

        private void EnsureRefs()
        {
            if (_text == null)
                _text = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        private void RebuildLayout()
        {
            if (_text != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_text.rectTransform);

            if (transform is RectTransform root)
                LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        }

        private void OnDisable()
        {
            StopHideTimer();
        }
    }
}
