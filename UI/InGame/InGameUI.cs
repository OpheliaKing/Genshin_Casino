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

        private GameObject _opponentModel;
        private readonly List<GameObject> _spawnedCards = new();
        private readonly List<CardObject> _playerCards = new();
        private readonly List<CardObject> _opponentCards = new();
        private readonly List<CardObject> _communityCards = new();
        private InGameManager _match;
        private int _displayedPot;
        private bool _hasDisplayedPot;
        private Tween _potTween;

        private const float PotTweenDuration = 0.45f;

        public async Task SetupAsync(OpponentData opponentData)
        {
            ClearOpponentModel();
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

            if (_playerStackUI != null)
                await _playerStackUI.SetIconAsync(PublicVariable.Address.PlayerIcon, PublicVariable.Address.CharacterAtlas);

            if (_opponentStackUI != null && !string.IsNullOrEmpty(opponentData.iconPath))
                await _opponentStackUI.SetIconAsync(opponentData.iconPath, opponentData.atlasAddress);
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
                    _match.OnPlayerAction(_match.PlayerToCall > 0 ? PokerAction.Raise : PokerAction.Bet);
                });
            }
        }

        public async Task RefreshCardsAsync(
            IReadOnlyList<PokerCard> playerHole,
            IReadOnlyList<PokerCard> opponentHole,
            IReadOnlyList<PokerCard> board,
            bool revealOpponent)
        {
            await ClearCardsAsync();
            if (this == null)
                return;

            await SpawnCardsAsync(playerHole, _playerCardSlots, _playerCards, true);
            await SpawnCardsAsync(opponentHole, _opponentCardSlots, _opponentCards, revealOpponent);
            await SpawnCardsAsync(board, _communityCardSlots, _communityCards, true);
        }

        public void RevealOpponentCards()
        {
            for (var i = 0; i < _opponentCards.Count; i++)
                _opponentCards[i]?.SetFaceUp(true);
        }

        public void RefreshHud(string status, int pot, int playerStack, int opponentStack, bool playerTurn, int toCall, bool matchOver)
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
                _raiseButton.SetLabel(toCall > 0 ? "레이즈" : "벳");
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
                _spawnedCards.Add(instance);

                var cardObject = instance.GetComponent<CardObject>();
                if (cardObject == null)
                    cardObject = instance.AddComponent<CardObject>();

                cardObject.Bind(cards[i], faceUp);
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
