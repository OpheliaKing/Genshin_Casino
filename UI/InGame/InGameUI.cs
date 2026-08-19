using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace SHIN
{
    public class InGameUI : UIBase
    {
        [SerializeField] private Transform _opponentCharacterParent;
        [SerializeField] private Transform _playerCardParent;
        [SerializeField] private Transform _opponentCardParent;
        [SerializeField] private Transform _communityCardParent;
        [SerializeField] private Transform _playerInputRoot;

        private GameObject _opponentModel;
        private readonly List<GameObject> _spawnedCards = new();
        private readonly List<CardObject> _playerCards = new();
        private readonly List<CardObject> _opponentCards = new();
        private readonly List<CardObject> _communityCards = new();

        private Text _statusText;
        private Text _potText;
        private Button _foldButton;
        private Button _callButton;
        private Button _raiseButton;
        private InGameManager _match;

        public async Task SetupAsync(OpponentData opponentData)
        {
            ClearOpponentModel();
            EnsureHud();
            EnsureCardLayout(_playerCardParent);
            EnsureCardLayout(_opponentCardParent);
            EnsureCardLayout(_communityCardParent);
            HideExistingCards(_playerCardParent);
            HideExistingCards(_opponentCardParent);

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
        }

        public void BindMatch(InGameManager match)
        {
            _match = match;
            if (_foldButton != null)
            {
                _foldButton.onClick.RemoveAllListeners();
                _foldButton.onClick.AddListener(() => _match?.OnPlayerAction(PokerAction.Fold));
            }

            if (_callButton != null)
            {
                _callButton.onClick.RemoveAllListeners();
                _callButton.onClick.AddListener(() =>
                {
                    if (_match == null)
                        return;
                    _match.OnPlayerAction(_match.PlayerToCall > 0 ? PokerAction.Call : PokerAction.Check);
                });
            }

            if (_raiseButton != null)
            {
                _raiseButton.onClick.RemoveAllListeners();
                _raiseButton.onClick.AddListener(() =>
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

            await SpawnCardsAsync(playerHole, _playerCardParent, _playerCards, true);
            await SpawnCardsAsync(opponentHole, _opponentCardParent, _opponentCards, revealOpponent);
            await SpawnCardsAsync(board, _communityCardParent, _communityCards, true);
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

            if (_potText != null)
                _potText.text = $"팟 {pot}\n나 {playerStack}  /  상대 {opponentStack}";

            var interactable = playerTurn && !matchOver;
            if (_foldButton != null)
            {
                _foldButton.interactable = interactable && toCall > 0;
                SetButtonLabel(_foldButton, "폴드");
            }

            if (_callButton != null)
            {
                _callButton.interactable = interactable;
                SetButtonLabel(_callButton, toCall > 0 ? $"콜 {toCall}" : "체크");
            }

            if (_raiseButton != null)
            {
                _raiseButton.interactable = interactable;
                SetButtonLabel(_raiseButton, toCall > 0 ? "레이즈" : "벳");
            }
        }

        private async Task SpawnCardsAsync(
            IReadOnlyList<PokerCard> cards,
            Transform parent,
            List<CardObject> bucket,
            bool faceUp)
        {
            bucket.Clear();
            if (cards == null || parent == null)
                return;

            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager == null)
                return;

            for (var i = 0; i < cards.Count; i++)
            {
                var instance = await resourceManager.InstantiateAsync(
                    PublicVariable.Address.CardItem,
                    parent,
                    startInactive: false);

                if (this == null)
                {
                    if (instance != null)
                        resourceManager.ReleaseInstance(instance);
                    return;
                }

                if (instance == null)
                    continue;

                ResetLocalTransform(instance.transform);
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

        private void EnsureHud()
        {
            var root = _playerInputRoot != null ? _playerInputRoot : transform;
            if (_statusText != null)
                return;

            var hud = CreateUiObject("PokerHud", root);
            var hudRect = hud.GetComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(0.05f, 0.08f);
            hudRect.anchorMax = new Vector2(0.95f, 0.92f);
            hudRect.offsetMin = Vector2.zero;
            hudRect.offsetMax = Vector2.zero;

            var layout = hud.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(24, 24, 24, 24);

            _statusText = CreateText(hud.transform, "Status", 28);
            _potText = CreateText(hud.transform, "Pot", 24);
            _foldButton = CreateButton(hud.transform, "폴드");
            _callButton = CreateButton(hud.transform, "체크");
            _raiseButton = CreateButton(hud.transform, "벳");
        }

        private static void HideExistingCards(Transform parent)
        {
            if (parent == null)
                return;

            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.GetComponent<CardObject>() != null)
                    child.gameObject.SetActive(false);
            }
        }

        private static void EnsureCardLayout(Transform parent)
        {
            if (parent == null)
                return;

            var layout = parent.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
                layout = parent.gameObject.AddComponent<HorizontalLayoutGroup>();

            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 12f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Text CreateText(Transform parent, string name, int fontSize)
        {
            var go = CreateUiObject(name, parent);
            var layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = 80f;
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button CreateButton(Transform parent, string label)
        {
            var go = CreateUiObject(label, parent);
            var layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = 72f;
            var image = go.AddComponent<Image>();
            image.color = new Color(0.18f, 0.12f, 0.08f, 0.92f);
            var button = go.AddComponent<Button>();
            var text = CreateText(go.transform, "Label", 26);
            text.text = label;
            text.raycastTarget = false;
            return button;
        }

        private static void SetButtonLabel(Button button, string label)
        {
            var text = button.GetComponentInChildren<Text>();
            if (text != null)
                text.text = label;
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
            _ = ClearCardsAsync();
            ClearOpponentModel();
        }
    }
}
