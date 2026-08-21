using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SHIN
{
    public class InGameManager : ManagerBase
    {
        private const int SmallBlind = 5;
        private const int BigBlind = 10;

        private const float StartAnnounceHoldMin = 1f;
        private const float StartAnnounceHoldMax = 3f;
        private const float HandResultAnnounceHold = 1.35f;
        private const float TurnAnnounceHold = 1f;
        private const float OpponentThinkDelayMin = 3f;
        private const float OpponentThinkDelayMax = 4.5f;
        private const float OpponentActionReactDelayMin = 1f;
        private const float OpponentActionReactDelayMax = 2f;
        private const float FoldResultDelayMin = 2f;
        private const float FoldResultDelayMax = 3f;
        private const float ShowdownReactDelayMin = 1.5f;
        private const float ShowdownReactDelayMax = 2.5f;

        private OpponentData _opponentData;
        private InGameUI _ui;
        private bool _isStarting;
        private bool _waitingPlayer;
        private bool _matchOver;
        private bool _handBusy;
        private bool _dealerIsPlayer;

        private int _playerStack;
        private int _opponentStack;
        private int _playerStreetBet;
        private int _opponentStreetBet;
        private int _pot;
        private int _currentBet;
        private bool _playerFolded;
        private bool _opponentFolded;
        private bool _playerActed;
        private bool _opponentActed;

        private PokerDeck _deck;
        private PokerCard _playerHole0;
        private PokerCard _playerHole1;
        private PokerCard _opponentHole0;
        private PokerCard _opponentHole1;
        private readonly List<PokerCard> _board = new();
        private PokerStreet _street;
        private PokerStreet _displayedStreet = (PokerStreet)(-1);
        private bool _hasPlayerHoleCards;

        public OpponentData OpponentData => _opponentData;
        public int PlayerToCall => Mathf.Max(0, _currentBet - _playerStreetBet);
        public int CurrentBet => _currentBet;
        public int PlayerStreetBet => _playerStreetBet;

        public void StartMatch(OpponentData opponentData)
        {
            _ = StartMatchAsync(opponentData);
        }

        /// <summary>
        /// 인게임 UI 표시 + 셋업까지. 페이드 아웃 중(검은 화면)에 호출한다.
        /// 시작/턴 연출은 <see cref="BeginGameplayAsync"/>에서 페이드 인 이후 재생한다.
        /// </summary>
        public async Task EnterMatchAsync(OpponentData opponentData)
        {
            if (opponentData == null || _isStarting)
                return;

            var uiManager = GameManager.Instance?.UIManager;
            if (uiManager == null)
                return;

            _opponentData = opponentData;
            _isStarting = true;

            var shown = new TaskCompletionSource<InGameUI>();
            uiManager.Show(PublicVariable.Address.InGameUI, ui =>
            {
                _isStarting = false;
                if (ui is InGameUI inGameUI)
                    shown.TrySetResult(inGameUI);
                else
                    shown.TrySetResult(null);
            });

            var inGameUI = await shown.Task;
            if (inGameUI == null)
                return;

            _ui = inGameUI;
            await inGameUI.SetupAsync(_opponentData);
            if (this == null || inGameUI == null)
                return;

            inGameUI.BindMatch(this);

            var playerData = GameManager.Instance != null
                ? await GameManager.Instance.EnsurePlayerDataAsync()
                : null;
            if (this == null || inGameUI == null)
                return;

            var playerGold = playerData != null ? playerData.haveGold : 100;
            _playerStack = Mathf.Max(BigBlind, playerGold);
            _opponentStack = Mathf.Max(BigBlind, _opponentData.haveGold);
            _dealerIsPlayer = false;
            _matchOver = false;
            _hasPlayerHoleCards = false;
            _board.Clear();
            RefreshHud();
        }

        /// <summary>
        /// 페이드 인 이후 호출. 게임 시작 연출 → 첫 핸드 → 턴 연출.
        /// </summary>
        public async Task BeginGameplayAsync()
        {
            if (_ui == null || _matchOver)
                return;

            GameManager.Instance?.SoundManager?.PlayBgm(PublicVariable.Address.InGameBgm);

            // 표정(GAME_START)과 시작 대사를 같은 타이밍에
            _ui.ShowOpponentReaction(CharacterExpressionType.GAME_START);

            var startHold = Random.Range(StartAnnounceHoldMin, StartAnnounceHoldMax);
            await _ui.PlayStartAnnounceAsync("게임 시작", startHold);
            if (this == null || _matchOver)
                return;

            await StartHandAsync();
        }

        /// <summary>Enter + Begin을 한 번에 (페이드 없이 바로 테스트할 때).</summary>
        public async Task StartMatchAsync(OpponentData opponentData)
        {
            await EnterMatchAsync(opponentData);
            if (this == null || _ui == null)
                return;
            await BeginGameplayAsync();
        }

        public void OnPlayerAction(PokerAction action)
        {
            if (!_waitingPlayer || _matchOver || _handBusy)
                return;

            ApplyAction(true, action);
        }

        private async Task StartHandAsync()
        {
            if (_playerStack <= 0 || _opponentStack <= 0)
            {
                await EndMatchAsync();
                return;
            }

            _handBusy = true;
            _waitingPlayer = false;
            _playerFolded = false;
            _opponentFolded = false;
            _playerActed = false;
            _opponentActed = false;
            _playerStreetBet = 0;
            _opponentStreetBet = 0;
            _pot = 0;
            _currentBet = 0;
            _board.Clear();
            _street = PokerStreet.Preflop;
            _hasPlayerHoleCards = false;
            _deck = new PokerDeck();

            // 새 핸드 시작 직후엔 패 표기를 비워 둔다
            RefreshHud();

            PostBlinds();
            _playerHole0 = _deck.Draw();
            _playerHole1 = _deck.Draw();
            _opponentHole0 = _deck.Draw();
            _opponentHole1 = _deck.Draw();
            _hasPlayerHoleCards = true;

            await RefreshTableAsync(false, resetCards: true);
            _handBusy = false;

            await ContinueBettingAsync(_dealerIsPlayer);
        }

        private void PostBlinds()
        {
            if (_dealerIsPlayer)
            {
                PutChips(true, SmallBlind);
                PutChips(false, BigBlind);
            }
            else
            {
                PutChips(false, SmallBlind);
                PutChips(true, BigBlind);
            }

            _currentBet = BigBlind;
        }

        private void PutChips(bool isPlayer, int amount)
        {
            if (amount <= 0)
                return;

            if (isPlayer)
            {
                var pay = Mathf.Min(amount, _playerStack);
                _playerStack -= pay;
                _playerStreetBet += pay;
                _pot += pay;
            }
            else
            {
                var pay = Mathf.Min(amount, _opponentStack);
                _opponentStack -= pay;
                _opponentStreetBet += pay;
                _pot += pay;
            }
        }

        private void ApplyAction(bool isPlayer, PokerAction action)
        {
            _ = ApplyActionAsync(isPlayer, action);
        }

        private async Task ApplyActionAsync(bool isPlayer, PokerAction action)
        {
            if (_matchOver || (isPlayer && _playerFolded) || (!isPlayer && _opponentFolded))
                return;

            if (isPlayer)
                _waitingPlayer = false;

            var toCall = isPlayer
                ? Mathf.Max(0, _currentBet - _playerStreetBet)
                : Mathf.Max(0, _currentBet - _opponentStreetBet);
            var stack = isPlayer ? _playerStack : _opponentStack;

            if (action == PokerAction.Fold && toCall <= 0)
                action = PokerAction.Check;
            if (action == PokerAction.Check && toCall > 0)
                action = PokerAction.Call;
            if (action == PokerAction.Bet && _currentBet > 0)
                action = PokerAction.Raise;
            if (action == PokerAction.Raise && _currentBet <= 0)
                action = PokerAction.Bet;
            if ((action == PokerAction.Bet || action == PokerAction.Raise || action == PokerAction.Call) && stack <= toCall)
                action = toCall > 0 ? PokerAction.Call : PokerAction.AllIn;

            var potBefore = _pot;
            var resolved = action;

            switch (action)
            {
                case PokerAction.Fold:
                    if (isPlayer)
                        _playerFolded = true;
                    else
                        _opponentFolded = true;
                    _ui?.PlayBetFx(isPlayer, PokerAction.Fold, 0);
                    if (isPlayer)
                        _ui?.PlayPlayerVoice(CharacterExpressionType.ACTION_FOLD);
                    else
                        await PlayOpponentActionReactionAsync(PokerAction.Fold);
                    if (this == null || _matchOver)
                        return;
                    await EndHandByFoldAsync(!isPlayer);
                    return;
                case PokerAction.Check:
                    _ui?.PlayBetFx(isPlayer, PokerAction.Check, 0);
                    break;
                case PokerAction.Call:
                    PutChips(isPlayer, toCall);
                    break;
                case PokerAction.Bet:
                    PutChips(isPlayer, Mathf.Min(BigBlind, stack));
                    _currentBet = isPlayer ? _playerStreetBet : _opponentStreetBet;
                    ResetActor(!isPlayer);
                    break;
                case PokerAction.Raise:
                    var raiseTo = Mathf.Max(_currentBet + BigBlind, _currentBet * 2);
                    var need = raiseTo - (isPlayer ? _playerStreetBet : _opponentStreetBet);
                    PutChips(isPlayer, need);
                    _currentBet = isPlayer ? _playerStreetBet : _opponentStreetBet;
                    ResetActor(!isPlayer);
                    break;
                case PokerAction.AllIn:
                    PutChips(isPlayer, stack);
                    var newBet = isPlayer ? _playerStreetBet : _opponentStreetBet;
                    if (newBet > _currentBet)
                    {
                        _currentBet = newBet;
                        ResetActor(!isPlayer);
                    }
                    break;
            }

            if (isPlayer)
                _playerActed = true;
            else
                _opponentActed = true;

            var paid = _pot - potBefore;
            if (resolved != PokerAction.Check)
                _ui?.PlayBetFx(isPlayer, resolved, paid);

            if (isPlayer)
                _ui?.PlayPlayerVoice(ExpressionForAction(resolved));

            RefreshHud();

            if (!isPlayer)
                await PlayOpponentActionReactionAsync(resolved);
            if (this == null || _matchOver)
                return;

            await AfterActionAsync();
        }

        private async Task PlayOpponentActionReactionAsync(PokerAction action)
        {
            var expression = ExpressionForAction(action);
            _ui?.ShowOpponentReaction(expression);

            var delayMs = Mathf.RoundToInt(
                Random.Range(OpponentActionReactDelayMin, OpponentActionReactDelayMax) * 1000f);
            await Task.Delay(delayMs);
        }

        private static CharacterExpressionType ExpressionForAction(PokerAction action)
        {
            return action switch
            {
                PokerAction.Check => CharacterExpressionType.ACTION_CHECK,
                PokerAction.Call => CharacterExpressionType.ACTION_CALL,
                PokerAction.Bet => CharacterExpressionType.ACTION_BET,
                PokerAction.Raise => CharacterExpressionType.ACTION_RAISE,
                PokerAction.Fold => CharacterExpressionType.ACTION_FOLD,
                PokerAction.AllIn => CharacterExpressionType.ACTION_ALL_IN,
                _ => CharacterExpressionType.NORMAL
            };
        }

        private void ResetActor(bool isPlayer)
        {
            if (isPlayer)
                _playerActed = false;
            else
                _opponentActed = false;
        }

        private async Task AfterActionAsync()
        {
            if (IsBettingRoundComplete())
            {
                if (_street == PokerStreet.River)
                {
                    await ShowdownAsync();
                    return;
                }

                if (_playerStack <= 0 || _opponentStack <= 0)
                {
                    await RunoutAsync();
                    return;
                }

                await AdvanceStreetAsync();
                return;
            }

            await ContinueBettingAsync(WhoActsNext());
        }

        private bool IsBettingRoundComplete()
        {
            if (_playerFolded || _opponentFolded)
                return true;

            if (_playerStack <= 0)
                return _opponentStreetBet >= _playerStreetBet && _opponentActed;
            if (_opponentStack <= 0)
                return _playerStreetBet >= _opponentStreetBet && _playerActed;

            return _playerStreetBet == _opponentStreetBet && _playerActed && _opponentActed;
        }

        private bool WhoActsNext()
        {
            var playerToCall = PlayerToCall;
            var opponentToCall = Mathf.Max(0, _currentBet - _opponentStreetBet);

            if (playerToCall > 0 && _playerStack > 0 && !_playerFolded)
                return true;
            if (opponentToCall > 0 && _opponentStack > 0 && !_opponentFolded)
                return false;
            if (!_playerActed && _playerStack > 0 && !_playerFolded)
                return true;
            return false;
        }

        private async Task ContinueBettingAsync(bool playerTurn)
        {
            if (_matchOver || IsBettingRoundComplete())
                return;

            if (playerTurn)
            {
                if (_playerStack > 0 && !_playerFolded)
                {
                    if (_ui != null)
                        await _ui.PlayTurnAnnounceAsync("당신 차례", TurnAnnounceHold);
                    if (this == null || _matchOver)
                        return;

                    _ui?.SetOpponentExpression(CharacterExpressionType.NORMAL);

                    _waitingPlayer = true;
                    RefreshHud();
                    return;
                }

                await ContinueBettingAsync(false);
                return;
            }

            if (_opponentStack > 0 && !_opponentFolded)
            {
                if (_ui != null)
                    await _ui.PlayTurnAnnounceAsync("상대 차례", TurnAnnounceHold);
                if (this == null || _matchOver)
                    return;

                _ui?.ShowOpponentReaction(CharacterExpressionType.TURN_START);

                _waitingPlayer = false;
                RefreshHud();

                // 턴 UI 직후 바로 행동하면 템포가 기계적이라, 사람처럼 랜덤 사고 시간
                var thinkMs = Mathf.RoundToInt(Random.Range(OpponentThinkDelayMin, OpponentThinkDelayMax) * 1000f);
                await Task.Delay(thinkMs);
                if (this == null || _matchOver)
                    return;
                ChooseOpponentAction();
                return;
            }

            _playerActed = true;
            _opponentActed = true;
            await AfterActionAsync();
        }

        private void ChooseOpponentAction()
        {
            var toCall = Mathf.Max(0, _currentBet - _opponentStreetBet);
            var roll = Random.value;

            if (toCall <= 0)
            {
                ApplyAction(false, roll < 0.7f ? PokerAction.Check : PokerAction.Bet);
                return;
            }

            if (roll < 0.2f)
                ApplyAction(false, PokerAction.Fold);
            else if (roll < 0.85f)
                ApplyAction(false, PokerAction.Call);
            else
                ApplyAction(false, PokerAction.Raise);
        }

        private async Task AdvanceStreetAsync()
        {
            _handBusy = true;
            _waitingPlayer = false;
            _playerStreetBet = 0;
            _opponentStreetBet = 0;
            _currentBet = 0;
            _playerActed = false;
            _opponentActed = false;

            switch (_street)
            {
                case PokerStreet.Preflop:
                    _street = PokerStreet.Flop;
                    _board.Add(_deck.Draw());
                    _board.Add(_deck.Draw());
                    _board.Add(_deck.Draw());
                    break;
                case PokerStreet.Flop:
                    _street = PokerStreet.Turn;
                    _board.Add(_deck.Draw());
                    break;
                case PokerStreet.Turn:
                    _street = PokerStreet.River;
                    _board.Add(_deck.Draw());
                    break;
                case PokerStreet.River:
                    await ShowdownAsync();
                    return;
            }

            await RefreshTableAsync(false);
            _handBusy = false;

            if (_playerStack <= 0 || _opponentStack <= 0)
            {
                await RunoutAsync();
                return;
            }

            await ContinueBettingAsync(!_dealerIsPlayer);
        }

        private async Task RunoutAsync()
        {
            _handBusy = true;
            _waitingPlayer = false;
            while (_board.Count < 5)
            {
                _board.Add(_deck.Draw());
                _street = _board.Count == 3 ? PokerStreet.Flop
                    : _board.Count == 4 ? PokerStreet.Turn
                    : PokerStreet.River;
                await RefreshTableAsync(false);
                await Task.Delay(350);
                if (this == null)
                    return;
            }

            await ShowdownAsync();
        }

        private async Task ShowdownAsync()
        {
            _street = PokerStreet.Showdown;
            await RefreshTableAsync(true);
            _ui?.RevealOpponentCards();

            // 쇼다운 시작: 카드 공개 직후 전용 대사·표정 + ShowDownUI 연출
            _ui?.ShowOpponentReaction(CharacterExpressionType.SHOWDOWN);
            if (_ui != null)
            {
                var hold = Random.Range(ShowdownReactDelayMin, ShowdownReactDelayMax);
                await _ui.PlayShowDownAsync(hold);
            }
            else
            {
                var showdownDelayMs = Mathf.RoundToInt(
                    Random.Range(ShowdownReactDelayMin, ShowdownReactDelayMax) * 1000f);
                await Task.Delay(showdownDelayMs);
            }

            if (this == null || _matchOver)
                return;

            var playerScore = PokerHandEvaluator.Evaluate(_playerHole0, _playerHole1, _board.ToArray());
            var opponentScore = PokerHandEvaluator.Evaluate(_opponentHole0, _opponentHole1, _board.ToArray());
            var compare = playerScore.CompareTo(opponentScore);

            if (compare > 0)
            {
                _playerStack += _pot;
                RefreshHud();
                _ui?.ShowOpponentReaction(CharacterExpressionType.HAND_LOSE);
                _pot = 0;
                if (_ui != null)
                    await _ui.PlayStartAnnounceAsync("승리", HandResultAnnounceHold);
            }
            else if (compare < 0)
            {
                _opponentStack += _pot;
                RefreshHud();
                _ui?.ShowOpponentReaction(CharacterExpressionType.HAND_WIN);
                _pot = 0;
                if (_ui != null)
                    await _ui.PlayStartAnnounceAsync("패배", HandResultAnnounceHold);
            }
            else
            {
                var half = _pot / 2;
                _playerStack += half;
                _opponentStack += _pot - half;
                RefreshHud();
                _pot = 0;
                if (_ui != null)
                    await _ui.PlayStartAnnounceAsync("무승부", HandResultAnnounceHold);
            }

            if (this == null || _matchOver)
                return;

            await FinishHandAsync();
        }

        private async Task EndHandByFoldAsync(bool playerWins)
        {
            _waitingPlayer = false;
            if (playerWins)
            {
                _playerStack += _pot;
                RefreshHud();
                _ui?.ShowOpponentReaction(CharacterExpressionType.HAND_LOSE);
            }
            else
            {
                _opponentStack += _pot;
                RefreshHud();
                _ui?.ShowOpponentReaction(CharacterExpressionType.HAND_WIN);
            }

            _pot = 0;

            // 폴드 직후 승리/패배 안내가 뜨지 않도록 상대 반응을 잠깐 보여 준다.
            var delayMs = Mathf.RoundToInt(Random.Range(FoldResultDelayMin, FoldResultDelayMax) * 1000f);
            await Task.Delay(delayMs);
            if (this == null || _matchOver)
                return;

            if (_ui != null)
                await _ui.PlayStartAnnounceAsync(playerWins ? "승리" : "패배", HandResultAnnounceHold);
            if (this == null || _matchOver)
                return;

            await FinishHandAsync();
        }

        private async Task FinishHandAsync()
        {
            _handBusy = true;
            await Task.Delay(400);
            if (this == null)
                return;

            _dealerIsPlayer = !_dealerIsPlayer;
            _handBusy = false;

            if (_playerStack <= 0 || _opponentStack <= 0)
            {
                await EndMatchAsync();
                return;
            }

            await StartHandAsync();
        }

        private async Task EndMatchAsync()
        {
            _matchOver = true;
            _waitingPlayer = false;
            var win = _playerStack > 0;
            RefreshHud(win ? "매치 승리" : "매치 패배");

            GameManager.Instance?.SoundManager?.StopBgm();

            // 캐릭터 기준: 플레이어 승리 → LOSE, 플레이어 패배 → WIN
            if (_ui != null)
            {
                _ui.ShowOpponentReaction(win ? CharacterExpressionType.LOSE : CharacterExpressionType.WIN);
                await _ui.PlayGameResultAsync(win);
            }
        }

        private async Task RefreshTableAsync(bool revealOpponent, bool resetCards = false)
        {
            if (_ui == null)
                return;

            await _ui.RefreshCardsAsync(
                new[] { _playerHole0, _playerHole1 },
                new[] { _opponentHole0, _opponentHole1 },
                _board,
                revealOpponent,
                resetCards);
            RefreshHud();
        }

        private void RefreshHud(string statusOverride = null)
        {
            var status = statusOverride ?? PlayerHandStatus();
            _ui?.RefreshHud(
                status,
                _pot,
                _playerStack,
                _opponentStack,
                _waitingPlayer,
                PlayerToCall,
                _matchOver,
                _currentBet);

            var animate = _displayedStreet != _street;
            _displayedStreet = _street;
            _ui?.SetStreetProgress(_street, animate);
        }

        private string PlayerHandStatus()
        {
            if (!_hasPlayerHoleCards)
                return "내 패 · -";

            var score = PokerHandEvaluator.Evaluate(_playerHole0, _playerHole1, _board.ToArray());
            return $"내 패 · {score.DisplayName}";
        }
    }
}
