using System;
using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;

namespace SHIN
{
    public class VersusUI : UIBase
    {
        [Header("Characters")]
        [SerializeField] private RectTransform _player;
        [SerializeField] private RectTransform _opponent;
        [SerializeField] private Image _playerImage;
        [SerializeField] private Image _opponentImage;

        [Header("Title")]
        [SerializeField] private RectTransform _title;
        [SerializeField] private CanvasGroup _titleCanvasGroup;

        [Header("Names")]
        [SerializeField] private CanvasGroup _playerNamePanel;
        [SerializeField] private CanvasGroup _opponentNamePanel;
        [SerializeField] private TextMeshProUGUI _playerNameText;
        [SerializeField] private TextMeshProUGUI _opponentNameText;

        [Header("Motion")]
        [SerializeField] private float _enterOffsetX = 900f;
        [SerializeField] private float _clashOffsetX = 110f;
        [SerializeField] private float _titleStartOffsetY = 900f;
        [SerializeField] private float _rushDuration = 0.28f;
        [SerializeField] private float _settleDuration = 0.35f;
        [SerializeField] private float _titleDropDuration = 0.45f;
        [SerializeField] private float _nameFadeDuration = 0.25f;
        [SerializeField] private float _holdAfterIntro = 0.35f;
        [SerializeField] private float _minimumDuration = 3f;

        private Vector2 _playerRestPos;
        private Vector2 _opponentRestPos;
        private Vector2 _titleRestPos;
        private bool _restCached;
        private Sequence _introSequence;
        private bool _playing;

        private void Awake()
        {
            CacheRestPositions();
            EnsureReferences();
        }

        public void Begin(OpponentData opponentData, Action onComplete)
        {
            if (_playing)
                return;

            _ = PlayAsync(opponentData, onComplete);
        }

        public override void OnHide()
        {
            KillIntro();
        }

        private async Task PlayAsync(OpponentData opponentData, Action onComplete)
        {
            _playing = true;
            KillIntro(resetPlaying: false);
            CacheRestPositions();
            EnsureReferences();

            var startedAt = Time.unscaledTime;

            var playerData = GameManager.Instance != null
                ? await GameManager.Instance.EnsurePlayerDataAsync()
                : null;
            if (this == null)
                return;

            ApplyNames(opponentData, playerData);
            await Task.WhenAll(
                ApplyPlayerVsImageAsync(playerData),
                ApplyOpponentVsImageAsync(opponentData));
            if (this == null)
                return;

            PrepareIntroPose();

            _introSequence = DOTween.Sequence().SetUpdate(true);

            // 1) 양측이 중앙으로 박치기
            if (_player != null)
            {
                _introSequence.Append(
                    _player.DOAnchorPos(new Vector2(-_clashOffsetX, _playerRestPos.y), _rushDuration)
                        .SetEase(Ease.InQuad));
            }

            if (_opponent != null)
            {
                var opponentClash = _opponent.DOAnchorPos(new Vector2(_clashOffsetX, _opponentRestPos.y), _rushDuration)
                    .SetEase(Ease.InQuad);
                if (_player != null)
                    _introSequence.Join(opponentClash);
                else
                    _introSequence.Append(opponentClash);
            }

            // 2) 프리팹 배치 위치로 복귀
            if (_player != null)
            {
                _introSequence.Append(
                    _player.DOAnchorPos(_playerRestPos, _settleDuration)
                        .SetEase(Ease.OutBack));
            }

            if (_opponent != null)
            {
                var opponentSettle = _opponent.DOAnchorPos(_opponentRestPos, _settleDuration)
                    .SetEase(Ease.OutBack);
                if (_player != null)
                    _introSequence.Join(opponentSettle);
                else
                    _introSequence.Append(opponentSettle);
            }

            // 3) VS 타이틀 하강
            if (_title != null)
            {
                _introSequence.Append(
                    _title.DOAnchorPos(_titleRestPos, _titleDropDuration)
                        .SetEase(Ease.OutBack));

                if (_titleCanvasGroup != null)
                {
                    _introSequence.Join(
                        _titleCanvasGroup.DOFade(1f, _titleDropDuration * 0.5f));
                }
            }

            // 4) 이름 패널
            if (_playerNamePanel != null)
                _introSequence.Append(_playerNamePanel.DOFade(1f, _nameFadeDuration));
            if (_opponentNamePanel != null)
            {
                if (_playerNamePanel != null)
                    _introSequence.Join(_opponentNamePanel.DOFade(1f, _nameFadeDuration));
                else
                    _introSequence.Append(_opponentNamePanel.DOFade(1f, _nameFadeDuration));
            }

            _introSequence.AppendInterval(_holdAfterIntro);

            var finishedOk = await WaitSequenceAsync(_introSequence);
            if (!finishedOk || this == null)
            {
                _playing = false;
                return;
            }

            // 최소 유지 시간 보장
            var elapsed = Time.unscaledTime - startedAt;
            var remain = _minimumDuration - elapsed;
            if (remain > 0f)
                await DelayUnscaledAsync(remain);

            if (this == null)
            {
                _playing = false;
                return;
            }

            _playing = false;
            onComplete?.Invoke();
        }

        private void ApplyNames(OpponentData opponentData, PlayerData playerData)
        {
            if (_playerNameText != null)
            {
                _playerNameText.text = playerData != null && !string.IsNullOrEmpty(playerData.name)
                    ? playerData.name
                    : "플레이어";
            }

            if (_opponentNameText != null)
                _opponentNameText.text = opponentData != null ? opponentData.name : string.Empty;
        }

        private Task ApplyPlayerVsImageAsync(PlayerData playerData)
        {
            if (playerData == null || string.IsNullOrEmpty(playerData.vsImagePath))
            {
                Debug.LogWarning("[VersusUI] PlayerData vsImagePath가 비어 있습니다.");
                return Task.CompletedTask;
            }

            var atlas = !string.IsNullOrEmpty(playerData.atlasAddress)
                ? playerData.atlasAddress
                : PublicVariable.Address.CharacterAtlas;

            return ApplyVsImageAsync(_playerImage, playerData.vsImagePath, atlas);
        }

        private Task ApplyOpponentVsImageAsync(OpponentData opponentData)
        {
            if (opponentData == null)
                return Task.CompletedTask;

            if (string.IsNullOrEmpty(opponentData.atlasAddress) || string.IsNullOrEmpty(opponentData.vsImagePath))
            {
                Debug.LogWarning("[VersusUI] atlasAddress 또는 vsImagePath가 비어 있습니다.");
                return Task.CompletedTask;
            }

            return ApplyVsImageAsync(_opponentImage, opponentData.vsImagePath, opponentData.atlasAddress);
        }

        private async Task ApplyVsImageAsync(Image target, string spriteName, string atlasAddress)
        {
            if (target == null || string.IsNullOrEmpty(spriteName))
                return;

            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager == null)
                return;

            Sprite sprite = null;
            if (!string.IsNullOrEmpty(atlasAddress))
            {
                var atlas = await resourceManager.LoadAsync<SpriteAtlas>(atlasAddress);
                if (this == null)
                    return;

                if (atlas == null)
                    Debug.LogError($"[VersusUI] 아틀라스 로드 실패: {atlasAddress}");
                else
                    sprite = atlas.GetSprite(spriteName);
            }

            if (sprite == null)
            {
                Debug.LogWarning($"[VersusUI] VS 이미지를 찾지 못했습니다: {spriteName}");
                return;
            }

            target.sprite = sprite;
            target.preserveAspect = true;
        }

        private void PrepareIntroPose()
        {
            if (_player != null)
                _player.anchoredPosition = new Vector2(_playerRestPos.x - _enterOffsetX, _playerRestPos.y);

            if (_opponent != null)
                _opponent.anchoredPosition = new Vector2(_opponentRestPos.x + _enterOffsetX, _opponentRestPos.y);

            if (_title != null)
                _title.anchoredPosition = new Vector2(_titleRestPos.x, _titleRestPos.y + _titleStartOffsetY);

            if (_titleCanvasGroup != null)
                _titleCanvasGroup.alpha = 0f;

            if (_playerNamePanel != null)
                _playerNamePanel.alpha = 0f;

            if (_opponentNamePanel != null)
                _opponentNamePanel.alpha = 0f;
        }

        private void CacheRestPositions()
        {
            if (_restCached)
                return;

            if (_player != null)
                _playerRestPos = _player.anchoredPosition;

            if (_opponent != null)
                _opponentRestPos = _opponent.anchoredPosition;

            if (_title != null)
                _titleRestPos = _title.anchoredPosition;

            _restCached = _player != null || _opponent != null || _title != null;
        }

        private void EnsureReferences()
        {
            if (_player == null)
            {
                var t = transform.Find("versus_player");
                if (t != null)
                    _player = t as RectTransform;
            }

            if (_opponent == null)
            {
                var t = transform.Find("versus_Opponent");
                if (t != null)
                    _opponent = t as RectTransform;
            }

            if (_playerImage == null && _player != null)
                _playerImage = _player.GetComponent<Image>();

            if (_opponentImage == null && _opponent != null)
                _opponentImage = _opponent.GetComponent<Image>();

            if (_title == null)
            {
                var t = transform.Find("title");
                if (t != null)
                    _title = t as RectTransform;
            }

            if (_title != null && _titleCanvasGroup == null)
                _titleCanvasGroup = _title.GetComponent<CanvasGroup>() ?? _title.gameObject.AddComponent<CanvasGroup>();

            if (_playerNamePanel == null && _player != null)
            {
                var panel = _player.Find("namePanel");
                if (panel != null)
                    _playerNamePanel = panel.GetComponent<CanvasGroup>() ?? panel.gameObject.AddComponent<CanvasGroup>();
            }

            if (_opponentNamePanel == null && _opponent != null)
            {
                var panel = _opponent.Find("namePanel");
                if (panel != null)
                    _opponentNamePanel = panel.GetComponent<CanvasGroup>() ?? panel.gameObject.AddComponent<CanvasGroup>();
            }

            if (_playerNameText == null && _playerNamePanel != null)
                _playerNameText = _playerNamePanel.GetComponentInChildren<TextMeshProUGUI>(true);

            if (_opponentNameText == null && _opponentNamePanel != null)
                _opponentNameText = _opponentNamePanel.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        private void KillIntro(bool resetPlaying = true)
        {
            if (_introSequence != null && _introSequence.IsActive())
                _introSequence.Kill();
            _introSequence = null;

            if (resetPlaying)
                _playing = false;
        }

        private static async Task<bool> WaitSequenceAsync(Sequence sequence)
        {
            if (sequence == null)
                return false;

            var tcs = new TaskCompletionSource<bool>();
            sequence.OnComplete(() => tcs.TrySetResult(true));
            sequence.OnKill(() => tcs.TrySetResult(false));
            return await tcs.Task;
        }

        private async Task DelayUnscaledAsync(float seconds)
        {
            if (seconds <= 0f)
                return;

            var end = Time.unscaledTime + seconds;
            while (this != null && Time.unscaledTime < end)
                await Task.Yield();
        }

        private void OnDestroy()
        {
            KillIntro();
        }
    }
}
