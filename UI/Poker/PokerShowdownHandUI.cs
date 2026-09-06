using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SHIN
{
    /// <summary>
    /// 쇼다운 족보 패널 연출.
    /// 플레이어 공개 → 상대 공개 → 승리 쪽 강조.
    /// </summary>
    public class PokerShowdownHandUI : MonoBehaviour
    {
        // StackUI 골드 텍스트와 맞춤
        private static readonly Color RankTextColor = new(0.96f, 0.78f, 0.28f, 1f);
        private static readonly Color WinnerTextColor = new(1f, 0.92f, 0.45f, 1f);
        private static readonly Color LoserTextColor = new(0.78f, 0.72f, 0.62f, 1f);
        private static readonly Color PanelNormalColor = Color.white;
        private static readonly Color PanelWinnerColor = new(1f, 0.97f, 0.88f, 1f);
        private static readonly Color PanelLoserColor = new(0.72f, 0.72f, 0.76f, 1f);

        [SerializeField] private RectTransform _playerPanel;
        [SerializeField] private RectTransform _opponentPanel;
        [SerializeField] private TextMeshProUGUI _playerLabel;
        [SerializeField] private TextMeshProUGUI _opponentLabel;
        [SerializeField] private CanvasGroup _playerGroup;
        [SerializeField] private CanvasGroup _opponentGroup;
        [SerializeField] private Image _playerPanelImage;
        [SerializeField] private Image _opponentPanelImage;
        [SerializeField] private float _revealDuration = 0.42f;
        [SerializeField] private float _fromScale = 0.55f;
        [SerializeField] private float _winnerPunchScale = 1.16f;
        [SerializeField] private float _winnerPunchDuration = 0.55f;

        private Vector3 _playerRestScale = Vector3.one;
        private Vector3 _opponentRestScale = Vector3.one;
        private bool _restCached;

        private void Awake()
        {
            EnsureRefs();
            CacheRestScales();
            ApplyHiddenVisuals();
        }

        /// <summary>
        /// 플레이어 족보 → (호출측 딜레이) → 상대 족보 → (호출측 딜레이) → 승자 연출.
        /// </summary>
        public async Task ShowSequentialAsync(
            string playerHandLabel,
            string opponentHandLabel,
            int compare,
            int opponentDelayMs = 1500,
            int winnerDelayMs = 1500)
        {
            EnsureRefs();
            CacheRestScales();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            ApplyHiddenVisuals();

            if (_playerPanel == null || _opponentPanel == null ||
                _playerLabel == null || _opponentLabel == null)
            {
                Debug.LogWarning("[PokerShowdownHandUI] PlayerHandPanel / OppHandPanel 연결이 필요합니다.");
                return;
            }

            var isTie = compare == 0;
            PrepareLabel(_playerLabel, playerHandLabel, RankTextColor);
            PrepareLabel(_opponentLabel, opponentHandLabel, RankTextColor);
            SetPanelColor(_playerPanelImage, PanelNormalColor);
            SetPanelColor(_opponentPanelImage, PanelNormalColor);

            await RevealPanelAsync(_playerPanel, _playerGroup, _playerRestScale);
            if (this == null)
                return;

            await Task.Delay(Mathf.Max(0, opponentDelayMs));
            if (this == null)
                return;

            await RevealPanelAsync(_opponentPanel, _opponentGroup, _opponentRestScale);
            if (this == null)
                return;

            await Task.Delay(Mathf.Max(0, winnerDelayMs));
            if (this == null)
                return;

            await PlayWinnerAsync(compare, isTie);
        }

        public void HideImmediate()
        {
            KillTweens();
            ApplyHiddenVisuals();
            RestoreRestScales();
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        private async Task RevealPanelAsync(RectTransform panel, CanvasGroup group, Vector3 restScale)
        {
            if (panel == null)
                return;

            group ??= panel.GetComponent<CanvasGroup>() ?? panel.gameObject.AddComponent<CanvasGroup>();
            panel.gameObject.SetActive(true);
            panel.localScale = restScale * _fromScale;
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            PokerSfx.PlayCardFlipShowdown();

            var tcs = new TaskCompletionSource<bool>();
            panel.DOKill();
            group.DOKill();

            var seq = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(panel.gameObject, LinkBehaviour.KillOnDestroy)
                .Append(group.DOFade(1f, _revealDuration).SetEase(Ease.OutQuad))
                .Join(panel.DOScale(restScale * 1.08f, _revealDuration).SetEase(Ease.OutBack))
                .Append(panel.DOScale(restScale, 0.12f).SetEase(Ease.OutQuad))
                .OnComplete(() => tcs.TrySetResult(true))
                .OnKill(() => tcs.TrySetResult(false));

            await tcs.Task;
            _ = seq;
        }

        private async Task PlayWinnerAsync(int compare, bool isTie)
        {
            if (isTie)
            {
                SetPanelColor(_playerPanelImage, PanelWinnerColor);
                SetPanelColor(_opponentPanelImage, PanelWinnerColor);
                if (_playerLabel != null)
                    _playerLabel.color = WinnerTextColor;
                if (_opponentLabel != null)
                    _opponentLabel.color = WinnerTextColor;
                return;
            }

            var playerWins = compare > 0;
            var winPanel = playerWins ? _playerPanel : _opponentPanel;
            var losePanel = playerWins ? _opponentPanel : _playerPanel;
            var winLabel = playerWins ? _playerLabel : _opponentLabel;
            var loseLabel = playerWins ? _opponentLabel : _playerLabel;
            var winImage = playerWins ? _playerPanelImage : _opponentPanelImage;
            var loseImage = playerWins ? _opponentPanelImage : _playerPanelImage;
            var winRest = playerWins ? _playerRestScale : _opponentRestScale;

            SetPanelColor(winImage, PanelWinnerColor);
            SetPanelColor(loseImage, PanelLoserColor);
            if (winLabel != null)
                winLabel.color = WinnerTextColor;
            if (loseLabel != null)
                loseLabel.color = LoserTextColor;

            if (playerWins)
                PokerSfx.PlayWin();
            else
                PokerSfx.PlayLose();

            if (winPanel == null)
                return;

            var tcs = new TaskCompletionSource<bool>();
            winPanel.DOKill();
            winPanel.DOPunchScale(winRest * (_winnerPunchScale - 1f), _winnerPunchDuration, 8, 0.6f)
                .SetUpdate(true)
                .SetLink(winPanel.gameObject, LinkBehaviour.KillOnDestroy)
                .OnComplete(() => tcs.TrySetResult(true))
                .OnKill(() => tcs.TrySetResult(false));

            await tcs.Task;
        }

        private static void PrepareLabel(TextMeshProUGUI label, string text, Color color)
        {
            if (label == null)
                return;

            label.text = text ?? string.Empty;
            label.color = color;
            label.raycastTarget = false;
            label.fontStyle = FontStyles.Bold;
        }

        private static void SetPanelColor(Image image, Color color)
        {
            if (image != null)
                image.color = color;
        }

        private void EnsureRefs()
        {
            if (_playerPanel == null)
            {
                var t = transform.Find("PlayerHandPanel");
                if (t != null)
                    _playerPanel = t as RectTransform;
            }

            if (_opponentPanel == null)
            {
                var t = transform.Find("OppHandPanel");
                if (t != null)
                    _opponentPanel = t as RectTransform;
            }

            if (_playerLabel == null && _playerPanel != null)
                _playerLabel = _playerPanel.GetComponentInChildren<TextMeshProUGUI>(true);

            if (_opponentLabel == null && _opponentPanel != null)
                _opponentLabel = _opponentPanel.GetComponentInChildren<TextMeshProUGUI>(true);

            if (_playerPanelImage == null && _playerPanel != null)
                _playerPanelImage = _playerPanel.GetComponent<Image>();

            if (_opponentPanelImage == null && _opponentPanel != null)
                _opponentPanelImage = _opponentPanel.GetComponent<Image>();

            if (_playerGroup == null && _playerPanel != null)
                _playerGroup = _playerPanel.GetComponent<CanvasGroup>() ?? _playerPanel.gameObject.AddComponent<CanvasGroup>();

            if (_opponentGroup == null && _opponentPanel != null)
                _opponentGroup = _opponentPanel.GetComponent<CanvasGroup>() ?? _opponentPanel.gameObject.AddComponent<CanvasGroup>();
        }

        private void CacheRestScales()
        {
            if (_restCached)
                return;

            if (_playerPanel != null)
            {
                _playerRestScale = _playerPanel.localScale;
                if (_playerRestScale == Vector3.zero)
                    _playerRestScale = Vector3.one;
            }

            if (_opponentPanel != null)
            {
                _opponentRestScale = _opponentPanel.localScale;
                if (_opponentRestScale == Vector3.zero)
                    _opponentRestScale = Vector3.one;
            }

            _restCached = _playerPanel != null || _opponentPanel != null;
        }

        private void RestoreRestScales()
        {
            if (_playerPanel != null)
                _playerPanel.localScale = _playerRestScale;
            if (_opponentPanel != null)
                _opponentPanel.localScale = _opponentRestScale;
        }

        private void ApplyHiddenVisuals()
        {
            HideGroup(_playerGroup, _playerPanel);
            HideGroup(_opponentGroup, _opponentPanel);
        }

        private static void HideGroup(CanvasGroup group, RectTransform panel)
        {
            if (group != null)
            {
                group.DOKill();
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }

            if (panel != null)
                panel.DOKill();
        }

        private void KillTweens()
        {
            _playerPanel?.DOKill();
            _opponentPanel?.DOKill();
            _playerGroup?.DOKill();
            _opponentGroup?.DOKill();
        }

        private void OnDestroy()
        {
            KillTweens();
        }
    }
}
