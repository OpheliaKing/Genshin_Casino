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
        private const string PromptText = "Touch to Start";

        [SerializeField] private TextMeshProUGUI _promptLabel;
        [SerializeField] private CanvasGroup _promptCanvasGroup;
        [SerializeField] private float _blinkDuration = 0.85f;
        [SerializeField] private float _blinkMinAlpha = 0.45f;

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
            // Cinzel Decorative는 이미 Bold 계열
            _promptLabel.fontStyle = FontStyles.Normal;
            // 타이틀 키아트(밝은 금색) 대비용 크림 톤
            _promptLabel.color = new Color(1f, 0.96f, 0.82f, 1f);

            // 외곽선 + 언더레이로 배경과 분리
            var mat = _promptLabel.fontMaterial;
            if (mat == null)
                return;

            mat.EnableKeyword("OUTLINE_ON");
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
            mat.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0f);
            mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.12f, 0.06f, 0.02f, 1f));

            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.6f);
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.6f);
            mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.15f);
            mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.2f);
            mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.75f));
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
