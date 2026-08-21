using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SHIN
{
    public class DialogUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private float _charsPerSecond = 28f;
        [SerializeField] private float _hideDelayMin = 2f;
        [SerializeField] private float _hideDelayMax = 3f;

        private Tween _typeTween;
        private Tween _hideTween;
        private int _targetVisibleChars;

        private void Awake()
        {
            EnsureRefs();
        }

        public void Show(string message, string voiceAddress = null)
        {
            PlayVoice(voiceAddress);

            if (string.IsNullOrWhiteSpace(message))
            {
                Hide();
                return;
            }

            EnsureRefs();
            KillTweens();

            // 비활성 상태에서 ForceMeshUpdate하면 characterCount가 이전 대사 값으로 남을 수 있음.
            gameObject.SetActive(true);

            if (_text == null)
                return;

            var body = message.Trim();
            _text.text = body;
            _text.maxVisibleCharacters = int.MaxValue;
            _text.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
            RebuildLayout();
            _text.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);

            var total = _text.textInfo != null ? _text.textInfo.characterCount : 0;
            if (total <= 0)
                total = body.Length;

            _targetVisibleChars = total;
            if (total <= 0)
            {
                ScheduleHide();
                return;
            }

            // 레이아웃은 전체 문장 기준으로 이미 잡힌 뒤, 타이핑만 진행한다.
            _text.maxVisibleCharacters = 0;

            var duration = total / Mathf.Max(1f, _charsPerSecond);
            var visible = 0f;
            _typeTween = DOTween.To(() => visible, value =>
                {
                    visible = value;
                    _text.maxVisibleCharacters = Mathf.Clamp(Mathf.FloorToInt(value), 0, _targetVisibleChars);
                }, total, duration)
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .OnComplete(() =>
                {
                    _text.maxVisibleCharacters = _targetVisibleChars;
                    ScheduleHide();
                });
        }

        public void Hide()
        {
            KillTweens();

            if (_text != null)
                _text.maxVisibleCharacters = int.MaxValue;

            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        private static void PlayVoice(string voiceAddress)
        {
            if (string.IsNullOrWhiteSpace(voiceAddress))
                return;

            var soundManager = GameManager.Instance?.SoundManager;
            if (soundManager == null)
            {
                Debug.LogWarning("[DialogUI] SoundManager가 없습니다.");
                return;
            }

            soundManager.PlaySe(voiceAddress);
        }

        private void ScheduleHide()
        {
            KillHideTween();
            var delay = Mathf.Max(0.05f, Random.Range(_hideDelayMin, _hideDelayMax));
            _hideTween = DOVirtual.DelayedCall(delay, Hide)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void KillTweens()
        {
            if (_typeTween != null && _typeTween.IsActive())
                _typeTween.Kill();
            _typeTween = null;
            KillHideTween();
        }

        private void KillHideTween()
        {
            if (_hideTween != null && _hideTween.IsActive())
                _hideTween.Kill();
            _hideTween = null;
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
            KillTweens();
        }
    }
}
