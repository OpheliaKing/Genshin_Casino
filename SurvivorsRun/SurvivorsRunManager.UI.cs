using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 인게임 HUD·레벨업·패배·일시정지 UI 및 고유 스킬 발동.
    /// </summary>
    public partial class SurvivorsRunManager
    {
        private SurvivorsRunInGameUI _inGameUi;
        private SurvivorsRunLevelUpUI _levelUpUi;
        private SurvivorsRunResultUI _resultUi;
        private SurvivorsRunPauseUI _pauseUi;

        private bool _runUiBound;
        private bool _defeatShown;
        private bool _pausedByUi;
        private float _timeScaleBeforePause = 1f;
        private int _lastSeenLevel = 1;
        private int _pendingLevelUps;
        private bool _levelUpShowing;

        private SurvivorsRunItemBase ResolveUniqueSkillItem()
        {
            if (_playerUnit == null || string.IsNullOrEmpty(_uniqueSkillTid))
                return null;

            var controller = _playerUnit.GetComponent<SurvivorsRunPlayerItemController>();
            return controller != null ? controller.FindByTid(_uniqueSkillTid) : null;
        }

        public bool IsEmergencySkillReady
        {
            get
            {
                if (_defeatShown || string.IsNullOrEmpty(_uniqueSkillTid))
                    return false;

                var item = ResolveUniqueSkillItem();
                return item != null && item.CanActivate;
            }
        }

        public float EmergencySkillCooldownRemaining
        {
            get
            {
                var item = ResolveUniqueSkillItem();
                return item != null ? item.CooldownRemaining : 0f;
            }
        }

        public float EmergencySkillCooldownNormalized
        {
            get
            {
                var item = ResolveUniqueSkillItem();
                if (item == null)
                    return 0f;

                var duration = item.CooldownDuration;
                if (duration <= 0f)
                    return 0f;

                return Mathf.Clamp01(item.CooldownRemaining / duration);
            }
        }

        public string UniqueSkillDisplayName =>
            _uniqueSkillData != null && !string.IsNullOrWhiteSpace(_uniqueSkillData.Name)
                ? _uniqueSkillData.Name
                : "고유 스킬";

        private async Task ShowInGameHudAsync()
        {
            var uiManager = GameManager.Instance?.UIManager;
            if (uiManager == null)
            {
                Debug.LogError("[SurvivorsRunManager] UIManager가 없어 InGameUI를 열 수 없습니다.");
                return;
            }

            if (_inGameUi == null)
            {
                var shown = new TaskCompletionSource<SurvivorsRunInGameUI>();
                uiManager.Show(PublicVariable.Address.SurvivorsRunInGameUI, ui =>
                {
                    shown.TrySetResult(ui as SurvivorsRunInGameUI);
                });
                _inGameUi = await shown.Task;
            }

            if (_inGameUi == null)
            {
                Debug.LogError("[SurvivorsRunManager] SurvivorsRunInGameUI Show 실패.");
                return;
            }

            BindRunUiEvents();
            _inGameUi.Bind(this);
            _defeatShown = false;
            _pendingLevelUps = 0;
            _levelUpShowing = false;
            _lastSeenLevel = _playerInfo != null ? _playerInfo.Level : 1;
        }

        private void BindRunUiEvents()
        {
            if (_runUiBound)
                return;

            _playerInfo.LevelChanged += OnPlayerLevelChangedForUi;
            _runUiBound = true;
        }

        private void UnbindRunUiEvents()
        {
            if (!_runUiBound)
                return;

            _playerInfo.LevelChanged -= OnPlayerLevelChangedForUi;
            _runUiBound = false;
        }

        private void OnPlayerLevelChangedForUi(int level)
        {
            if (_defeatShown)
            {
                _lastSeenLevel = level;
                return;
            }

            if (level <= _lastSeenLevel)
            {
                _lastSeenLevel = level;
                return;
            }

            var gained = level - _lastSeenLevel;
            _lastSeenLevel = level;
            _pendingLevelUps += gained;
            TryShowNextLevelUp();
        }

        /// <summary>적 처치 시 호출. Kill/Exp 반영.</summary>
        public void NotifyEnemyKilled(SurvivorsRunUnitBase enemy)
        {
            if (_defeatShown || enemy == null)
                return;

            _playerInfo.AddKill(1);
            _playerInfo.AddExp(5);
        }

        private void TryShowNextLevelUp()
        {
            if (_defeatShown || _levelUpShowing || _pendingLevelUps <= 0)
                return;

            _ = ShowLevelUpUiAsync();
        }

        private async Task ShowLevelUpUiAsync()
        {
            if (_levelUpShowing || _pendingLevelUps <= 0)
                return;

            _levelUpShowing = true;
            PauseForOverlay();

            var uiManager = GameManager.Instance?.UIManager;
            if (uiManager == null)
            {
                _levelUpShowing = false;
                ResumeFromOverlay();
                return;
            }

            var shown = new TaskCompletionSource<SurvivorsRunLevelUpUI>();
            uiManager.Show(PublicVariable.Address.SurvivorsRunLevelUpUI, ui =>
            {
                shown.TrySetResult(ui as SurvivorsRunLevelUpUI);
            });
            _levelUpUi = await shown.Task;
            if (_levelUpUi == null)
            {
                _levelUpShowing = false;
                _pendingLevelUps = Mathf.Max(0, _pendingLevelUps - 1);
                ResumeFromOverlay();
                return;
            }

            await EnsureItemDataSoAsync();
            var choices = BuildLevelUpChoices(3);
            _levelUpUi.Setup(this, choices);
        }

        private List<SurvivorsRunItemData> BuildLevelUpChoices(int count)
        {
            var result = new List<SurvivorsRunItemData>(count);
            var source = _itemDataSo != null ? _itemDataSo.ItemList : null;
            if (source == null || source.Count == 0)
                return result;

            var pool = new List<SurvivorsRunItemData>();
            for (var i = 0; i < source.Count; i++)
            {
                var item = source[i];
                if (item == null)
                    continue;

                // 레벨업: 자동 무기 + 액티브. UNIQUE는 캐릭터 고정이라 제외.
                if (item.HasAttackPattern ||
                    (item.ItemType == SURVIVORSRUN_ITEM_TYPE.ACTIVE && item.UsesAttackPattern))
                {
                    if (IsOwnedAtMaxStack(item))
                        continue;

                    pool.Add(item);
                }
            }

            if (pool.Count == 0)
            {
                for (var i = 0; i < source.Count; i++)
                {
                    var fallback = source[i];
                    if (fallback == null || IsOwnedAtMaxStack(fallback))
                        continue;
                    pool.Add(fallback);
                }
            }

            while (result.Count < count && pool.Count > 0)
            {
                var idx = Random.Range(0, pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }

            return result;
        }

        private bool IsOwnedAtMaxStack(SurvivorsRunItemData itemData)
        {
            if (itemData == null || _playerInfo == null)
                return false;

            var owned = _playerInfo.FindOwned(itemData.Tid);
            return owned != null && itemData.IsAtMaxStack(owned.Stack);
        }

        public void ConfirmLevelUpChoice(SurvivorsRunItemData itemData)
        {
            if (itemData != null)
                _playerInfo.AddItem(itemData);

            CloseLevelUpUi();
            _pendingLevelUps = Mathf.Max(0, _pendingLevelUps - 1);
            _levelUpShowing = false;

            if (_pendingLevelUps > 0)
                TryShowNextLevelUp();
            else
                ResumeFromOverlay();
        }

        private void CloseLevelUpUi()
        {
            var uiManager = GameManager.Instance?.UIManager;
            if (uiManager != null && _levelUpUi != null)
            {
                uiManager.Close(_levelUpUi, restoreVisibleStack: true);
                _levelUpUi = null;
            }
        }

        public void OpenPauseUi()
        {
            if (_defeatShown || _levelUpShowing)
                return;

            _ = OpenPauseUiAsync();
        }

        private async Task OpenPauseUiAsync()
        {
            if (_pauseUi != null)
                return;

            PauseForOverlay();

            var uiManager = GameManager.Instance?.UIManager;
            if (uiManager == null)
            {
                ResumeFromOverlay();
                return;
            }

            var shown = new TaskCompletionSource<SurvivorsRunPauseUI>();
            uiManager.Show(PublicVariable.Address.SurvivorsRunPauseUI, ui =>
            {
                shown.TrySetResult(ui as SurvivorsRunPauseUI);
            });
            _pauseUi = await shown.Task;
            _pauseUi?.Setup(this);
        }

        public void ClosePauseUi()
        {
            var uiManager = GameManager.Instance?.UIManager;
            if (uiManager != null && _pauseUi != null)
            {
                uiManager.Close(_pauseUi, restoreVisibleStack: true);
                _pauseUi = null;
            }

            if (!_levelUpShowing && !_defeatShown)
                ResumeFromOverlay();
        }

        public void ShowDefeatUi()
        {
            if (_defeatShown)
                return;

            _defeatShown = true;
            StopEnemySpawning();
            ClosePauseUiSilent();
            CloseLevelUpUi();
            _pendingLevelUps = 0;
            _levelUpShowing = false;
            PauseForOverlay();
            _ = ShowDefeatUiAsync();
        }

        private async Task ShowDefeatUiAsync()
        {
            var uiManager = GameManager.Instance?.UIManager;
            if (uiManager == null)
                return;

            var shown = new TaskCompletionSource<SurvivorsRunResultUI>();
            uiManager.Show(PublicVariable.Address.SurvivorsRunResultUI, ui =>
            {
                shown.TrySetResult(ui as SurvivorsRunResultUI);
            });
            _resultUi = await shown.Task;
            _resultUi?.SetupDefeat(this);
        }

        private void ClosePauseUiSilent()
        {
            var uiManager = GameManager.Instance?.UIManager;
            if (uiManager != null && _pauseUi != null)
            {
                uiManager.Close(_pauseUi, restoreVisibleStack: true);
                _pauseUi = null;
            }
        }

        private void CloseResultUi()
        {
            var uiManager = GameManager.Instance?.UIManager;
            if (uiManager != null && _resultUi != null)
            {
                uiManager.Close(_resultUi, restoreVisibleStack: true);
                _resultUi = null;
            }
        }

        private void CloseInGameUi()
        {
            var uiManager = GameManager.Instance?.UIManager;
            if (uiManager != null && _inGameUi != null)
            {
                uiManager.Close(_inGameUi, restoreVisibleStack: false);
                _inGameUi = null;
            }
        }

        private void PauseForOverlay()
        {
            if (!_pausedByUi)
            {
                _timeScaleBeforePause = _timeScale > 0f ? _timeScale : 1f;
                _pausedByUi = true;
            }

            SetTimeScale(0f);
            EnableInputMap(InputManager.ActionMapName.UI);
        }

        private void ResumeFromOverlay()
        {
            _pausedByUi = false;
            SetTimeScale(_timeScaleBeforePause > 0f ? _timeScaleBeforePause : 1f);
            EnableInputMap(InputManager.ActionMapName.SurvivorsRun);
        }

        /// <summary>고유 스킬(UNIQUE) 수동 발동. HUD 긴급 버튼과 동일 경로.</summary>
        public bool TryActivateEmergencySkill()
        {
            if (_defeatShown || _pausedByUi || _levelUpShowing)
                return false;

            if (string.IsNullOrEmpty(_uniqueSkillTid) || _playerUnit == null)
                return false;

            var controller = _playerUnit.GetComponent<SurvivorsRunPlayerItemController>();
            if (controller == null)
                return false;

            return controller.TryActivateItem(_uniqueSkillTid);
        }

        public const int MaxActiveSkillSlots = 3;

        public SurvivorsRunItemBase GetActiveSkillItem(int slotIndex)
        {
            if (slotIndex < 0 || _playerUnit == null)
                return null;

            var controller = _playerUnit.GetComponent<SurvivorsRunPlayerItemController>();
            if (controller == null)
                return null;

            var found = 0;
            for (var i = 0; i < controller.Items.Count; i++)
            {
                var item = controller.Items[i];
                if (item?.ItemData == null || item.ItemData.ItemType != SURVIVORSRUN_ITEM_TYPE.ACTIVE)
                    continue;

                if (found == slotIndex)
                    return item;

                found++;
            }

            return null;
        }

        public bool TryActivateActiveSkill(int slotIndex)
        {
            if (_defeatShown || _pausedByUi || _levelUpShowing)
                return false;

            if (_playerUnit == null)
                return false;

            var controller = _playerUnit.GetComponent<SurvivorsRunPlayerItemController>();
            return controller != null && controller.TryActivateActiveAt(slotIndex);
        }

        public bool IsActiveSkillReady(int slotIndex)
        {
            if (_defeatShown)
                return false;

            var item = GetActiveSkillItem(slotIndex);
            return item != null && item.CanActivate;
        }

        public float GetActiveSkillCooldownRemaining(int slotIndex)
        {
            var item = GetActiveSkillItem(slotIndex);
            return item != null ? item.CooldownRemaining : 0f;
        }

        public float GetActiveSkillCooldownNormalized(int slotIndex)
        {
            var item = GetActiveSkillItem(slotIndex);
            if (item == null)
                return 0f;

            var duration = item.CooldownDuration;
            if (duration <= 0f)
                return 0f;

            return Mathf.Clamp01(item.CooldownRemaining / duration);
        }

        public string GetActiveSkillDisplayName(int slotIndex)
        {
            var item = GetActiveSkillItem(slotIndex);
            if (item?.ItemData != null && !string.IsNullOrWhiteSpace(item.ItemData.Name))
                return item.ItemData.Name;

            return "액티브";
        }

        private void TickEmergencySkillCooldown()
        {
            // 쿨은 UNIQUE/ACTIVE Item.Tick에서 감소. 매니저 별도 타이머 없음.
        }

        public void RetryAfterDefeat()
        {
            _ = RetryAfterDefeatAsync();
        }

        private async Task RetryAfterDefeatAsync()
        {
            CloseResultUi();
            ClosePauseUiSilent();
            CloseLevelUpUi();

            ReleaseAllEnemies();
            ClearAttackTarget();

            if (_playerUnit != null)
            {
                _playerUnit.RestoreFullHp();
                var itemController = EnsurePlayerItemController(_playerUnit);
                _playerInfo.Reset();
                _playerInfo.BindItemController(itemController);
                _lastSeenLevel = 1;
                if (_selectedCharacter != null)
                {
                    await ApplyStartWeaponAsync(_selectedCharacter);
                    await ApplyUniqueSkillAsync(_selectedCharacter);
                    await ApplyStartActiveAsync(_selectedCharacter);
                }
            }

            _defeatShown = false;
            _pendingLevelUps = 0;
            _levelUpShowing = false;
            _inGameUi?.Bind(this);

            ResumeFromOverlay();
            await StartEnemySpawningAsync();
        }

        public void ExitToLobby()
        {
            _ = ExitToLobbyAsync();
        }

        private async Task ExitToLobbyAsync()
        {
            UnbindRunUiEvents();
            CloseResultUi();
            ClosePauseUiSilent();
            CloseLevelUpUi();
            CloseInGameUi();

            StopEnemySpawning();
            ReleaseAllEnemies();
            ReleasePlayerInstance();
            ClearAttackTarget();
            ClearAllPools();
            RestoreLobbyInputMap();

            var sessionGo = gameObject;
            var resourceManager = GameManager.Instance?.ResourceManager;
            var uiManager = GameManager.Instance?.UIManager;

            if (uiManager == null)
            {
                if (resourceManager != null)
                    resourceManager.ReleaseInstance(sessionGo);
                else
                    Destroy(sessionGo);
                return;
            }

            await uiManager.FadeTransitionAsync(() =>
            {
                if (resourceManager != null)
                    resourceManager.ReleaseInstance(sessionGo);
                else if (sessionGo != null)
                    Destroy(sessionGo);

                var shown = new TaskCompletionSource<bool>();
                uiManager.Show(PublicVariable.Address.MainUI, _ => shown.TrySetResult(true));
                return shown.Task;
            });
        }
    }
}
