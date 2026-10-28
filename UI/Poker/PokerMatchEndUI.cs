using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SHIN
{
    /// <summary>
    /// 매치 승패 연출 이후. 다시 하기 / 로비.
    /// </summary>
    public class PokerMatchEndUI : MonoBehaviour
    {
        private TextMeshProUGUI _title;
        private ButtonBase _rematchButton;
        private ButtonBase _lobbyButton;
        private Action _onRematch;
        private Action _onLobby;
        private bool _built;
        private bool _chosen;

        public void Show(bool playerWins, Action onRematch, Action onLobby)
        {
            EnsureBuilt();
            _chosen = false;
            _onRematch = onRematch;
            _onLobby = onLobby;

            if (_title != null)
                _title.text = playerWins ? "승리" : "패배";

            if (_rematchButton != null)
                _rematchButton.interactable = true;
            if (_lobbyButton != null)
                _lobbyButton.interactable = true;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            _onRematch = null;
            _onLobby = null;
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        private void EnsureBuilt()
        {
            if (_built)
                return;

            _built = true;

            var root = gameObject.GetComponent<RectTransform>();
            if (root == null)
                root = gameObject.AddComponent<RectTransform>();

            Stretch(root);
            var dim = gameObject.GetComponent<Image>();
            if (dim == null)
                dim = gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.62f);
            dim.raycastTarget = true;

            var panel = Child(transform, "Panel", new Vector2(560f, 280f));
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.07f, 0.12f, 0.09f, 0.96f);
            panelImage.raycastTarget = true;

            var titleGo = Child(panel, "Title", new Vector2(440f, 72f));
            titleGo.anchoredPosition = new Vector2(0f, 48f);
            _title = AddText(titleGo.gameObject, "승리", 40f);

            _rematchButton = CreateButton(panel, "Rematch", "다시 하기", new Vector2(-120f, -62f), OnRematch);
            _lobbyButton = CreateButton(panel, "Lobby", "로비", new Vector2(120f, -62f), OnLobby);
        }

        private void OnRematch()
        {
            Choose(_onRematch);
        }

        private void OnLobby()
        {
            Choose(_onLobby);
        }

        private void Choose(Action action)
        {
            if (_chosen)
                return;

            _chosen = true;
            if (_rematchButton != null)
                _rematchButton.interactable = false;
            if (_lobbyButton != null)
                _lobbyButton.interactable = false;

            action?.Invoke();
        }

        private static ButtonBase CreateButton(RectTransform parent, string name, string label, Vector2 position, Action onClick)
        {
            var rect = Child(parent, name, new Vector2(200f, 64f));
            rect.anchoredPosition = position;

            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.2f, 0.34f, 0.24f, 1f);
            image.raycastTarget = true;

            var button = rect.gameObject.AddComponent<ButtonBase>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            if (onClick != null)
                button.onClick.AddListener(() => onClick());

            var labelRect = Child(rect, "Label", new Vector2(180f, 48f));
            AddText(labelRect.gameObject, label, 26f);
            return button;
        }

        private static TextMeshProUGUI AddText(GameObject target, string text, float fontSize)
        {
            var label = target.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            var font = TMP_Settings.defaultFontAsset;
            if (font != null)
            {
                label.font = font;
                if (font.material != null)
                    label.fontSharedMaterial = font.material;
            }

            return label;
        }

        private static RectTransform Child(Transform parent, string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
