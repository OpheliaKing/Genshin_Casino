using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SHIN
{
    /// <summary>
    /// 화면 전환용 페이드 오버레이. UI 스택과 별도로 UIManager가 관리한다.
    /// </summary>
    public class FadeUI : MonoBehaviour
    {
        private const int OverlaySortingOrder = 32760;

        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _image;
        [SerializeField] private float _defaultDuration = 0.4f;

        private Canvas _overlayCanvas;
        private Tween _tween;

        private void Awake()
        {
            EnsureComponents();
            BringToFront();
            SetAlpha(0f);
            SetRaycastBlocking(false);
            gameObject.SetActive(true);
        }

        public float DefaultDuration => _defaultDuration;

        public Task FadeOutAsync(float duration = -1f)
        {
            return FadeToAsync(1f, duration);
        }

        public Task FadeInAsync(float duration = -1f)
        {
            return FadeToAsync(0f, duration);
        }

        public async Task FadeToAsync(float targetAlpha, float duration = -1f)
        {
            EnsureComponents();
            if (_canvasGroup == null)
                return;

            if (duration < 0f)
                duration = _defaultDuration;

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            BringToFront();

            _tween?.Kill();
            SetRaycastBlocking(true);

            if (duration <= 0f)
            {
                SetAlpha(targetAlpha);
                if (targetAlpha <= 0.001f)
                    SetRaycastBlocking(false);
                return;
            }

            var tcs = new TaskCompletionSource<bool>();
            _tween = _canvasGroup
                .DOFade(targetAlpha, duration)
                .SetUpdate(true)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() =>
                {
                    SetAlpha(targetAlpha);
                    if (targetAlpha <= 0.001f)
                        SetRaycastBlocking(false);
                    tcs.TrySetResult(true);
                })
                .OnKill(() => tcs.TrySetResult(false));

            await tcs.Task;
            BringToFront();
        }

        public void SetAlphaImmediate(float alpha)
        {
            EnsureComponents();
            _tween?.Kill();
            BringToFront();
            SetAlpha(alpha);
            SetRaycastBlocking(alpha > 0.001f);
        }

        public void BringToFront()
        {
            EnsureComponents();
            transform.SetAsLastSibling();

            if (_overlayCanvas == null)
                return;

            _overlayCanvas.overrideSorting = true;
            _overlayCanvas.sortingOrder = OverlaySortingOrder;
        }

        private void SetAlpha(float alpha)
        {
            if (_canvasGroup == null)
                return;

            _canvasGroup.alpha = Mathf.Clamp01(alpha);

            if (_image != null)
            {
                var color = _image.color;
                color.a = 1f;
                _image.color = color;
            }
        }

        private void SetRaycastBlocking(bool block)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = block;
                _canvasGroup.interactable = block;
            }

            if (_image != null)
                _image.raycastTarget = block;
        }

        private void EnsureComponents()
        {
            if (_image == null)
                _image = GetComponent<Image>();

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (_overlayCanvas == null)
            {
                _overlayCanvas = GetComponent<Canvas>();
                if (_overlayCanvas == null)
                    _overlayCanvas = gameObject.AddComponent<Canvas>();
            }

            if (_overlayCanvas != null)
            {
                _overlayCanvas.overrideSorting = true;
                _overlayCanvas.sortingOrder = OverlaySortingOrder;
            }

            if (GetComponent<GraphicRaycaster>() == null && _overlayCanvas != null)
                gameObject.AddComponent<GraphicRaycaster>();
        }

        private void OnDestroy()
        {
            _tween?.Kill();
        }
    }
}
