using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SHIN
{
    /// <summary>
    /// 뱀서 런 HUD. PlayerInfo·플레이어 HP 바인딩, 고유/액티브 스킬 버튼, Pause 진입.
    /// </summary>
    public class SurvivorsRunInGameUI : UIBase
    {
        private const int MaxItemSlots = 6;

        private sealed class SkillSlotView
        {
            public GameObject Root;
            public Button Button;
            public Image Icon;
            public Image CooldownFill;
            public TextMeshProUGUI Label;
            public int SlotIndex;
        }

        [SerializeField] private TextMeshProUGUI _lifeText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _killText;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _hpText;
        [SerializeField] private Slider _expSlider;
        [SerializeField] private Slider _hpSlider;
        [SerializeField] private Transform _itemRow;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _emergencySkillButton;
        [SerializeField] private Image _emergencyCooldownFill;
        [SerializeField] private TextMeshProUGUI _emergencyLabel;
        [SerializeField] private Transform _activeSkillRow;
        [SerializeField] private GameObject _bossWarningRoot;
        [SerializeField] private CanvasGroup _bossWarningGroup;
        [SerializeField] private TextMeshProUGUI _bossWarningText;
        [SerializeField] private Image _bossWarningOverlay;
        [SerializeField] private Image _bossWarningPanel;

        private static readonly Color BossOverlayColor = new(0.45f, 0.05f, 0.08f, 0.35f);
        private static readonly Color BossPanelColor = new(0.12f, 0.04f, 0.06f, 0.92f);
        private static readonly Color BossTextColor = new(1f, 0.45f, 0.4f, 1f);
        private static readonly Color StartOverlayColor = new(0.08f, 0.06f, 0.02f, 0.28f);
        private static readonly Color StartPanelColor = new(0.14f, 0.1f, 0.05f, 0.92f);
        private static readonly Color StartTextColor = new(1f, 0.92f, 0.55f, 1f);

        private SurvivorsRunManager _manager;
        private SurvivorsRunPlayerInfo _playerInfo;
        private SurvivorsRunUnitBase _boundPlayer;
        private readonly List<Image> _itemIcons = new();
        private readonly List<TextMeshProUGUI> _itemStacks = new();
        private readonly List<SkillSlotView> _activeSlots = new();
        private bool _built;
        private float _bossWarningTimer;
        private float _bossWarningDuration;
        private bool _bossWarningActive;

        public override void OnShow()
        {
            EnsureBuilt();
            SurvivorsRunUiBuild.ApplyKoreanFontRecursive(transform);
        }

        public override void OnHide()
        {
            Unbind();
        }

        public void Bind(SurvivorsRunManager manager)
        {
            EnsureBuilt();
            Unbind();

            _manager = manager;
            if (_manager == null)
                return;

            _playerInfo = _manager.PlayerInfo;
            if (_playerInfo != null)
            {
                _playerInfo.LifeChanged += OnLifeChanged;
                _playerInfo.ExpChanged += OnExpChanged;
                _playerInfo.LevelChanged += OnLevelChanged;
                _playerInfo.KillCountChanged += OnKillChanged;
                _playerInfo.OwnedItemsChanged += OnOwnedItemsChanged;

                OnLifeChanged(_playerInfo.Life);
                OnExpChanged(_playerInfo.Exp, _playerInfo.GetExpToNextLevel());
                OnLevelChanged(_playerInfo.Level);
                OnKillChanged(_playerInfo.KillCount);
                OnOwnedItemsChanged();
            }

            BindPlayerUnit(_manager.PlayerUnit);
            RefreshSkillVisuals();
        }

        public void RefreshEmergencyVisual() => RefreshSkillVisuals();

        /// <summary>보스 페이즈 진입 시 중앙 워닝. duration초 후 자동 숨김.</summary>
        public void ShowBossWarning(float duration = 2f, string message = null)
        {
            ShowCenterAnnounce(
                duration,
                string.IsNullOrWhiteSpace(message) ? "경고\n보스 출현" : message,
                BossTextColor,
                BossOverlayColor,
                BossPanelColor);
        }

        /// <summary>런 시작 START 문구. unscaled 시간 기준 await.</summary>
        public Task PlayStartAnnounceAsync(float duration = 1.25f, string message = "START")
        {
            return PlayCenterAnnounceAsync(
                duration,
                message,
                StartTextColor,
                StartOverlayColor,
                StartPanelColor);
        }

        /// <summary>중앙 안내 문구를 띄우고 duration초 후 자동 숨김까지 대기한다.</summary>
        public async Task PlayCenterAnnounceAsync(
            float duration,
            string message,
            Color textColor,
            Color overlayColor,
            Color panelColor)
        {
            ShowCenterAnnounce(duration, message, textColor, overlayColor, panelColor);
            await Task.Delay(TimeSpan.FromSeconds(Mathf.Max(0.1f, duration)));
            if (this == null)
                return;
            HideBossWarning();
        }

        private void ShowCenterAnnounce(
            float duration,
            string message,
            Color textColor,
            Color overlayColor,
            Color panelColor)
        {
            EnsureBuilt();
            EnsureBossWarning();

            if (_bossWarningRoot == null)
                return;

            if (_bossWarningOverlay != null)
                _bossWarningOverlay.color = overlayColor;
            if (_bossWarningPanel != null)
                _bossWarningPanel.color = panelColor;

            if (_bossWarningText != null)
            {
                _bossWarningText.text = string.IsNullOrWhiteSpace(message) ? "START" : message;
                _bossWarningText.color = textColor;
            }

            _bossWarningDuration = Mathf.Max(0.35f, duration);
            _bossWarningTimer = 0f;
            _bossWarningActive = true;
            _bossWarningRoot.SetActive(true);
            if (_bossWarningGroup != null)
                _bossWarningGroup.alpha = 1f;
        }

        private void HideBossWarning()
        {
            _bossWarningActive = false;
            _bossWarningTimer = 0f;
            if (_bossWarningGroup != null)
                _bossWarningGroup.alpha = 0f;
            if (_bossWarningRoot != null)
                _bossWarningRoot.SetActive(false);
        }

        private void TickBossWarning(float dt)
        {
            if (!_bossWarningActive)
                return;

            _bossWarningTimer += dt;
            var t = _bossWarningTimer;
            var dur = _bossWarningDuration;
            // 앞 60% 유지, 뒤 40% 페이드 아웃
            var hold = dur * 0.6f;
            if (_bossWarningGroup != null)
            {
                if (t <= hold)
                    _bossWarningGroup.alpha = 1f;
                else
                    _bossWarningGroup.alpha = Mathf.Clamp01(1f - (t - hold) / Mathf.Max(0.01f, dur - hold));
            }

            if (t >= dur)
                HideBossWarning();
        }

        private void RefreshSkillVisuals()
        {
            if (_manager == null)
                return;

            RefreshUniqueVisual();
            RefreshActiveVisuals();
        }

        private void RefreshUniqueVisual()
        {
            var ready = _manager.IsEmergencySkillReady;
            var fill = _manager.EmergencySkillCooldownNormalized;
            if (_emergencyCooldownFill != null)
                _emergencyCooldownFill.fillAmount = ready ? 0f : fill;
            if (_emergencySkillButton != null)
                _emergencySkillButton.interactable = ready;
            if (_emergencyLabel != null)
                _emergencyLabel.text = ready
                    ? _manager.UniqueSkillDisplayName
                    : $"{Mathf.CeilToInt(_manager.EmergencySkillCooldownRemaining)}";
        }

        private void RefreshActiveVisuals()
        {
            for (var i = 0; i < _activeSlots.Count; i++)
            {
                var slot = _activeSlots[i];
                if (slot == null || slot.Root == null)
                    continue;

                var item = _manager.GetActiveSkillItem(i);
                var has = item != null;
                slot.Root.SetActive(has);
                if (!has)
                    continue;

                var ready = _manager.IsActiveSkillReady(i);
                var fill = _manager.GetActiveSkillCooldownNormalized(i);
                if (slot.CooldownFill != null)
                    slot.CooldownFill.fillAmount = ready ? 0f : fill;
                if (slot.Button != null)
                    slot.Button.interactable = ready;

                var data = item.ItemData;
                if (slot.Icon != null)
                {
                    slot.Icon.sprite = data != null ? data.Icon : null;
                    slot.Icon.color = slot.Icon.sprite != null
                        ? Color.white
                        : new Color(0.3f, 0.55f, 0.75f, 0.95f);
                }

                if (slot.Label != null)
                {
                    slot.Label.text = ready
                        ? _manager.GetActiveSkillDisplayName(i)
                        : $"{Mathf.CeilToInt(_manager.GetActiveSkillCooldownRemaining(i))}";
                }
            }
        }

        private void Update()
        {
            if (_manager == null)
                return;

            RefreshSkillVisuals();
            RefreshTimer();
            TickBossWarning(Time.unscaledDeltaTime);
        }

        private void RefreshTimer()
        {
            if (_timerText == null || _manager == null)
                return;

            if (_manager.IsRunFinished)
            {
                _timerText.text = "END";
                return;
            }

            if (_manager.IsBossPhase)
            {
                _timerText.text = "BOSS";
                return;
            }

            var remain = Mathf.CeilToInt(_manager.CycleRemaining);
            var m = remain / 60;
            var s = remain % 60;
            _timerText.text = $"{m:0}:{s:00}";
        }

        private void BindPlayerUnit(SurvivorsRunUnitBase player)
        {
            if (_boundPlayer != null)
                _boundPlayer.HpChanged -= OnPlayerHpChanged;

            _boundPlayer = player;
            if (_boundPlayer != null)
            {
                _boundPlayer.HpChanged += OnPlayerHpChanged;
                OnPlayerHpChanged(_boundPlayer.Hp, _boundPlayer.MaxHp);
            }
            else
            {
                OnPlayerHpChanged(0, 1);
            }
        }

        private void Unbind()
        {
            if (_playerInfo != null)
            {
                _playerInfo.LifeChanged -= OnLifeChanged;
                _playerInfo.ExpChanged -= OnExpChanged;
                _playerInfo.LevelChanged -= OnLevelChanged;
                _playerInfo.KillCountChanged -= OnKillChanged;
                _playerInfo.OwnedItemsChanged -= OnOwnedItemsChanged;
                _playerInfo = null;
            }

            if (_boundPlayer != null)
            {
                _boundPlayer.HpChanged -= OnPlayerHpChanged;
                _boundPlayer = null;
            }

            _manager = null;
            HideBossWarning();
        }

        private void OnLifeChanged(int life)
        {
            if (_lifeText != null)
                _lifeText.text = $"♥ {life}";
        }

        private void OnExpChanged(int current, int toNext)
        {
            if (_expSlider == null)
                return;

            _expSlider.minValue = 0f;
            _expSlider.maxValue = 1f;
            _expSlider.value = toNext > 0 ? Mathf.Clamp01((float)current / toNext) : 0f;
        }

        private void OnLevelChanged(int level)
        {
            if (_levelText != null)
                _levelText.text = $"Lv.{level}";
        }

        private void OnKillChanged(int kills)
        {
            if (_killText != null)
                _killText.text = $"Kills {kills}";
        }

        private void OnPlayerHpChanged(int hp, int maxHp)
        {
            maxHp = Mathf.Max(1, maxHp);
            if (_hpSlider != null)
            {
                _hpSlider.minValue = 0f;
                _hpSlider.maxValue = 1f;
                _hpSlider.value = Mathf.Clamp01((float)hp / maxHp);
            }

            if (_hpText != null)
                _hpText.text = $"HP {hp}/{maxHp}";
        }

        private void OnOwnedItemsChanged()
        {
            RefreshInventoryRow();
            RefreshActiveVisuals();
        }

        private void RefreshInventoryRow()
        {
            if (_playerInfo == null)
                return;

            var items = _playerInfo.OwnedItems;
            var displayIndex = 0;
            for (var i = 0; i < items.Count && displayIndex < MaxItemSlots; i++)
            {
                var owned = items[i];
                if (owned?.ItemData == null)
                    continue;

                // 하단 인벤: 자동 무기/패시브만. UNIQUE·ACTIVE는 우측 스킬 슬롯.
                if (owned.ItemData.ItemType == SURVIVORSRUN_ITEM_TYPE.UNIQUE ||
                    owned.ItemData.ItemType == SURVIVORSRUN_ITEM_TYPE.ACTIVE)
                    continue;

                var icon = _itemIcons[displayIndex];
                var stack = _itemStacks[displayIndex];
                if (icon == null)
                    continue;

                icon.gameObject.SetActive(true);
                icon.sprite = owned.ItemData.Icon;
                icon.color = icon.sprite != null ? Color.white : new Color(0.35f, 0.35f, 0.45f, 0.9f);
                if (stack != null)
                    stack.text = owned.Stack > 1 ? owned.Stack.ToString() : string.Empty;
                displayIndex++;
            }

            for (var i = displayIndex; i < MaxItemSlots; i++)
            {
                if (i < _itemIcons.Count && _itemIcons[i] != null)
                    _itemIcons[i].gameObject.SetActive(false);
            }
        }

        private void OnPauseClicked()
        {
            _manager?.OpenPauseUi();
        }

        private void OnEmergencyClicked()
        {
            _manager?.TryActivateEmergencySkill();
            RefreshSkillVisuals();
        }

        private void OnActiveClicked(int slotIndex)
        {
            _manager?.TryActivateActiveSkill(slotIndex);
            RefreshSkillVisuals();
        }

        private void EnsureBuilt()
        {
            if (_built)
                return;

            SurvivorsRunUiBuild.StretchFull(gameObject);
            _built = true;

            if (_lifeText != null && _pauseButton != null)
            {
                EnsureTimerLabel();
                EnsureBossWarning();
                EnsureActiveSlotsExist();
                WireButtons();
                return;
            }

            BuildRuntimeTree();
            EnsureBossWarning();
            WireButtons();
        }

        private void EnsureBossWarning()
        {
            if (_bossWarningRoot != null)
            {
                if (_bossWarningGroup == null)
                    _bossWarningGroup = _bossWarningRoot.GetComponent<CanvasGroup>()
                        ?? _bossWarningRoot.AddComponent<CanvasGroup>();
                if (_bossWarningOverlay == null)
                    _bossWarningOverlay = _bossWarningRoot.GetComponent<Image>();
                if (_bossWarningPanel == null)
                {
                    var panelTf = _bossWarningRoot.transform.Find("Panel");
                    if (panelTf != null)
                        _bossWarningPanel = panelTf.GetComponent<Image>();
                }

                _bossWarningRoot.SetActive(false);
                return;
            }

            var root = SurvivorsRunUiBuild.Child(transform, "BossWarning");
            SurvivorsRunUiBuild.StretchFull(root);
            _bossWarningOverlay = SurvivorsRunUiBuild.AddImage(root, BossOverlayColor, raycast: false);

            var panel = SurvivorsRunUiBuild.Child(root.transform, "Panel");
            SurvivorsRunUiBuild.SetAnchored(panel.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(520f, 160f));
            _bossWarningPanel = SurvivorsRunUiBuild.AddImage(panel, BossPanelColor, raycast: false);

            var textGo = SurvivorsRunUiBuild.Child(panel.transform, "Text");
            SurvivorsRunUiBuild.StretchFull(textGo);
            _bossWarningText = SurvivorsRunUiBuild.AddText(textGo, "경고\n보스 출현", 42f, TextAlignmentOptions.Center);
            _bossWarningText.color = BossTextColor;

            _bossWarningGroup = root.GetComponent<CanvasGroup>();
            if (_bossWarningGroup == null)
                _bossWarningGroup = root.AddComponent<CanvasGroup>();
            _bossWarningGroup.alpha = 0f;
            _bossWarningGroup.blocksRaycasts = false;
            _bossWarningGroup.interactable = false;

            _bossWarningRoot = root;
            _bossWarningRoot.SetActive(false);
            SurvivorsRunUiBuild.ApplyKoreanFontRecursive(root.transform);
        }

        private void EnsureTimerLabel()
        {
            if (_timerText != null)
                return;

            var top = transform.Find("TopBar");
            if (top == null)
                return;

            _timerText = CreateLabel(top, "Timer", "2:00", TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(120f, 48f));
        }

        private void WireButtons()
        {
            if (_pauseButton != null)
            {
                _pauseButton.onClick.RemoveListener(OnPauseClicked);
                _pauseButton.onClick.AddListener(OnPauseClicked);
            }

            if (_emergencySkillButton != null)
            {
                _emergencySkillButton.onClick.RemoveListener(OnEmergencyClicked);
                _emergencySkillButton.onClick.AddListener(OnEmergencyClicked);
            }

            for (var i = 0; i < _activeSlots.Count; i++)
            {
                var slot = _activeSlots[i];
                if (slot?.Button == null)
                    continue;

                var index = slot.SlotIndex;
                slot.Button.onClick.RemoveAllListeners();
                slot.Button.onClick.AddListener(() => OnActiveClicked(index));
            }
        }

        private void EnsureActiveSlotsExist()
        {
            if (_activeSlots.Count > 0)
                return;

            if (_activeSkillRow == null)
            {
                var rowGo = transform.Find("SkillBar/ActiveRow");
                _activeSkillRow = rowGo != null ? rowGo : null;
            }

            if (_activeSkillRow == null)
                BuildSkillBar(createUnique: false);
        }

        private void BuildRuntimeTree()
        {
            // Top bar
            var top = SurvivorsRunUiBuild.Child(transform, "TopBar");
            var topRt = top.GetComponent<RectTransform>();
            SurvivorsRunUiBuild.SetAnchored(topRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -12f), new Vector2(-40f, 72f));
            SurvivorsRunUiBuild.AddImage(top, new Color(0f, 0f, 0f, 0.35f), raycast: false);

            _lifeText = CreateLabel(top.transform, "Life", "♥ 3", TextAlignmentOptions.Left, new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(160f, 48f));
            _levelText = CreateLabel(top.transform, "Level", "Lv.1", TextAlignmentOptions.Left, new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(200f, 0f), new Vector2(120f, 48f));

            var expGo = SurvivorsRunUiBuild.Child(top.transform, "ExpSlider");
            var expRt = expGo.GetComponent<RectTransform>();
            SurvivorsRunUiBuild.SetAnchored(expRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(340f, 0f), new Vector2(280f, 22f));
            _expSlider = CreateSlider(expGo, new Color(0.35f, 0.85f, 0.55f, 1f));

            _timerText = CreateLabel(top.transform, "Timer", "2:00", TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(120f, 48f));

            _killText = CreateLabel(top.transform, "Kills", "Kills 0", TextAlignmentOptions.Right, new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-120f, 0f), new Vector2(200f, 48f));

            var pauseGo = SurvivorsRunUiBuild.Child(top.transform, "PauseButton");
            var pauseRt = pauseGo.GetComponent<RectTransform>();
            SurvivorsRunUiBuild.SetAnchored(pauseRt, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-16f, 0f), new Vector2(72f, 48f));
            _pauseButton = SurvivorsRunUiBuild.AddButton(pauseGo, new Color(0.2f, 0.2f, 0.28f, 0.9f));
            SurvivorsRunUiBuild.AddText(SurvivorsRunUiBuild.Child(pauseGo.transform, "Label"), "Ⅱ", 28f,
                TextAlignmentOptions.Center);

            // Bottom-left HP + items
            var bottomLeft = SurvivorsRunUiBuild.Child(transform, "BottomLeft");
            var blRt = bottomLeft.GetComponent<RectTransform>();
            SurvivorsRunUiBuild.SetAnchored(blRt, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(24f, 24f), new Vector2(420f, 140f));

            _hpText = CreateLabel(bottomLeft.transform, "HpText", "HP 0/0", TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -8f), new Vector2(200f, 32f));

            var hpGo = SurvivorsRunUiBuild.Child(bottomLeft.transform, "HpSlider");
            var hpRt = hpGo.GetComponent<RectTransform>();
            SurvivorsRunUiBuild.SetAnchored(hpRt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, -44f), new Vector2(280f, 18f));
            _hpSlider = CreateSlider(hpGo, new Color(0.9f, 0.35f, 0.4f, 1f));

            _itemRow = SurvivorsRunUiBuild.Child(bottomLeft.transform, "ItemRow").transform;
            var itemRowRt = _itemRow.GetComponent<RectTransform>();
            SurvivorsRunUiBuild.SetAnchored(itemRowRt, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(0f, 64f));
            var layout = _itemRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            _itemIcons.Clear();
            _itemStacks.Clear();
            for (var i = 0; i < MaxItemSlots; i++)
            {
                var slot = SurvivorsRunUiBuild.Child(_itemRow, $"Item_{i}");
                var slotRt = slot.GetComponent<RectTransform>();
                slotRt.sizeDelta = new Vector2(56f, 56f);
                var icon = SurvivorsRunUiBuild.AddImage(slot, new Color(0.35f, 0.35f, 0.45f, 0.9f), raycast: false);
                var stackGo = SurvivorsRunUiBuild.Child(slot.transform, "Stack");
                SurvivorsRunUiBuild.StretchFull(stackGo);
                var stack = SurvivorsRunUiBuild.AddText(stackGo, string.Empty, 18f, TextAlignmentOptions.BottomRight);
                stack.margin = new Vector4(0f, 0f, 4f, 2f);
                slot.SetActive(false);
                _itemIcons.Add(icon);
                _itemStacks.Add(stack);
            }

            BuildSkillBar(createUnique: true);
        }

        private void BuildSkillBar(bool createUnique)
        {
            var bar = transform.Find("SkillBar")?.gameObject;
            if (bar == null)
                bar = SurvivorsRunUiBuild.Child(transform, "SkillBar");

            var barRt = bar.GetComponent<RectTransform>();
            SurvivorsRunUiBuild.SetAnchored(barRt, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-24f, 24f), new Vector2(460f, 150f));

            var barLayout = bar.GetComponent<HorizontalLayoutGroup>();
            if (barLayout == null)
                barLayout = bar.AddComponent<HorizontalLayoutGroup>();
            barLayout.spacing = 12f;
            barLayout.childAlignment = TextAnchor.LowerRight;
            barLayout.childControlWidth = false;
            barLayout.childControlHeight = false;
            barLayout.childForceExpandWidth = false;
            barLayout.childForceExpandHeight = false;
            barLayout.reverseArrangement = false;

            if (_activeSkillRow == null)
            {
                var rowGo = SurvivorsRunUiBuild.Child(bar.transform, "ActiveRow");
                _activeSkillRow = rowGo.transform;
                var rowRt = rowGo.GetComponent<RectTransform>();
                rowRt.sizeDelta = new Vector2(300f, 110f);
                var rowLayout = rowGo.AddComponent<HorizontalLayoutGroup>();
                rowLayout.spacing = 8f;
                rowLayout.childAlignment = TextAnchor.MiddleRight;
                rowLayout.childControlWidth = false;
                rowLayout.childControlHeight = false;
                rowLayout.childForceExpandWidth = false;
                rowLayout.childForceExpandHeight = false;
            }

            _activeSlots.Clear();
            for (var i = 0; i < SurvivorsRunManager.MaxActiveSkillSlots; i++)
                _activeSlots.Add(CreateActiveSlot(_activeSkillRow, i));

            if (!createUnique && _emergencySkillButton != null)
                return;

            // Unique skill (rightmost, larger)
            var existingUnique = bar.transform.Find("UniqueSkill")?.gameObject;
            var uniqueGo = existingUnique != null
                ? existingUnique
                : SurvivorsRunUiBuild.Child(bar.transform, "UniqueSkill");
            var uniqueRt = uniqueGo.GetComponent<RectTransform>();
            uniqueRt.sizeDelta = new Vector2(140f, 140f);

            if (_emergencySkillButton == null)
                _emergencySkillButton = SurvivorsRunUiBuild.AddButton(uniqueGo, new Color(0.45f, 0.25f, 0.7f, 0.92f));

            if (_emergencyCooldownFill == null)
            {
                var fillGo = SurvivorsRunUiBuild.Child(uniqueGo.transform, "CooldownFill");
                SurvivorsRunUiBuild.StretchFull(fillGo);
                _emergencyCooldownFill = SurvivorsRunUiBuild.AddImage(fillGo, new Color(0f, 0f, 0f, 0.55f), raycast: false);
                _emergencyCooldownFill.type = Image.Type.Filled;
                _emergencyCooldownFill.fillMethod = Image.FillMethod.Radial360;
                _emergencyCooldownFill.fillOrigin = (int)Image.Origin360.Top;
                _emergencyCooldownFill.fillClockwise = true;
                _emergencyCooldownFill.fillAmount = 0f;
            }

            if (_emergencyLabel == null)
            {
                var labelGo = SurvivorsRunUiBuild.Child(uniqueGo.transform, "Label");
                SurvivorsRunUiBuild.StretchFull(labelGo);
                _emergencyLabel = SurvivorsRunUiBuild.AddText(labelGo, "고유", 26f, TextAlignmentOptions.Center);
            }

            // move unique to end
            uniqueGo.transform.SetAsLastSibling();
        }

        private static SkillSlotView CreateActiveSlot(Transform parent, int index)
        {
            var go = SurvivorsRunUiBuild.Child(parent, $"Active_{index}");
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(96f, 96f);

            var button = SurvivorsRunUiBuild.AddButton(go, new Color(0.2f, 0.45f, 0.65f, 0.92f));
            var icon = SurvivorsRunUiBuild.AddImage(
                SurvivorsRunUiBuild.Child(go.transform, "Icon"),
                new Color(0.3f, 0.55f, 0.75f, 0.95f),
                raycast: false);
            SurvivorsRunUiBuild.StretchFull(icon.gameObject);
            var iconRt = icon.GetComponent<RectTransform>();
            iconRt.offsetMin = new Vector2(10f, 10f);
            iconRt.offsetMax = new Vector2(-10f, -10f);

            var fillGo = SurvivorsRunUiBuild.Child(go.transform, "CooldownFill");
            SurvivorsRunUiBuild.StretchFull(fillGo);
            var fill = SurvivorsRunUiBuild.AddImage(fillGo, new Color(0f, 0f, 0f, 0.55f), raycast: false);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillClockwise = true;
            fill.fillAmount = 0f;

            var labelGo = SurvivorsRunUiBuild.Child(go.transform, "Label");
            SurvivorsRunUiBuild.StretchFull(labelGo);
            var label = SurvivorsRunUiBuild.AddText(labelGo, "액티브", 18f, TextAlignmentOptions.Center);

            go.SetActive(false);
            return new SkillSlotView
            {
                Root = go,
                Button = button,
                Icon = icon,
                CooldownFill = fill,
                Label = label,
                SlotIndex = index,
            };
        }

        private static TextMeshProUGUI CreateLabel(
            Transform parent,
            string name,
            string text,
            TextAlignmentOptions align,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPos,
            Vector2 size)
        {
            var go = SurvivorsRunUiBuild.Child(parent, name);
            var rt = go.GetComponent<RectTransform>();
            SurvivorsRunUiBuild.SetAnchored(rt, anchorMin, anchorMax, anchorMin, anchoredPos, size);
            return SurvivorsRunUiBuild.AddText(go, text, 28f, align);
        }

        private static Slider CreateSlider(GameObject root, Color fillColor)
        {
            SurvivorsRunUiBuild.AddImage(root, new Color(0.1f, 0.1f, 0.12f, 0.8f), raycast: false);

            var fillArea = SurvivorsRunUiBuild.Child(root.transform, "Fill Area");
            SurvivorsRunUiBuild.StretchFull(fillArea);
            var fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.offsetMin = new Vector2(2f, 2f);
            fillAreaRt.offsetMax = new Vector2(-2f, -2f);

            var fill = SurvivorsRunUiBuild.Child(fillArea.transform, "Fill");
            SurvivorsRunUiBuild.StretchFull(fill);
            var fillImg = SurvivorsRunUiBuild.AddImage(fill, fillColor, raycast: false);

            var slider = root.GetComponent<Slider>();
            if (slider == null)
                slider = root.AddComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.interactable = false;
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = fillImg;
            return slider;
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}
