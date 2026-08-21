using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SHIN
{
    /// <summary>
    /// 콜/벳/레이즈 시 칩 비행 + 액션 콜아웃 + 팟 펀치.
    /// </summary>
    public class InGameBetFx : MonoBehaviour
    {
        [SerializeField] private RectTransform _fxRoot;
        [SerializeField] private RectTransform _potTarget;
        [SerializeField] private Image _chipTemplate;
        [SerializeField] private TextMeshProUGUI _actionCallout;
        [SerializeField] private TMP_FontAsset _calloutFont;
        [SerializeField] private float _flyDuration = 0.38f;
        [SerializeField] private float _chipStagger = 0.055f;

        private static readonly Color CalloutFill = new(1f, 0.96f, 0.82f, 1f);
        private static readonly Color CalloutOutline = new(0.18f, 0.1f, 0.05f, 1f);

        private Sequence _calloutSequence;
        private Tween _potPunchTween;
        private Vector3 _potRestScale = Vector3.one;
        private bool _potRestCached;

        public void Play(bool isPlayer, PokerAction action, int chipsPaid, RectTransform from)
        {
            EnsureRefs();

            var label = ActionLabel(action);
            if (!string.IsNullOrEmpty(label))
                PlayCallout(label, Intensity(action));

            if (chipsPaid <= 0 || from == null || action == PokerAction.Check || action == PokerAction.Fold)
                return;

            var count = ChipCount(action, chipsPaid);
            FlyChips(from, count, Intensity(action));
            PunchPot(Intensity(action));
        }

        private void EnsureRefs()
        {
            if (_fxRoot == null)
                _fxRoot = transform as RectTransform;

            if (_potTarget == null)
            {
                var pot = FindDeep("PotArea");
                if (pot != null)
                    _potTarget = pot as RectTransform;
            }

            if (_chipTemplate == null)
            {
                var chip = FindDeep("Chip");
                if (chip != null)
                    _chipTemplate = chip.GetComponent<Image>();
            }

            if (_actionCallout == null)
            {
                var existing = transform.Find("BetActionCallout");
                if (existing != null)
                    _actionCallout = existing.GetComponent<TextMeshProUGUI>();
            }

            if (_actionCallout == null)
                CreateCallout();
            else
                ApplyCalloutStyle(_actionCallout);

            RemoveCalloutBackdrop(_actionCallout);

            if (!_potRestCached && _potTarget != null)
            {
                _potRestScale = _potTarget.localScale;
                if (_potRestScale == Vector3.zero)
                    _potRestScale = Vector3.one;
                _potRestCached = true;
            }

            if (_chipTemplate != null)
                _chipTemplate.enabled = true;
        }

        private void CreateCallout()
        {
            var parent = _potTarget != null ? _potTarget.parent : transform;
            var go = new GameObject("BetActionCallout", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(280f, 64f);
            if (_potTarget != null)
                rt.anchoredPosition = _potTarget.anchoredPosition + new Vector2(0f, 90f);
            else
                rt.anchoredPosition = new Vector2(0f, 80f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            ApplyCalloutStyle(tmp);

            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            _actionCallout = tmp;
            go.SetActive(false);
        }

        private void ApplyCalloutStyle(TextMeshProUGUI tmp)
        {
            if (tmp == null)
                return;

            if (_calloutFont != null)
                tmp.font = _calloutFont;

            tmp.fontSize = 46f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            tmp.color = CalloutFill;

            // 공유 머티리얼을 직접 만지지 않고, 유효한 폰트 머티리얼로 인스턴스만 만든다.
            if (tmp.font != null && tmp.font.material != null && tmp.font.atlasTexture != null)
            {
                var mat = new Material(tmp.font.material);
                mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
                mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.32f);
                mat.SetColor(ShaderUtilities.ID_OutlineColor, CalloutOutline);
                tmp.fontMaterial = mat;
            }
            else if (tmp.font != null)
            {
                tmp.fontSharedMaterial = tmp.font.material;
            }
        }

        private static void RemoveCalloutBackdrop(TextMeshProUGUI tmp)
        {
            if (tmp == null)
                return;

            var backdrop = tmp.transform.Find("Backdrop");
            if (backdrop == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(backdrop.gameObject);
            else
                Object.DestroyImmediate(backdrop.gameObject);
        }

        private void PlayCallout(string label, float intensity)
        {
            if (_actionCallout == null)
                return;

            _calloutSequence?.Kill();
            var rt = _actionCallout.rectTransform;
            var cg = _actionCallout.GetComponent<CanvasGroup>() ?? _actionCallout.gameObject.AddComponent<CanvasGroup>();
            _actionCallout.gameObject.SetActive(true);
            _actionCallout.text = label;
            cg.alpha = 0f;
            rt.localScale = Vector3.one * (0.75f + intensity * 0.05f);

            var punch = 1.08f + intensity * 0.06f;
            var hold = 0.45f + intensity * 0.1f;
            _calloutSequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _calloutSequence.Append(cg.DOFade(1f, 0.12f));
            _calloutSequence.Join(rt.DOScale(punch, 0.18f).SetEase(Ease.OutBack));
            _calloutSequence.Append(rt.DOScale(1f, 0.1f));
            _calloutSequence.AppendInterval(hold);
            _calloutSequence.Append(cg.DOFade(0f, 0.2f));
            _calloutSequence.OnComplete(() =>
            {
                if (_actionCallout != null)
                    _actionCallout.gameObject.SetActive(false);
            });
        }

        private void FlyChips(RectTransform from, int count, float intensity)
        {
            if (_chipTemplate == null || _potTarget == null || from == null)
                return;

            var parent = _fxRoot != null ? _fxRoot : _potTarget.parent as RectTransform;
            if (parent == null)
                return;

            var start = from.position;
            var end = _potTarget.position;
            var baseSize = _chipTemplate.rectTransform.sizeDelta;
            var flySize = baseSize * (0.55f + intensity * 0.08f);

            for (var i = 0; i < count; i++)
            {
                var go = Instantiate(_chipTemplate.gameObject, parent);
                go.name = "BetChipFx";
                go.SetActive(true);

                var img = go.GetComponent<Image>();
                if (img != null)
                {
                    img.raycastTarget = false;
                    img.enabled = true;
                    img.color = Color.white;
                }

                var rt = go.GetComponent<RectTransform>();
                rt.SetAsLastSibling();
                rt.sizeDelta = flySize;
                rt.position = start + new Vector3(Random.Range(-12f, 12f), Random.Range(-8f, 8f), 0f);
                rt.localScale = Vector3.one * 0.7f;

                var delay = i * _chipStagger;
                var duration = _flyDuration + intensity * 0.04f;
                var mid = Vector3.Lerp(rt.position, end, 0.5f);
                mid.y += 40f + intensity * 20f + Random.Range(-10f, 18f);
                mid.x += Random.Range(-24f, 24f);

                var seq = DOTween.Sequence().SetUpdate(true).SetLink(go);
                seq.AppendInterval(delay);
                seq.Append(rt.DOScale(1f, duration * 0.35f).SetEase(Ease.OutQuad));
                seq.Join(rt.DOPath(new[] { mid, end }, duration, PathType.CatmullRom)
                    .SetEase(Ease.InOutCubic));
                seq.OnComplete(() =>
                {
                    if (go != null)
                        Destroy(go);
                });
            }
        }

        private void PunchPot(float intensity)
        {
            if (_potTarget == null)
                return;

            _potPunchTween?.Kill(false);
            _potTarget.localScale = _potRestScale;
            var peak = 1f + 0.1f + intensity * 0.08f;
            _potPunchTween = DOTween.Sequence().SetUpdate(true).SetLink(gameObject)
                .AppendInterval(Mathf.Max(0.05f, _flyDuration * 0.65f))
                .Append(_potTarget.DOScale(_potRestScale * peak, 0.12f).SetEase(Ease.OutBack))
                .Append(_potTarget.DOScale(_potRestScale, 0.16f).SetEase(Ease.OutQuad));
        }

        private static string ActionLabel(PokerAction action)
        {
            return action switch
            {
                PokerAction.Check => "CHECK",
                PokerAction.Call => "CALL",
                PokerAction.Bet => "BET",
                PokerAction.Raise => "RAISE",
                PokerAction.AllIn => "ALL IN",
                PokerAction.Fold => "FOLD",
                _ => string.Empty
            };
        }

        private static float Intensity(PokerAction action)
        {
            return action switch
            {
                PokerAction.Check => 0.2f,
                PokerAction.Call => 0.45f,
                PokerAction.Bet => 0.7f,
                PokerAction.Raise => 0.85f,
                PokerAction.AllIn => 1f,
                PokerAction.Fold => 0.35f,
                _ => 0.4f
            };
        }

        private static int ChipCount(PokerAction action, int chipsPaid)
        {
            var byAction = action switch
            {
                PokerAction.Call => 2,
                PokerAction.Bet => 3,
                PokerAction.Raise => 4,
                PokerAction.AllIn => 6,
                _ => 2
            };

            if (chipsPaid >= 40)
                byAction = Mathf.Max(byAction, 5);
            return Mathf.Clamp(byAction, 1, 7);
        }

        private Transform FindDeep(string name)
        {
            var all = GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == name)
                    return all[i];
            }

            return null;
        }

        private void OnDestroy()
        {
            _calloutSequence?.Kill();
            _potPunchTween?.Kill();
        }
    }
}
