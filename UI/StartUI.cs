using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SHIN
{
    /// <summary>
    /// 타이틀(스타트) 화면. 프롬프트 깜빡임 후 클릭/터치로 MainUI로 이동.
    /// </summary>
    public class StartUI : UIBase
    {
        private const string PromptText = "화면을 터치하세요";

        [SerializeField] private TextMeshProUGUI _promptLabel;
        [SerializeField] private CanvasGroup _promptCanvasGroup;
        [SerializeField] private float _blinkDuration = 0.85f;
        [SerializeField] private float _blinkMinAlpha = 0.25f;

        private Tween _blinkTween;
        private bool _inputEnabled;
        private bool _transitionStarted;

        public override void OnShow()
        {
            _transitionStarted = false;
            _inputEnabled = false;
            EnsurePromptLabel();
            ApplyPromptStyle();
            StartBlink();
        }

        public override void OnHide()
        {
            _inputEnabled = false;
            StopBlink();
        }

        /// <summary>페이드인이 끝난 뒤 GameManager에서 호출한다.</summary>
        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
        }

        private void Update()
        {
            if (!_inputEnabled || _transitionStarted)
                return;

            if (WasPrimaryPressThisFrame())
                TryGoToMain();
        }

        private static bool WasPrimaryPressThisFrame()
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                return true;

            var touch = Touchscreen.current;
            return touch != null && touch.primaryTouch.press.wasPressedThisFrame;
        }

        private void TryGoToMain()
        {
            if (!_inputEnabled || _transitionStarted)
                return;

            _transitionStarted = true;
            _inputEnabled = false;
            StopBlink();
            GameManager.Instance?.GoToMainFromStart();
        }

        private void EnsurePromptLabel()
        {
            if (_promptLabel == null)
                _promptLabel = GetComponentInChildren<TextMeshProUGUI>(true);

            if (_promptLabel == null)
                return;

            if (_promptCanvasGroup == null)
            {
                _promptCanvasGroup = _promptLabel.GetComponent<CanvasGroup>();
                if (_promptCanvasGroup == null)
                    _promptCanvasGroup = _promptLabel.gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void ApplyPromptStyle()
        {
            if (_promptLabel == null)
                return;

            _promptLabel.text = PromptText;
            _promptLabel.raycastTarget = false;
        }

        private void StartBlink()
        {
            StopBlink();
            if (_promptCanvasGroup == null)
                return;

            _promptCanvasGroup.alpha = 1f;
            _blinkTween = _promptCanvasGroup
                .DOFade(_blinkMinAlpha, _blinkDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void StopBlink()
        {
            _blinkTween?.Kill();
            _blinkTween = null;

            if (_promptCanvasGroup != null)
                _promptCanvasGroup.alpha = 1f;
        }

        private void OnDestroy()
        {
            StopBlink();
        }
    }
}
