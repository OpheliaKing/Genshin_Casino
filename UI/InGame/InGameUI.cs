using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace SHIN
{
    public class InGameUI : UIBase
    {
        [SerializeField] private Transform _opponentCharacterParent;
        [SerializeField] private Transform[] _playerCardSlots;
        [SerializeField] private Transform[] _opponentCardSlots;
        [SerializeField] private Transform[] _communityCardSlots;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private TextMeshProUGUI _potText;
        [SerializeField] private InGameStackUI _playerStackUI;
        [SerializeField] private InGameStackUI _opponentStackUI;
        [SerializeField] private InGameButton _foldButton;
        [SerializeField] private InGameButton _callButton;
        [SerializeField] private InGameButton _raiseButton;
        [SerializeField] private InGameAnnouncePanel _startPanel;
        [SerializeField] private InGameAnnouncePanel _turnPanel;
        [SerializeField] private DialogUI _dialogUI;
        [SerializeField] private GameResultUI _gameResultUI;
        [SerializeField] private ShowDownUI _showDownUI;
        [SerializeField] private StreetProgressUI _streetProgressUI;
        [SerializeField] private InGameBetFx _betFx;

        private GameObject _opponentModel;
        private CharacterFaceController _opponentFace;
        private readonly List<GameObject> _spawnedCards = new();
        private readonly List<CardObject> _playerCards = new();
        private readonly List<CardObject> _opponentCards = new();
        private readonly List<CardObject> _communityCards = new();
        private InGameManager _match;
        private OpponentData _opponentData;
        private int _displayedPot;
        private bool _hasDisplayedPot;
        private Tween _potTween;

        private const float PotTweenDuration = 0.45f;

        public async Task SetupAsync(OpponentData opponentData)
        {
            _opponentData = opponentData;
            ClearOpponentModel();
            HideDialog();
            _gameResultUI?.HideImmediate();
            _showDownUI?.HideImmediate();
            EnsureStreetProgressUI();
            _streetProgressUI?.ResetToPreflop();
            HideExistingCardsInSlots(_playerCardSlots);
            HideExistingCardsInSlots(_opponentCardSlots);

            if (opponentData == null || _opponentCharacterParent == null || string.IsNullOrEmpty(opponentData.modelPath))
            {
                Debug.LogError("[InGameUI] 상대 모델 정보가 없습니다.");
                return;
            }

            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager == null)
                return;

            var instance = await resourceManager.InstantiateAsync(
                opponentData.modelPath,
                _opponentCharacterParent,
                startInactive: false);

            if (this == null)
            {
                if (instance != null)
                    resourceManager.ReleaseInstance(instance);
                return;
            }

            if (instance == null)
                return;

            _opponentModel = instance;
            ResetLocalTransform(instance.transform);
            BindOpponentFace(instance, opponentData);

            var playerData = GameManager.Instance != null
                ? await GameManager.Instance.EnsurePlayerDataAsync()
                : null;
            if (this == null)
                return;

            if (_playerStackUI != null && playerData != null && !string.IsNullOrEmpty(playerData.iconPath))
            {
                var atlas = !string.IsNullOrEmpty(playerData.atlasAddress)
                    ? playerData.atlasAddress
                    : PublicVariable.Address.CharacterAtlas;
                await _playerStackUI.SetIconAsync(playerData.iconPath, atlas);
            }

            if (_opponentStackUI != null && !string.IsNullOrEmpty(opponentData.iconPath))
                await _opponentStackUI.SetIconAsync(opponentData.iconPath, opponentData.atlasAddress);
        }

        private void BindOpponentFace(GameObject model, OpponentData opponentData)
        {
            _opponentFace = model != null
                ? model.GetComponentInChildren<CharacterFaceController>(true)
                : null;

            if (_opponentFace == null)
                return;

            // 구 UIBlinkLoopPlayer와 충돌 방지
            var blinkers = model.GetComponentsInChildren<UIBlinkLoopPlayer>(true);
            for (var i = 0; i < blinkers.Length; i++)
            {
                if (blinkers[i] != null)
                    blinkers[i].enabled = false;
            }

            _opponentFace.Bind(opponentData);
        }

        public void BindMatch(InGameManager match)
        {
            _match = match;
            if (_foldButton != null)
            {
                _foldButton.RemoveAllClickListeners();
                _foldButton.AddClickListener(() => _match?.OnPlayerAction(PokerAction.Fold));
            }

            if (_callButton != null)
            {
                _callButton.RemoveAllClickListeners();
                _callButton.AddClickListener(() =>
                {
                    if (_match == null)
                        return;
                    _match.OnPlayerAction(_match.PlayerToCall > 0 ? PokerAction.Call : PokerAction.Check);
                });
            }

            if (_raiseButton != null)
            {
                _raiseButton.RemoveAllClickListeners();
                _raiseButton.AddClickListener(() =>
                {
                    if (_match == null)
                        return;
                    // 이미 판돈(블라인드 포함)이 있으면 벳이 아니라 레이즈
                    var isRaise = _match.PlayerToCall > 0 || _match.CurrentBet > 0;
                    _match.OnPlayerAction(isRaise ? PokerAction.Raise : PokerAction.Bet);
                });
            }
        }

        public async Task RefreshCardsAsync(
            IReadOnlyList<PokerCard> playerHole,
            IReadOnlyList<PokerCard> opponentHole,
            IReadOnlyList<PokerCard> board,
            bool revealOpponent,
            bool resetCards = false)
        {
            if (resetCards)
            {
                await ClearCardsAsync();
                if (this == null)
                    return;
            }

            await EnsureHoleCardsAsync(playerHole, _playerCardSlots, _playerCards, true);
            if (this == null)
                return;

            await EnsureHoleCardsAsync(opponentHole, _opponentCardSlots, _opponentCards, revealOpponent);
            if (this == null)
                return;

            // 이미 있는 상대 패는 뒤집기만 (스프라이트 준비까지 await)
            for (var i = 0; i < _opponentCards.Count; i++)
            {
                if (_opponentCards[i] != null)
                    await _opponentCards[i].SetFaceUpAsync(revealOpponent);
            }

            if (this == null)
                return;

            await EnsureBoardCardsAsync(board);
        }

        public void RevealOpponentCards()
        {
            for (var i = 0; i < _opponentCards.Count; i++)
                _opponentCards[i]?.SetFaceUp(true);
        }

        public Task PlayStartAnnounceAsync(string text = "게임 시작", float holdSeconds = 2f)
        {
            EnsureAnnouncePanels();
            if (_startPanel == null)
            {
                Debug.LogWarning("[InGameUI] StartPanel가 연결되지 않았습니다.");
                return Task.CompletedTask;
            }

            return _startPanel.PlayAsync(text, holdSeconds);
        }

        public Task PlayTurnAnnounceAsync(string text, float holdSeconds = 1f)
        {
            EnsureAnnouncePanels();
            if (_turnPanel == null)
            {
                Debug.LogWarning("[InGameUI] TurnPanel이 연결되지 않았습니다.");
                return Task.CompletedTask;
            }

            return _turnPanel.PlayAsync(text, holdSeconds);
        }

        public void PlayBetFx(bool isPlayer, PokerAction action, int chipsPaid)
        {
            EnsureBetFx();
            if (_betFx == null)
                return;

            var from = isPlayer
                ? _playerStackUI != null ? _playerStackUI.transform as RectTransform : null
                : _opponentStackUI != null ? _opponentStackUI.transform as RectTransform : null;
            _betFx.Play(isPlayer, action, chipsPaid, from);
        }

        public void SetStreetProgress(PokerStreet street, bool animate = true)
        {
            EnsureStreetProgressUI();
            _streetProgressUI?.SetStreet(street, animate);
        }

        public Task PlayGameResultAsync(bool playerWins, float holdSeconds = 1.35f)
        {
            EnsureGameResultUI();
            if (_gameResultUI == null)
            {
                Debug.LogWarning("[InGameUI] GameResultUI가 연결되지 않았습니다.");
                return Task.CompletedTask;
            }

            return _gameResultUI.PlayAsync(playerWins, holdSeconds);
        }

        public Task PlayShowDownAsync(float holdSeconds = 1.8f)
        {
            EnsureShowDownUI();
            if (_showDownUI == null)
            {
                Debug.LogWarning("[InGameUI] ShowDownUI가 연결되지 않았습니다.");
                return Task.CompletedTask;
            }

            return _showDownUI.PlayAsync(holdSeconds);
        }

        public void ShowOpponentReaction(CharacterExpressionType type, bool showDialog = true)
        {
            var faceType = type;
            if (_opponentData != null &&
                !_opponentData.TryGetEyeExpression(type, out _) &&
                !_opponentData.TryGetMouthExpression(type, out _))
            {
                faceType = CharacterExpressionType.NORMAL;
            }

            _opponentFace?.SetExpression(faceType);

            if (!showDialog || _opponentData == null)
                return;

            ShowDialog(_opponentData.PickDialog(type), _opponentData.PickVoice(type));
        }

        public void SetOpponentExpression(CharacterExpressionType type)
        {
            _opponentFace?.SetExpression(type);
        }

        public void PlayPlayerVoice(CharacterExpressionType type)
        {
            var playerData = GameManager.Instance?.PlayerData;
            if (playerData == null)
                return;

            var address = playerData.PickVoice(type);
            if (string.IsNullOrWhiteSpace(address))
                return;

            var soundManager = GameManager.Instance?.SoundManager;
            if (soundManager == null)
            {
                Debug.LogWarning("[InGameUI] SoundManager가 없습니다.");
                return;
            }

            soundManager.PlaySe(address);
        }

        public void ShowDialog(string message, string voiceAddress = null)
        {
            EnsureDialogUI();
            if (_dialogUI == null)
            {
                Debug.LogWarning("[InGameUI] DialogUI가 연결되지 않았습니다.");
                return;
            }

            _dialogUI.Show(message, voiceAddress);
        }

        public void HideDialog()
        {
            EnsureDialogUI();
            _dialogUI?.Hide();
        }

        private void EnsureAnnouncePanels()
        {
            if (_startPanel == null)
            {
                var start = transform.Find("InGameStartPanel");
                if (start != null)
                    _startPanel = start.GetComponent<InGameAnnouncePanel>();
            }

            if (_turnPanel == null)
            {
                var turn = transform.Find("TurnPanel");
                if (turn != null)
                    _turnPanel = turn.GetComponent<InGameAnnouncePanel>();
            }
        }

        private void EnsureDialogUI()
        {
            if (_dialogUI != null)
                return;

            var found = GetComponentInChildren<DialogUI>(true);
            if (found != null)
            {
                _dialogUI = found;
                return;
            }

            var t = transform.Find("DialogUI");
            if (t != null)
                _dialogUI = t.GetComponent<DialogUI>() ?? t.gameObject.AddComponent<DialogUI>();
        }

        private void EnsureGameResultUI()
        {
            if (_gameResultUI != null)
                return;

            var found = GetComponentInChildren<GameResultUI>(true);
            if (found != null)
            {
                _gameResultUI = found;
                return;
            }

            var t = transform.Find("GameResultUI");
            if (t != null)
                _gameResultUI = t.GetComponent<GameResultUI>() ?? t.gameObject.AddComponent<GameResultUI>();
        }

        private void EnsureShowDownUI()
        {
            if (_showDownUI != null)
                return;

            var found = GetComponentInChildren<ShowDownUI>(true);
            if (found != null)
            {
                _showDownUI = found;
                return;
            }

            var t = transform.Find("ShowDownUI");
            if (t != null)
                _showDownUI = t.GetComponent<ShowDownUI>() ?? t.gameObject.AddComponent<ShowDownUI>();
        }

        private void EnsureStreetProgressUI()
        {
            if (_streetProgressUI != null)
                return;

            _streetProgressUI = GetComponentInChildren<StreetProgressUI>(true);
            if (_streetProgressUI != null)
                return;

            Debug.LogWarning("[InGameUI] StreetProgressUI가 프리팹에 연결되어 있지 않습니다.");
        }

        private void EnsureBetFx()
        {
            if (_betFx != null)
                return;

            _betFx = GetComponentInChildren<InGameBetFx>(true);
            if (_betFx != null)
                return;

            _betFx = gameObject.AddComponent<InGameBetFx>();
        }

        public void RefreshHud(string status, int pot, int playerStack, int opponentStack, bool playerTurn, int toCall, bool matchOver, int currentBet = 0)
        {
            if (_statusText != null)
                _statusText.text = status;

            TweenPot(pot);

            _playerStackUI?.SetGold(playerStack);
            _opponentStackUI?.SetGold(opponentStack);

            var interactable = playerTurn && !matchOver;
            if (_foldButton != null)
            {
                _foldButton.Interactable = interactable && toCall > 0;
                _foldButton.SetLabel("폴드");
            }

            if (_callButton != null)
            {
                _callButton.Interactable = interactable;
                _callButton.SetLabel(toCall > 0 ? $"콜 {toCall}" : "체크");
            }

            if (_raiseButton != null)
            {
                _raiseButton.Interactable = interactable;
                // toCall이 0이어도 블라인드 등으로 currentBet이 있으면 레이즈
                var isRaise = toCall > 0 || currentBet > 0;
                _raiseButton.SetLabel(isRaise ? "레이즈" : "벳");
            }
        }

        private async Task EnsureHoleCardsAsync(
            IReadOnlyList<PokerCard> cards,
            Transform[] slots,
            List<CardObject> bucket,
            bool faceUp)
        {
            // 파괴된 참조가 남아 스폰을 건너뛰지 않도록 정리
            for (var i = bucket.Count - 1; i >= 0; i--)
            {
                if (bucket[i] == null)
                    bucket.RemoveAt(i);
            }

            if (bucket.Count > 0)
                return;

            await SpawnCardsAsync(cards, slots, bucket, faceUp);
        }

        private async Task EnsureBoardCardsAsync(IReadOnlyList<PokerCard> board)
        {
            if (board == null || _communityCardSlots == null)
                return;

            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager == null)
                return;

            var targetCount = Mathf.Min(board.Count, _communityCardSlots.Length);
            for (var i = _communityCards.Count; i < targetCount; i++)
            {
                var slot = _communityCardSlots[i];
                if (slot == null)
                {
                    Debug.LogError($"[InGameUI] 커뮤니티 카드 슬롯 {i}이 비어 있습니다.");
                    continue;
                }

                var instance = await resourceManager.InstantiateAsync(
                    PublicVariable.Address.CardItem,
                    slot,
                    startInactive: false);

                if (this == null)
                {
                    if (instance != null)
                        resourceManager.ReleaseInstance(instance);
                    return;
                }

                if (instance == null)
                    continue;

                FitToSlot(instance.transform);
                _spawnedCards.Add(instance);

                var cardObject = instance.GetComponent<CardObject>();
                if (cardObject == null)
                    cardObject = instance.AddComponent<CardObject>();

                await cardObject.BindAsync(board[i], true);
                if (this == null)
                    return;

                _communityCards.Add(cardObject);
            }
        }

        private async Task SpawnCardsAsync(
            IReadOnlyList<PokerCard> cards,
            Transform[] slots,
            List<CardObject> bucket,
            bool faceUp)
        {
            bucket.Clear();
            if (cards == null || slots == null)
                return;

            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager == null)
                return;

            var count = Mathf.Min(cards.Count, slots.Length);
            for (var i = 0; i < count; i++)
            {
                var slot = slots[i];
                if (slot == null)
                {
                    Debug.LogError($"[InGameUI] 카드 슬롯 {i}이 비어 있습니다.");
                    continue;
                }

                var instance = await resourceManager.InstantiateAsync(
                    PublicVariable.Address.CardItem,
                    slot,
                    startInactive: false);

                if (this == null)
                {
                    if (instance != null)
                        resourceManager.ReleaseInstance(instance);
                    return;
                }

                if (instance == null)
                    continue;

                FitToSlot(instance.transform);
                instance.transform.SetAsLastSibling();
                if (slot != null)
                    slot.SetAsLastSibling();
                _spawnedCards.Add(instance);

                var cardObject = instance.GetComponent<CardObject>();
                if (cardObject == null)
                    cardObject = instance.AddComponent<CardObject>();

                await cardObject.BindAsync(cards[i], faceUp);
                if (this == null)
                    return;

                // 앞면 카드가 아틀라스 실패로 숨겨지지 않도록 한 번 더 보장
                if (faceUp)
                    await cardObject.SetFaceUpAsync(true);
                if (this == null)
                    return;

                bucket.Add(cardObject);
            }
        }

        private async Task ClearCardsAsync()
        {
            var resourceManager = GameManager.Instance?.ResourceManager;
            for (var i = 0; i < _spawnedCards.Count; i++)
            {
                var instance = _spawnedCards[i];
                if (instance == null)
                    continue;

                if (resourceManager != null)
                    resourceManager.ReleaseInstance(instance);
                else
                    Destroy(instance);
            }

            _spawnedCards.Clear();
            _playerCards.Clear();
            _opponentCards.Clear();
            _communityCards.Clear();
            await Task.Yield();
        }

        private static void HideExistingCardsInSlots(Transform[] slots)
        {
            if (slots == null)
                return;

            for (var i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null)
                    continue;

                for (var c = 0; c < slot.childCount; c++)
                {
                    var child = slot.GetChild(c);
                    if (child.GetComponent<CardObject>() != null)
                        child.gameObject.SetActive(false);
                }
            }
        }

        private static void FitToSlot(Transform target)
        {
            target.localRotation = Quaternion.identity;
            target.localScale = Vector3.one;

            if (target is not RectTransform rect)
            {
                target.localPosition = Vector3.zero;
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }

        private void ClearOpponentModel()
        {
            _opponentFace = null;
            if (_opponentModel == null)
                return;

            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager != null)
                resourceManager.ReleaseInstance(_opponentModel);
            else
                Destroy(_opponentModel);

            _opponentModel = null;
        }

        private void TweenPot(int pot)
        {
            if (_potText == null)
                return;

            if (!_hasDisplayedPot)
            {
                _hasDisplayedPot = true;
                _displayedPot = pot;
                _potText.text = pot.ToString();
                return;
            }

            if (_displayedPot == pot)
                return;

            _potTween?.Kill();
            _potTween = DOTween
                .To(() => _displayedPot, value =>
                {
                    _displayedPot = value;
                    _potText.text = value.ToString();
                }, pot, PotTweenDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private static void ResetLocalTransform(Transform target)
        {
            target.localPosition = Vector3.zero;
            target.localRotation = Quaternion.identity;
            target.localScale = Vector3.one;

            if (target is RectTransform rect)
            {
                rect.anchoredPosition = Vector2.zero;
                rect.localRotation = Quaternion.identity;
            }
        }

        private void OnDestroy()
        {
            _potTween?.Kill();
            _potTween = null;
            _ = ClearCardsAsync();
            ClearOpponentModel();
        }
    }
}
