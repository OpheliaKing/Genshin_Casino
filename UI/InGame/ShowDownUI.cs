using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SHIN
{
    /// <summary>
    /// 쇼다운 시작 연출. DOTween으로 화려하게 등장·퇴장.
    /// </summary>
    public class ShowDownUI : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _rect;
        [SerializeField] private float _appearDuration = 0.45f;
        [SerializeField] private float _hideDuration = 0.3f;
        [SerializeField] private float _fromScale = 0.18f;
        [SerializeField] private float _punchScale = 1.22f;
        [SerializeField] private float _fromAngle = -14f;
        [SerializeField] private float _punchStrength = 0.14f;

        private Sequence _sequence;
        private Vector3 _restScale = Vector3.one;
        private Quaternion _restRotation = Quaternion.identity;
        private bool _restCached;
        private bool _completing;

        private void Awake()
        {
            EnsureRefs();
            CacheRestTransform();
            ApplyHiddenVisuals();
        }

        public async Task PlayAsync(float holdSeconds = 1.8f)
        {
            EnsureRefs();
            CacheRestTransform();
            KillSequence(completeAwaiter: false);

            if (_image != null)
                _image.preserveAspect = true;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            if (_rect != null)
            {
                _rect.localScale = _restScale * _fromScale;
                _rect.localRotation = Quaternion.Euler(0f, 0f, _fromAngle);
            }

            ApplyHiddenVisuals();

            GameManager.Instance?.SoundManager?.PlaySe(PublicVariable.Address.AnnouncerShowdown);

            var tcs = new TaskCompletionSource<bool>();
            _completing = false;
            _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            if (_canvasGroup != null)
                _sequence.Append(_canvasGroup.DOFade(1f, _appearDuration * 0.55f).SetEase(Ease.OutQuad));

            if (_rect != null)
            {
                var appearScale = _rect.DOScale(_restScale * _punchScale, _appearDuration).SetEase(Ease.OutBack);
                var appearRot = _rect.DOLocalRotate(Vector3.zero, _appearDuration).SetEase(Ease.OutBack);
                if (_canvasGroup != null)
                {
                    _sequence.Join(appearScale);
                    _sequence.Join(appearRot);
                }
                else
                {
                    _sequence.Append(appearScale);
                    _sequence.Join(appearRot);
                }

                _sequence.Append(_rect.DOScale(_restScale, 0.12f).SetEase(Ease.OutQuad));
                _sequence.Append(_rect.DOPunchScale(Vector3.one * _punchStrength, 0.38f, 10, 0.65f));
            }

            _sequence.AppendInterval(Mathf.Max(0.05f, holdSeconds));

            if (_canvasGroup != null)
                _sequence.Append(_canvasGroup.DOFade(0f, _hideDuration).SetEase(Ease.InQuad));

            if (_rect != null)
            {
                var hideScale = _rect.DOScale(_restScale * 0.78f, _hideDuration).SetEase(Ease.InBack);
                var hideRot = _rect.DOLocalRotate(new Vector3(0f, 0f, 8f), _hideDuration).SetEase(Ease.InQuad);
                if (_canvasGroup != null)
                {
                    _sequence.Join(hideScale);
                    _sequence.Join(hideRot);
                }
                else
                {
                    _sequence.Append(hideScale);
                    _sequence.Join(hideRot);
                }
            }

            _sequence.OnComplete(() =>
            {
                _completing = true;
                _sequence = null;
                RestoreRestTransform();
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
            RestoreRestTransform();
            ApplyHiddenVisuals();
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

        private void CacheRestTransform()
        {
            if (_restCached || _rect == null)
                return;

            _restScale = _rect.localScale;
            if (_restScale == Vector3.zero)
                _restScale = Vector3.one;
            _restRotation = _rect.localRotation;
            _restCached = true;
        }

        private void RestoreRestTransform()
        {
            if (_rect == null || !_restCached)
                return;

            _rect.localScale = _restScale;
            _rect.localRotation = _restRotation;
        }

        private void EnsureRefs()
        {
            if (_rect == null)
                _rect = transform as RectTransform;

            if (_image == null)
                _image = GetComponent<Image>();

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
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
