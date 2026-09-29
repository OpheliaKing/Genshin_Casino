using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 인게임 HUD·레벨업·패배·일시정지 UI 및 긴급 스킬 스텁.
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

        [SerializeField]
        [Tooltip("긴급 스킬 쿨다운(초). 로직 연결 전 스텁용.")]
        private float _emergencySkillCooldown = 12f;

        private float _emergencyCooldownRemaining;

        public bool IsEmergencySkillReady => _emergencyCooldownRemaining <= 0f && !_defeatShown;
        public float EmergencySkillCooldownRemaining => Mathf.Max(0f, _emergencyCooldownRemaining);
        public float EmergencySkillCooldownNormalized
        {
            get
            {
                if (_emergencySkillCooldown <= 0f)
                    return 0f;
                return Mathf.Clamp01(_emergencyCooldownRemaining / _emergencySkillCooldown);
            }
        }

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
            _emergencyCooldownRemaining = 0f;
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
                if (item == null || !item.HasAttackPattern)
                    continue;
                pool.Add(item);
            }

            if (pool.Count == 0)
            {
                for (var i = 0; i < source.Count; i++)
                {
                    if (source[i] != null)
                        pool.Add(source[i]);
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

        /// <summary>긴급 스킬 스텁. 쿨만 돌리고 실제 효과는 이후 작업.</summary>
        public bool TryActivateEmergencySkill()
        {
            if (_defeatShown || _pausedByUi || _levelUpShowing)
                return false;

            if (!IsEmergencySkillReady)
                return false;

            _emergencyCooldownRemaining = Mathf.Max(0.1f, _emergencySkillCooldown);
            Debug.Log("[SurvivorsRun] Emergency skill stub activated (logic TBD).");
            return true;
        }

        private void TickEmergencySkillCooldown()
        {
            if (_emergencyCooldownRemaining <= 0f || _timeScale <= 0f)
                return;

            _emergencyCooldownRemaining -= Time.deltaTime * _timeScale;
            if (_emergencyCooldownRemaining < 0f)
                _emergencyCooldownRemaining = 0f;
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
                    await ApplyStartWeaponAsync(_selectedCharacter);
            }

            _defeatShown = false;
            _pendingLevelUps = 0;
            _levelUpShowing = false;
            _emergencyCooldownRemaining = 0f;
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
