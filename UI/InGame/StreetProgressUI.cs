using DG.Tweening;
using TMPro;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 프리플랍→플랍→턴→리버 진행도 HUD. 라벨은 프리팹에 배치하고 여기서 상태만 갱신한다.
    /// </summary>
    public class StreetProgressUI : MonoBehaviour
    {
        private static readonly string[] StepHints =
        {
            "내 손패 2장만으로 배팅하는 단계",
            "테이블에 공용 카드 3장 공개",
            "공용 카드 1장 추가",
            "마지막 공용 카드 공개",
            "패를 비교해 승패 결정"
        };

        [SerializeField] private TextMeshProUGUI[] _stepLabels;
        [SerializeField] private TextMeshProUGUI _hintText;
        [SerializeField] private Color _pastColor = new(0.35f, 0.3f, 0.28f, 0.7f);
        [SerializeField] private Color _currentColor = new(0.55f, 0.38f, 0.18f, 1f);
        [SerializeField] private Color _futureColor = new(0.45f, 0.4f, 0.38f, 0.4f);
        [SerializeField] private float _currentScale = 1.12f;
        [SerializeField] private float _punchDuration = 0.28f;

        private int _activeIndex = -1;
        private Vector3[] _restScales;
        private Tween _punchTween;
        private bool _restCached;

        private void Awake()
        {
            CacheRestScales();
            ApplyVisuals(0, animate: false);
        }

        public void SetStreet(PokerStreet street, bool animate = true)
        {
            CacheRestScales();
            var index = street switch
            {
                PokerStreet.Preflop => 0,
                PokerStreet.Flop => 1,
                PokerStreet.Turn => 2,
                PokerStreet.River => 3,
                PokerStreet.Showdown => 4,
                _ => 0
            };
            ApplyVisuals(index, animate);
        }

        public void ResetToPreflop(bool animate = false)
        {
            SetStreet(PokerStreet.Preflop, animate);
        }

        private void ApplyVisuals(int index, bool animate)
        {
            if (_stepLabels == null || _stepLabels.Length == 0)
                return;

            var changed = _activeIndex != index;
            _activeIndex = index;

            for (var i = 0; i < _stepLabels.Length; i++)
            {
                var label = _stepLabels[i];
                if (label == null)
                    continue;

                Color color;
                float scale;
                if (index >= 4)
                {
                    color = _pastColor;
                    scale = 1f;
                }
                else if (i < index)
                {
                    color = _pastColor;
                    scale = 1f;
                }
                else if (i == index)
                {
                    color = _currentColor;
                    scale = _currentScale;
                }
                else
                {
                    color = _futureColor;
                    scale = 1f;
                }

                label.color = color;
                var rest = _restScales != null && i < _restScales.Length ? _restScales[i] : Vector3.one;
                label.rectTransform.localScale = rest * scale;
            }

            if (_hintText != null)
            {
                var hintIndex = Mathf.Clamp(index, 0, StepHints.Length - 1);
                _hintText.text = StepHints[hintIndex];
                _hintText.color = index >= 4 ? _pastColor : _currentColor;
            }

            if (!animate || !changed || index < 0 || index >= _stepLabels.Length)
                return;

            var current = _stepLabels[index];
            if (current == null)
                return;

            _punchTween?.Kill();
            var targetScale = (_restScales != null && index < _restScales.Length ? _restScales[index] : Vector3.one) * _currentScale;
            current.rectTransform.localScale = targetScale * 0.85f;
            _punchTween = current.rectTransform
                .DOScale(targetScale, _punchDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void CacheRestScales()
        {
            if (_restCached || _stepLabels == null)
                return;

            _restScales = new Vector3[_stepLabels.Length];
            for (var i = 0; i < _stepLabels.Length; i++)
            {
                if (_stepLabels[i] == null)
                {
                    _restScales[i] = Vector3.one;
                    continue;
                }

                var s = _stepLabels[i].rectTransform.localScale;
                _restScales[i] = s == Vector3.zero ? Vector3.one : s;
            }

            _restCached = true;
        }

        private void OnDestroy()
        {
            _punchTween?.Kill();
            _punchTween = null;
        }
    }
}
