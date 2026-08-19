using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SHIN
{
    public class InGameManager : ManagerBase
    {
        private const int DefaultPlayerStack = 100;
        private const int SmallBlind = 5;
        private const int BigBlind = 10;

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

        public OpponentData OpponentData => _opponentData;
        public int PlayerToCall => Mathf.Max(0, _currentBet - _playerStreetBet);
        public int CurrentBet => _currentBet;
        public int PlayerStreetBet => _playerStreetBet;

        public void StartMatch(OpponentData opponentData)
        {
            if (opponentData == null || _isStarting)
                return;

            var uiManager = GameManager.Instance?.UIManager;
            if (uiManager == null)
                return;

            _opponentData = opponentData;
            _isStarting = true;

            uiManager.Show(PublicVariable.Address.InGameUI, ui =>
            {
                _isStarting = false;
                if (ui is not InGameUI inGameUI)
                    return;

                _ = BeginMatchAsync(inGameUI);
            });
        }

        public void OnPlayerAction(PokerAction action)
        {
            if (!_waitingPlayer || _matchOver || _handBusy)
                return;

            ApplyAction(true, action);
        }

        private async Task BeginMatchAsync(InGameUI ui)
        {
            _ui = ui;
            await ui.SetupAsync(_opponentData);
            if (this == null || ui == null)
                return;

            ui.BindMatch(this);
            _playerStack = DefaultPlayerStack;
            _opponentStack = Mathf.Max(BigBlind, _opponentData.haveGold);
            _dealerIsPlayer = false;
            _matchOver = false;
            await StartHandAsync();
        }

        private async Task StartHandAsync()
        {
            if (_playerStack <= 0 || _opponentStack <= 0)
            {
                EndMatch();
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
            _deck = new PokerDeck();

            PostBlinds();
            _playerHole0 = _deck.Draw();
            _playerHole1 = _deck.Draw();
            _opponentHole0 = _deck.Draw();
            _opponentHole1 = _deck.Draw();

            await RefreshTableAsync(false);
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
            if (_matchOver || (isPlayer && _playerFolded) || (!isPlayer && _opponentFolded))
                return;

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

            switch (action)
            {
                case PokerAction.Fold:
                    if (isPlayer)
                        _playerFolded = true;
                    else
                        _opponentFolded = true;
                    _ = EndHandByFoldAsync(!isPlayer);
                    return;
                case PokerAction.Check:
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

            RefreshHud(StreetLabel());
            _ = AfterActionAsync();
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
                    _waitingPlayer = true;
                    RefreshHud("당신 차례");
                    return;
                }

                await ContinueBettingAsync(false);
                return;
            }

            if (_opponentStack > 0 && !_opponentFolded)
            {
                _waitingPlayer = false;
                RefreshHud("상대 생각 중...");
                await Task.Delay(700);
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

            var playerScore = PokerHandEvaluator.Evaluate(_playerHole0, _playerHole1, _board.ToArray());
            var opponentScore = PokerHandEvaluator.Evaluate(_opponentHole0, _opponentHole1, _board.ToArray());
            var compare = playerScore.CompareTo(opponentScore);

            if (compare > 0)
            {
                _playerStack += _pot;
                RefreshHud($"승리! {playerScore.DisplayName}");
            }
            else if (compare < 0)
            {
                _opponentStack += _pot;
                RefreshHud($"패배... 상대 {opponentScore.DisplayName}");
            }
            else
            {
                var half = _pot / 2;
                _playerStack += half;
                _opponentStack += _pot - half;
                RefreshHud($"무승부 {playerScore.DisplayName}");
            }

            _pot = 0;
            await FinishHandAsync();
        }

        private async Task EndHandByFoldAsync(bool playerWins)
        {
            _waitingPlayer = false;
            if (playerWins)
            {
                _playerStack += _pot;
                RefreshHud("상대 폴드. 팟 획득");
            }
            else
            {
                _opponentStack += _pot;
                RefreshHud("폴드. 상대가 팟 획득");
            }

            _pot = 0;
            await FinishHandAsync();
        }

        private async Task FinishHandAsync()
        {
            _handBusy = true;
            await Task.Delay(1600);
            if (this == null)
                return;

            _dealerIsPlayer = !_dealerIsPlayer;
            _handBusy = false;

            if (_playerStack <= 0 || _opponentStack <= 0)
            {
                EndMatch();
                return;
            }

            await StartHandAsync();
        }

        private void EndMatch()
        {
            _matchOver = true;
            _waitingPlayer = false;
            var win = _playerStack > 0;
            RefreshHud(win ? "매치 승리" : "매치 패배");
        }

        private async Task RefreshTableAsync(bool revealOpponent)
        {
            if (_ui == null)
                return;

            await _ui.RefreshCardsAsync(
                new[] { _playerHole0, _playerHole1 },
                new[] { _opponentHole0, _opponentHole1 },
                _board,
                revealOpponent);
            RefreshHud(StreetLabel());
        }

        private void RefreshHud(string status)
        {
            _ui?.RefreshHud(
                status,
                _pot,
                _playerStack,
                _opponentStack,
                _waitingPlayer,
                PlayerToCall,
                _matchOver);
        }

        private string StreetLabel()
        {
            return _street switch
            {
                PokerStreet.Preflop => "프리플랍",
                PokerStreet.Flop => "플랍",
                PokerStreet.Turn => "턴",
                PokerStreet.River => "리버",
                _ => "쇼다운"
            };
        }
    }
}
