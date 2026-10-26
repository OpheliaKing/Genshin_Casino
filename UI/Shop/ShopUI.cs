using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SHIN
{
    /// <summary>
    /// 상점 화면. SO/구매 로직은 이후 연결. 지금은 UX 골격 + 더미 목록.
    /// </summary>
    public class ShopUI : UIBase
    {
        [Header("Refs (프리팹 연결용)")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _goldText;
        [SerializeField] private Transform _slotRoot;
        [SerializeField] private Button _closeButton;
        [SerializeField] private ShopItemSlotUI _slotPrefab;

        [Header("Runtime")]
        [SerializeField] private bool _useDummyEntries = true;

        private readonly List<ShopItemSlotUI> _slots = new();
        private readonly List<ShopEntryView> _entries = new();
        private bool _built;

        public override void OnShow()
        {
            EnsureBuilt();
            RefreshGold();
            RebuildEntries();
            RefreshSlots();
        }

        public override void OnHide()
        {
            // 슬롯은 유지. 다음 OnShow에서 다시 바인딩.
        }

        public void OnClickClose()
        {
            var uiManager = GameManager.Instance?.UIManager;
            if (uiManager == null)
            {
                gameObject.SetActive(false);
                return;
            }

            uiManager.Close(this, restoreVisibleStack: true);
        }

        /// <summary>SO 확정 전: UI에서 구매 요청만 로그. 이후 GameManager/ShopService로 이전.</summary>
        public void RequestBuy(ShopEntryView entry)
        {
            if (entry == null)
                return;

            if (entry.Owned)
            {
                Debug.Log($"[ShopUI] 이미 보유: {entry.Tid}");
                return;
            }

            if (!entry.CanAfford)
            {
                Debug.Log($"[ShopUI] 칩 부족: {entry.Tid} price={entry.Price}");
                return;
            }

            Debug.Log($"[ShopUI] 구매 요청(미구현): tid={entry.Tid}, price={entry.Price}");
            // TODO: SO + 재화 차감 + Owned 반영 후 RefreshSlots()
        }

        /// <summary>외부(또는 이후 SO 로더)에서 목록을 넣을 때 사용.</summary>
        public void SetEntries(IReadOnlyList<ShopEntryView> entries)
        {
            _entries.Clear();
            if (entries != null)
            {
                for (var i = 0; i < entries.Count; i++)
                {
                    if (entries[i] != null)
                        _entries.Add(entries[i]);
                }
            }

            RefreshGold();
            ApplyAffordFlags();
            RefreshSlots();
        }

        public void RefreshGold()
        {
            EnsureBuilt();
            var gold = 0;
            var gm = GameManager.Instance;
            if (gm?.PlayerData != null)
                gold = Mathf.Max(0, gm.PlayerData.haveGold);

            if (_goldText != null)
                _goldText.text = $"칩 {gold}";
        }

        private void RebuildEntries()
        {
            if (!_useDummyEntries && _entries.Count > 0)
            {
                ApplyAffordFlags();
                return;
            }

            _entries.Clear();
            if (!_useDummyEntries)
                return;

            var gold = GameManager.Instance?.PlayerData != null
                ? GameManager.Instance.PlayerData.haveGold
                : 0;

            // UX 확인용 더미. SO 붙이면 SetEntries로 교체.
            _entries.Add(new ShopEntryView
            {
                Tid = "shop_dummy_chip_boost",
                DisplayName = "칩 파우치(더미)",
                Description = "구매 연출·슬롯 UX 확인용. 실제 상품 아님.",
                Price = 50,
                Owned = false,
                CanAfford = gold >= 50,
            });
            _entries.Add(new ShopEntryView
            {
                Tid = "shop_dummy_companion",
                DisplayName = "동료 티켓(더미)",
                Description = "이후 동료 해금 상품이 들어갈 자리.",
                Price = 200,
                Owned = false,
                CanAfford = gold >= 200,
            });
            _entries.Add(new ShopEntryView
            {
                Tid = "shop_dummy_owned",
                DisplayName = "보유 예시(더미)",
                Description = "이미 산 상품 상태 표시용.",
                Price = 10,
                Owned = true,
                CanAfford = true,
            });
        }

        private void ApplyAffordFlags()
        {
            var gold = GameManager.Instance?.PlayerData != null
                ? GameManager.Instance.PlayerData.haveGold
                : 0;

            for (var i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (e == null)
                    continue;
                e.CanAfford = e.Owned || gold >= e.Price;
            }
        }

        private void RefreshSlots()
        {
            EnsureBuilt();
            if (_slotRoot == null)
                return;

            while (_slots.Count < _entries.Count)
            {
                ShopItemSlotUI slot;
                if (_slotPrefab != null)
                {
                    slot = Instantiate(_slotPrefab, _slotRoot);
                }
                else
                {
                    var go = SurvivorsRunUiBuild.Child(_slotRoot, $"Slot_{_slots.Count}");
                    var rt = go.GetComponent<RectTransform>();
                    SurvivorsRunUiBuild.SetAnchored(
                        rt,
                        new Vector2(0f, 1f),
                        new Vector2(1f, 1f),
                        new Vector2(0.5f, 1f),
                        new Vector2(0f, -12f - _slots.Count * 120f),
                        new Vector2(-24f, 110f));
                    slot = go.AddComponent<ShopItemSlotUI>();
                }

                slot.gameObject.SetActive(true);
                _slots.Add(slot);
            }

            for (var i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot == null)
                    continue;

                if (i < _entries.Count)
                {
                    slot.gameObject.SetActive(true);
                    slot.Bind(this, _entries[i]);
                }
                else
                {
                    slot.gameObject.SetActive(false);
                }
            }
        }

        private void EnsureBuilt()
        {
            if (_built)
                return;

            if (_titleText == null || _slotRoot == null || _closeButton == null)
                BuildRuntimeShell();

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnClickClose);
                _closeButton.onClick.AddListener(OnClickClose);
            }

            SurvivorsRunUiBuild.ApplyKoreanFontRecursive(transform);
            _built = true;
        }

        /// <summary>프리팹 없이도 레이아웃을 볼 수 있게 런타임 골격 생성.</summary>
        private void BuildRuntimeShell()
        {
            SurvivorsRunUiBuild.StretchFull(gameObject);

            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 80;
                gameObject.AddComponent<CanvasScaler>().uiScaleMode =
                    CanvasScaler.ScaleMode.ScaleWithScreenSize;
                gameObject.AddComponent<GraphicRaycaster>();
            }

            SurvivorsRunUiBuild.AddImage(gameObject, new Color(0.05f, 0.06f, 0.1f, 0.96f), raycast: true);

            var titleGo = SurvivorsRunUiBuild.Child(transform, "Title");
            SurvivorsRunUiBuild.SetAnchored(
                titleGo.GetComponent<RectTransform>(),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -24f), new Vector2(-40f, 56f));
            _titleText = SurvivorsRunUiBuild.AddText(titleGo, "상점", 40f, TextAlignmentOptions.Center);

            var goldGo = SurvivorsRunUiBuild.Child(transform, "Gold");
            SurvivorsRunUiBuild.SetAnchored(
                goldGo.GetComponent<RectTransform>(),
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-28f, -28f), new Vector2(220f, 40f));
            _goldText = SurvivorsRunUiBuild.AddText(goldGo, "칩 0", 26f, TextAlignmentOptions.Right);
            _goldText.color = new Color(1f, 0.92f, 0.45f, 1f);

            var closeGo = SurvivorsRunUiBuild.Child(transform, "Close");
            SurvivorsRunUiBuild.SetAnchored(
                closeGo.GetComponent<RectTransform>(),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -24f), new Vector2(120f, 48f));
            SurvivorsRunUiBuild.AddButton(closeGo, new Color(0.35f, 0.2f, 0.22f, 1f));
            var closeLabel = SurvivorsRunUiBuild.Child(closeGo.transform, "Label");
            SurvivorsRunUiBuild.StretchFull(closeLabel);
            SurvivorsRunUiBuild.AddText(closeLabel, "닫기", 22f, TextAlignmentOptions.Center);
            _closeButton = closeGo.GetComponent<Button>();

            var listGo = SurvivorsRunUiBuild.Child(transform, "SlotRoot");
            SurvivorsRunUiBuild.SetAnchored(
                listGo.GetComponent<RectTransform>(),
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -20f), new Vector2(-48f, -120f));
            _slotRoot = listGo.transform;
        }
    }
}
