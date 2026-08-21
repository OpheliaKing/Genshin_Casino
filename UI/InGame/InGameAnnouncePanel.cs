using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 게임 시작 / 턴 알림 등 중앙 안내 패널. DOTween으로 등장·퇴장.
    /// </summary>
    public class InGameAnnouncePanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _rect;
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private float _appearDuration = 0.35f;
        [SerializeField] private float _hideDuration = 0.25f;
        [SerializeField] private float _fromScale = 0.65f;
        [SerializeField] private float _punchScale = 1.08f;

        private Sequence _sequence;
        private Vector3 _restScale = Vector3.one;
        private bool _restCached;
        private bool _completing;

        private void Awake()
        {
            EnsureRefs();
            CacheRestScale();
            // PlayAsync의 SetActive(true) 중 Awake가 돌 수 있음.
            // 여기서 SetActive(false) 하면 첫 연출이 즉시 꺼지므로 alpha만 초기화한다.
            ApplyHiddenVisuals();
        }

        public async Task PlayAsync(string text, float holdSeconds = 1f)
        {
            EnsureRefs();
            CacheRestScale();
            KillSequence(completeAwaiter: false);

            if (_label != null)
                _label.text = text ?? string.Empty;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            if (_rect != null)
                _rect.localScale = _restScale * _fromScale;

            ApplyHiddenVisuals();

            var tcs = new TaskCompletionSource<bool>();
            _completing = false;
            _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            if (_canvasGroup != null)
                _sequence.Append(_canvasGroup.DOFade(1f, _appearDuration).SetEase(Ease.OutQuad));

            if (_rect != null)
            {
                var appear = _rect.DOScale(_restScale * _punchScale, _appearDuration).SetEase(Ease.OutBack);
                if (_canvasGroup != null)
                    _sequence.Join(appear);
                else
                    _sequence.Append(appear);

                _sequence.Append(_rect.DOScale(_restScale, 0.12f).SetEase(Ease.OutQuad));
            }

            _sequence.AppendInterval(Mathf.Max(0.05f, holdSeconds));

            if (_canvasGroup != null)
                _sequence.Append(_canvasGroup.DOFade(0f, _hideDuration).SetEase(Ease.InQuad));

            if (_rect != null)
            {
                var hide = _rect.DOScale(_restScale * 0.85f, _hideDuration).SetEase(Ease.InBack);
                if (_canvasGroup != null)
                    _sequence.Join(hide);
                else
                    _sequence.Append(hide);
            }

            _sequence.OnComplete(() =>
            {
                _completing = true;
                _sequence = null;
                ApplyHiddenVisuals();
                if (gameObject.activeSelf)
                    gameObject.SetActive(false);
                tcs.TrySetResult(true);
                _completing = false;
            });
            _sequence.OnKill(() =>
            {
                if (_completing)
                    return;
                tcs.TrySetResult(false);
            });

            await tcs.Task;
        }

        public void HideImmediate()
        {
            KillSequence(completeAwaiter: false);
            EnsureRefs();
            ApplyHiddenVisuals();

            if (_rect != null && _restCached)
                _rect.localScale = _restScale;

            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        private void ApplyHiddenVisuals()
        {
            if (_canvasGroup == null)
                return;

            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        private void CacheRestScale()
        {
            if (_restCached || _rect == null)
                return;

            _restScale = _rect.localScale;
            if (_restScale == Vector3.zero)
                _restScale = Vector3.one;
            _restCached = true;
        }

        private void EnsureRefs()
        {
            if (_rect == null)
                _rect = transform as RectTransform;

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (_label == null)
                _label = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        private void KillSequence(bool completeAwaiter)
        {
            if (_sequence == null || !_sequence.IsActive())
            {
                _sequence = null;
                return;
            }

            if (!completeAwaiter)
                _sequence.OnKill(null);

            _sequence.Kill();
            _sequence = null;
        }

        private void OnDestroy()
        {
            KillSequence(completeAwaiter: false);
        }
    }
}
