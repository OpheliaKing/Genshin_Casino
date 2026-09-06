using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace SHIN
{
    public class InGamePokerUI : UIBase
    {
        [SerializeField] private Transform _opponentCharacterParent;
        [SerializeField] private Transform[] _playerCardSlots;
        [SerializeField] private Transform[] _opponentCardSlots;
        [SerializeField] private Transform[] _communityCardSlots;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private TextMeshProUGUI _potText;
        [SerializeField] private PokerStackUI _playerStackUI;
        [SerializeField] private PokerStackUI _opponentStackUI;
        [SerializeField] private PokerButton _foldButton;
        [SerializeField] private PokerButton _callButton;
        [SerializeField] private PokerButton _raiseButton;
        [SerializeField] private PokerAnnouncePanel _startPanel;
        [SerializeField] private PokerAnnouncePanel _turnPanel;
        [SerializeField] private PokerDialogUI _dialogUI;
        [SerializeField] private PokerGameResultUI _gameResultUI;
        [SerializeField] private PokerShowDownUI _showDownUI;
        [SerializeField] private PokerShowdownHandUI _showdownHandUI;
        [SerializeField] private PokerStreetProgressUI _streetProgressUI;
        [SerializeField] private PokerBetFx _betFx;

        private GameObject _opponentModel;
        private CharacterFaceController _opponentFace;
        private readonly List<GameObject> _spawnedCards = new();
        private readonly List<PokerCardObject> _playerCards = new();
        private readonly List<PokerCardObject> _opponentCards = new();
        private readonly List<PokerCardObject> _communityCards = new();
        private PokerMatchManager _match;
        private OpponentData _opponentData;
        private int _displayedPot;
        private bool _hasDisplayedPot;
        private Tween _potTween;

        private const float PotTweenDuration = 0.45f;
        private const float ShowdownBannerHold = 1.6f;
        private const int ShowdownAfterBannerDelayMs = 1000;
        private const int ShowdownOpponentRevealDelayMs = 1500;
        private const int ShowdownWinnerRevealDelayMs = 1500;
        private const int ShowdownHandHoldMs = 2200;

        private static bool _gameStartRunning;

        /// <summary>상대 선택 후 포커 매치 진입 (Versus → Poker UI).</summary>
        public static void GameStart(OpponentData opponentData)
        {
            if (opponentData == null)
            {
                Debug.LogError("[InGamePokerUI] GameStart opponentData가 없습니다.");
                return;
            }

            if (_gameStartRunning)
                return;

            _gameStartRunning = true;
            _ = GameStartAsync(opponentData);
        }

        private static async Task GameStartAsync(OpponentData opponentData)
        {
            try
            {
                var gameManager = GameManager.Instance;
                var uiManager = gameManager?.UIManager;
                if (gameManager == null || uiManager == null)
                    return;

                await gameManager.EnsurePlayerDataAsync();
                if (gameManager == null)
                    return;

                var preloadTask = PreloadMatchResourcesAsync(opponentData);

                var versusReady = new TaskCompletionSource<VersusUI>();
                uiManager.Show(PublicVariable.Address.VersusUI, ui =>
                {
                    versusReady.TrySetResult(ui as VersusUI);
                });

                var versusUI = await versusReady.Task;
                if (gameManager == null)
                    return;

                PokerMatchManager match = null;

                if (versusUI == null)
                {
                    Debug.LogError("[InGamePokerUI] VersusUI를 찾지 못했습니다.");
                    await preloadTask;
                    if (gameManager == null)
                        return;

                    await uiManager.FadeTransitionAsync(async () =>
                    {
                        match = await ShowAndSetupMatchAsync(uiManager, opponentData);
                    });
                    if (match != null)
                        await match.BeginGameplayAsync();
                    return;
                }

                var introDone = new TaskCompletionSource<bool>();
                versusUI.Begin(opponentData, () => introDone.TrySetResult(true));

                await introDone.Task;
                if (gameManager == null)
                    return;

                await preloadTask;
                if (gameManager == null)
                    return;

                var versusToClose = versusUI;
                await uiManager.FadeTransitionAsync(async () =>
                {
                    uiManager.Close(versusToClose, restoreVisibleStack: false);
                    match = await ShowAndSetupMatchAsync(uiManager, opponentData);
                });

                if (match != null)
                    await match.BeginGameplayAsync();
            }
            finally
            {
                _gameStartRunning = false;
            }
        }

        private static async Task<PokerMatchManager> ShowAndSetupMatchAsync(UIManager uiManager, OpponentData opponentData)
        {
            var shown = new TaskCompletionSource<InGamePokerUI>();
            uiManager.Show(PublicVariable.Address.InGamePokerUI, ui =>
            {
                shown.TrySetResult(ui as InGamePokerUI);
            });

            var pokerUI = await shown.Task;
            if (pokerUI == null)
            {
                Debug.LogError("[InGamePokerUI] InGamePokerUI Show에 실패했습니다.");
                return null;
            }

            var match = pokerUI.GetComponent<PokerMatchManager>();
            if (match == null)
                match = pokerUI.gameObject.AddComponent<PokerMatchManager>();

            await match.SetupMatchAsync(pokerUI, opponentData);
            return match;
        }

        private static async Task PreloadMatchResourcesAsync(OpponentData opponentData)
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null)
                return;

            var playerData = await gameManager.EnsurePlayerDataAsync();
            if (gameManager == null)
                return;

            var uiPreload = gameManager.UIManager.PreloadPokerUIAsync();
            var opponentPreload = PreloadOpponentAsync(opponentData);
            var voicePreload = PreloadMatchVoicesAsync(playerData, opponentData);
            await Task.WhenAll(uiPreload, opponentPreload, voicePreload);
        }

        private static async Task PreloadMatchVoicesAsync(PlayerData playerData, OpponentData opponentData)
        {
            var soundManager = GameManager.Instance?.SoundManager;
            if (soundManager == null)
                return;

            var addresses = new List<string>
            {
                PublicVariable.Address.AnnouncerShowdown,
                PublicVariable.Address.AnnouncerWin,
                PublicVariable.Address.AnnouncerLose,
                PublicVariable.Address.InGameBgm,
                PublicVariable.Address.SeCardDraw,
                PublicVariable.Address.SeCardFlip,
                PublicVariable.Address.SeCardFlipShowdown,
                PublicVariable.Address.SeCardShuffle,
                PublicVariable.Address.SeChipBet,
                PublicVariable.Address.SeChipUp,
                PublicVariable.Address.SePotUp,
                PublicVariable.Address.SeUiClick,
                PublicVariable.Address.SeUiShowTurnPopup,
                PublicVariable.Address.SeWin,
                PublicVariable.Address.SeLose,
                PublicVariable.Address.SeCharacterDialog,
                PublicVariable.Address.SeHandWin,
                PublicVariable.Address.SeVersusStart,
                PublicVariable.Address.SeVersusClash
            };
            playerData?.CollectVoiceAddresses(addresses);
            opponentData?.CollectVoiceAddresses(addresses);

            await soundManager.PreloadAsync(addresses);
        }

        private static async Task PreloadOpponentAsync(OpponentData opponentData)
        {
            if (opponentData == null)
                return;

            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager == null)
                return;

            var tasks = new List<Task>();

            if (!string.IsNullOrEmpty(opponentData.modelPath))
                tasks.Add(resourceManager.LoadAsync<GameObject>(opponentData.modelPath));

            var atlasAddress = !string.IsNullOrEmpty(opponentData.atlasAddress)
                ? opponentData.atlasAddress
                : PublicVariable.Address.CharacterAtlas;
            if (!string.IsNullOrEmpty(atlasAddress))
                tasks.Add(resourceManager.LoadAsync<UnityEngine.U2D.SpriteAtlas>(atlasAddress));

            if (tasks.Count == 0)
                return;

            await Task.WhenAll(tasks);
        }

        public async Task SetupAsync(OpponentData opponentData)
        {
            _opponentData = opponentData;
            ClearOpponentModel();
            HideDialog();
            _gameResultUI?.HideImmediate();
            _showDownUI?.HideImmediate();
            _showdownHandUI?.HideImmediate();
            ClearShowdownVisuals();
            EnsurePokerStreetProgressUI();
            _streetProgressUI?.ResetToPreflop();
            HideExistingCardsInSlots(_playerCardSlots);
            HideExistingCardsInSlots(_opponentCardSlots);

            if (opponentData == null || _opponentCharacterParent == null || string.IsNullOrEmpty(opponentData.modelPath))
            {
                Debug.LogError("[InGamePokerUI] 상대 모델 정보가 없습니다.");
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

            _opponentFace.Bind(opponentData);
        }

        public void BindMatch(PokerMatchManager match)
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
            bool resetCards = false,
            bool showdownReveal = false)
        {
            if (resetCards)
            {
                await ClearCardsAsync();
                if (this == null)
                    return;
            }

            await EnsureHoleCardsAsync(playerHole, _playerCardSlots, _playerCards, true, playDrawSound: true);
            if (this == null)
                return;

            await EnsureHoleCardsAsync(opponentHole, _opponentCardSlots, _opponentCards, revealOpponent);
            if (this == null)
                return;

            // 이미 있는 상대 패는 뒤집기만 (스프라이트 준비까지 await)
            for (var i = 0; i < _opponentCards.Count; i++)
            {
                if (_opponentCards[i] != null)
                {
                    await _opponentCards[i].SetFaceUpAsync(revealOpponent, playRevealSound: revealOpponent, showdownReveal);
                    if (showdownReveal && revealOpponent && i < _opponentCards.Count - 1)
                    {
                        await Task.Delay(120);
                        if (this == null)
                            return;
                    }
                }
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

        public Task PlayStartAnnounceAsync(string text = "게임 시작", float holdSeconds = 2f, bool playPopupSound = false)
        {
            EnsureAnnouncePanels();
            if (_startPanel == null)
            {
                Debug.LogWarning("[InGamePokerUI] StartPanel가 연결되지 않았습니다.");
                return Task.CompletedTask;
            }

            if (playPopupSound)
                PokerSfx.PlayUiShowTurnPopup();

            return _startPanel.PlayAsync(text, holdSeconds);
        }

        public Task PlayHandResultAnnounceAsync(string text, float holdSeconds = 2f)
        {
            EnsureAnnouncePanels();
            if (_startPanel == null)
            {
                Debug.LogWarning("[InGamePokerUI] StartPanel가 연결되지 않았습니다.");
                return Task.CompletedTask;
            }

            PokerSfx.PlayHandWin();
            return _startPanel.PlayAsync(text, holdSeconds);
        }

        public Task PlayTurnAnnounceAsync(string text, float holdSeconds = 1f)
        {
            EnsureAnnouncePanels();
            if (_turnPanel == null)
            {
                Debug.LogWarning("[InGamePokerUI] TurnPanel이 연결되지 않았습니다.");
                return Task.CompletedTask;
            }

            PokerSfx.PlayUiShowTurnPopup();
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
            EnsurePokerStreetProgressUI();
            _streetProgressUI?.SetStreet(street, animate);
        }

        public Task PlayGameResultAsync(bool playerWins, float holdSeconds = 1.35f)
        {
            EnsurePokerGameResultUI();
            if (_gameResultUI == null)
            {
                Debug.LogWarning("[InGamePokerUI] PokerGameResultUI가 연결되지 않았습니다.");
                return Task.CompletedTask;
            }

            return _gameResultUI.PlayAsync(playerWins, holdSeconds);
        }

        public Task PlayShowDownAsync(float holdSeconds = 1.8f)
        {
            EnsurePokerShowDownUI();
            if (_showDownUI == null)
            {
                Debug.LogWarning("[InGamePokerUI] PokerShowDownUI가 연결되지 않았습니다.");
                return Task.CompletedTask;
            }

            return _showDownUI.PlayAsync(holdSeconds);
        }

        /// <summary>
        /// SHOWDOWN 배너 종료 → 1초 → 플레이어 족보 → 1.5초 → 상대 족보 → 1.5초 → 승자 연출.
        /// </summary>
        public async Task PlayShowdownRevealAsync(
            HandEvaluation playerHand,
            HandEvaluation opponentHand,
            int compare)
        {
            EnsurePokerShowDownUI();
            EnsurePokerShowdownHandUI();
            ClearShowdownVisuals();

            await PlayShowDownAsync(ShowdownBannerHold);
            if (this == null)
                return;

            HideDialog();

            await Task.Delay(ShowdownAfterBannerDelayMs);
            if (this == null)
                return;

            if (_showdownHandUI != null)
            {
                _showdownHandUI.transform.SetAsLastSibling();

                var isTie = compare == 0;
                var playerLabel = playerHand.BuildShowdownLabel(opponentHand, compare > 0, isTie);
                var opponentLabel = opponentHand.BuildShowdownLabel(playerHand, compare < 0, isTie);

                // 플레이어 공개 직후 카드 하이라이트도 같이
                ApplyShowdownHighlights(playerHand, opponentHand, compare);

                await _showdownHandUI.ShowSequentialAsync(
                    playerLabel,
                    opponentLabel,
                    compare,
                    ShowdownOpponentRevealDelayMs,
                    ShowdownWinnerRevealDelayMs);
            }
            else
            {
                ApplyShowdownHighlights(playerHand, opponentHand, compare);
            }

            if (this == null)
                return;

            if (compare != 0)
                PulseShowdownWinner(compare > 0, playerHand, opponentHand);

            await Task.Delay(ShowdownHandHoldMs);
            if (this == null)
                return;

            _showdownHandUI?.HideImmediate();
            ClearShowdownVisuals();
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
                Debug.LogWarning("[InGamePokerUI] SoundManager가 없습니다.");
                return;
            }

            soundManager.PlaySe(address);
        }

        public void ShowDialog(string message, string voiceAddress = null)
        {
            EnsurePokerDialogUI();
            if (_dialogUI == null)
            {
                Debug.LogWarning("[InGamePokerUI] PokerDialogUI가 연결되지 않았습니다.");
                return;
            }

            _dialogUI.Show(message, voiceAddress);
        }

        public void HideDialog()
        {
            EnsurePokerDialogUI();
            _dialogUI?.Hide();
        }

        private void EnsureAnnouncePanels()
        {
            if (_startPanel == null)
            {
                var start = transform.Find("PokerStartPanel") ?? transform.Find("InGameStartPanel");
                if (start != null)
                    _startPanel = start.GetComponent<PokerAnnouncePanel>();
            }

            if (_turnPanel == null)
            {
                var turn = transform.Find("PokerTurnPanel") ?? transform.Find("TurnPanel");
                if (turn != null)
                    _turnPanel = turn.GetComponent<PokerAnnouncePanel>();
            }
        }

        private void EnsurePokerDialogUI()
        {
            if (_dialogUI != null)
                return;

            var found = GetComponentInChildren<PokerDialogUI>(true);
            if (found != null)
            {
                _dialogUI = found;
                return;
            }

            var t = transform.Find("PokerDialogUI");
            if (t != null)
                _dialogUI = t.GetComponent<PokerDialogUI>() ?? t.gameObject.AddComponent<PokerDialogUI>();
        }

        private void EnsurePokerGameResultUI()
        {
            if (_gameResultUI != null)
                return;

            var found = GetComponentInChildren<PokerGameResultUI>(true);
            if (found != null)
            {
                _gameResultUI = found;
                return;
            }

            var t = transform.Find("PokerGameResultUI");
            if (t != null)
                _gameResultUI = t.GetComponent<PokerGameResultUI>() ?? t.gameObject.AddComponent<PokerGameResultUI>();
        }

        private void EnsurePokerShowDownUI()
        {
            if (_showDownUI != null)
                return;

            var found = GetComponentInChildren<PokerShowDownUI>(true);
            if (found != null)
            {
                _showDownUI = found;
                return;
            }

            var t = transform.Find("PokerShowDownUI");
            if (t != null)
                _showDownUI = t.GetComponent<PokerShowDownUI>() ?? t.gameObject.AddComponent<PokerShowDownUI>();
        }

        private void EnsurePokerShowdownHandUI()
        {
            if (_showdownHandUI != null)
                return;

            _showdownHandUI = GetComponentInChildren<PokerShowdownHandUI>(true);
            if (_showdownHandUI != null)
                return;

            var t = transform.Find("PokerShowdownHandUI");
            if (t != null)
                _showdownHandUI = t.GetComponent<PokerShowdownHandUI>();

            if (_showdownHandUI == null)
                Debug.LogWarning("[InGamePokerUI] PokerShowdownHandUI가 프리팹에 연결되어 있지 않습니다.");
        }

        private void ClearShowdownVisuals()
        {
            ResetShowdownCards(_playerCards);
            ResetShowdownCards(_opponentCards);
            ResetShowdownCards(_communityCards);
        }

        private static void ResetShowdownCards(List<PokerCardObject> cards)
        {
            for (var i = 0; i < cards.Count; i++)
                cards[i]?.ResetShowdownVisual();
        }

        private void ApplyShowdownHighlights(
            HandEvaluation playerHand,
            HandEvaluation opponentHand,
            int compare)
        {
            var playerKeys = ToCardKeySet(playerHand.BestFive);
            var opponentKeys = ToCardKeySet(opponentHand.BestFive);

            var playerWins = compare > 0;
            var opponentWins = compare < 0;
            var isTie = compare == 0;

            HighlightCardList(_playerCards, playerKeys, isTie || playerWins);
            HighlightCardList(_opponentCards, opponentKeys, isTie || opponentWins);
            HighlightCommunityCards(playerKeys, opponentKeys, compare);
        }

        private void HighlightCommunityCards(
            HashSet<(CardRank rank, CardSuit suit)> playerKeys,
            HashSet<(CardRank rank, CardSuit suit)> opponentKeys,
            int compare)
        {
            var isTie = compare == 0;
            var playerWins = compare > 0;
            var winnerKeys = isTie
                ? UnionCardKeys(playerKeys, opponentKeys)
                : playerWins
                    ? playerKeys
                    : opponentKeys;

            for (var i = 0; i < _communityCards.Count; i++)
            {
                var card = _communityCards[i];
                if (card == null || !card.HasBoundCard)
                    continue;

                var key = (card.BoundCard.Rank, card.BoundCard.Suit);
                var contributing = playerKeys.Contains(key) || opponentKeys.Contains(key);
                var winnerSide = contributing && winnerKeys.Contains(key);
                card.SetShowdownVisual(contributing, winnerSide);
            }
        }

        private void PulseShowdownWinner(
            bool playerWins,
            HandEvaluation playerHand,
            HandEvaluation opponentHand)
        {
            var winnerKeys = playerWins
                ? ToCardKeySet(playerHand.BestFive)
                : ToCardKeySet(opponentHand.BestFive);

            PulseCardList(_playerCards, winnerKeys, playerWins);
            PulseCardList(_opponentCards, winnerKeys, !playerWins);
            PulseCardList(_communityCards, winnerKeys, true);
        }

        private static HashSet<(CardRank rank, CardSuit suit)> ToCardKeySet(PokerCard[] cards)
        {
            var set = new HashSet<(CardRank, CardSuit)>();
            if (cards == null)
                return set;

            for (var i = 0; i < cards.Length; i++)
                set.Add((cards[i].Rank, cards[i].Suit));

            return set;
        }

        private static HashSet<(CardRank rank, CardSuit suit)> UnionCardKeys(
            HashSet<(CardRank rank, CardSuit suit)> a,
            HashSet<(CardRank rank, CardSuit suit)> b)
        {
            var set = new HashSet<(CardRank, CardSuit)>(a);
            set.UnionWith(b);
            return set;
        }

        private static void HighlightCardList(
            List<PokerCardObject> cards,
            HashSet<(CardRank rank, CardSuit suit)> contributingKeys,
            bool winnerSide)
        {
            for (var i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card == null || !card.HasBoundCard)
                    continue;

                var key = (card.BoundCard.Rank, card.BoundCard.Suit);
                var contributing = contributingKeys.Contains(key);
                card.SetShowdownVisual(contributing, winnerSide && contributing);
            }
        }

        private static void PulseCardList(
            List<PokerCardObject> cards,
            HashSet<(CardRank rank, CardSuit suit)> winnerKeys,
            bool sideIsWinner)
        {
            if (!sideIsWinner)
                return;

            for (var i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card == null || !card.HasBoundCard)
                    continue;

                var key = (card.BoundCard.Rank, card.BoundCard.Suit);
                if (winnerKeys.Contains(key))
                    card.PlayShowdownWinnerPulse();
            }
        }

        private void EnsurePokerStreetProgressUI()
        {
            if (_streetProgressUI != null)
                return;

            _streetProgressUI = GetComponentInChildren<PokerStreetProgressUI>(true);
            if (_streetProgressUI != null)
                return;

            Debug.LogWarning("[InGamePokerUI] PokerStreetProgressUI가 프리팹에 연결되어 있지 않습니다.");
        }

        private void EnsureBetFx()
        {
            if (_betFx != null)
                return;

            _betFx = GetComponentInChildren<PokerBetFx>(true);
            if (_betFx != null)
                return;

            _betFx = gameObject.AddComponent<PokerBetFx>();
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
            List<PokerCardObject> bucket,
            bool faceUp,
            bool playDrawSound = false)
        {
            // 파괴된 참조가 남아 스폰을 건너뛰지 않도록 정리
            for (var i = bucket.Count - 1; i >= 0; i--)
            {
                if (bucket[i] == null)
                    bucket.RemoveAt(i);
            }

            if (bucket.Count > 0)
                return;

            await SpawnCardsAsync(cards, slots, bucket, faceUp, playDrawSound);
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
                    Debug.LogError($"[InGamePokerUI] 커뮤니티 카드 슬롯 {i}이 비어 있습니다.");
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

                var cardObject = instance.GetComponent<PokerCardObject>();
                if (cardObject == null)
                    cardObject = instance.AddComponent<PokerCardObject>();

                await cardObject.BindAsync(board[i], true);
                if (this == null)
                    return;

                PokerSfx.PlayCardFlip();
                if (i < targetCount - 1)
                {
                    await Task.Delay(100);
                    if (this == null)
                        return;
                }

                _communityCards.Add(cardObject);
            }
        }

        private async Task SpawnCardsAsync(
            IReadOnlyList<PokerCard> cards,
            Transform[] slots,
            List<PokerCardObject> bucket,
            bool faceUp,
            bool playDrawSound = false)
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
                    Debug.LogError($"[InGamePokerUI] 카드 슬롯 {i}이 비어 있습니다.");
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

                var cardObject = instance.GetComponent<PokerCardObject>();
                if (cardObject == null)
                    cardObject = instance.AddComponent<PokerCardObject>();

                await cardObject.BindAsync(cards[i], faceUp);
                if (this == null)
                    return;

                // 앞면 카드가 아틀라스 실패로 숨겨지지 않도록 한 번 더 보장
                if (faceUp)
                    await cardObject.SetFaceUpAsync(true);
                if (this == null)
                    return;

                bucket.Add(cardObject);

                if (playDrawSound)
                {
                    PokerSfx.PlayCardDraw();
                    if (i < count - 1)
                    {
                        await Task.Delay(120);
                        if (this == null)
                            return;
                    }
                }
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
                    if (child.GetComponent<PokerCardObject>() != null)
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

            if (pot > _displayedPot)
                PokerSfx.PlayPotUp();

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
