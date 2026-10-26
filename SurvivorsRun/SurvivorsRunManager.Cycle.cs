using System.Threading.Tasks;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 1사이클: 선택 맵(MapData) 설정 → 타이머 → 보스(리스트 랜덤) → 클리어.
    /// </summary>
    public partial class SurvivorsRunManager
    {
        private float _runElapsed;
        private bool _cycleTimerRunning;
        private bool _bossPhase;
        private bool _bossSpawnInFlight;
        private SurvivorsRunUnitBase _activeBoss;
        private bool _clearShown;
        private bool _clearRewardGranted;

        public bool IsRunFinished => _defeatShown || _clearShown;
        public float CycleDurationSeconds =>
            _selectedMapData != null ? _selectedMapData.CycleDurationSeconds : 120f;
        public float RunElapsed => _runElapsed;
        public float CycleRemaining => Mathf.Max(0f, CycleDurationSeconds - _runElapsed);
        public bool IsBossPhase => _bossPhase;
        public int ClearGoldReward =>
            _selectedMapData != null ? _selectedMapData.ClearGoldReward : 0;

        private void ResetCycleState()
        {
            _runElapsed = 0f;
            _cycleTimerRunning = false;
            _bossPhase = false;
            _bossSpawnInFlight = false;
            _activeBoss = null;
            _clearShown = false;
            _clearRewardGranted = false;
        }

        private Task BeginCycleTimerAsync()
        {
            if (_selectedMapData == null)
                Debug.LogWarning("[SurvivorsRunManager] SelectedMapData가 없습니다. 기본 120초·기본 적으로 진행합니다.");

            _runElapsed = 0f;
            _cycleTimerRunning = true;
            _bossPhase = false;
            _bossSpawnInFlight = false;
            _activeBoss = null;
            _clearShown = false;
            _clearRewardGranted = false;
            return Task.CompletedTask;
        }

        private void TickCycle(float dt)
        {
            if (!_cycleTimerRunning || IsRunFinished)
                return;

            if (_timeScale <= 0f)
                return;

            if (_bossPhase)
                return;

            _runElapsed += dt * _timeScale;
            if (_runElapsed < CycleDurationSeconds)
                return;

            _runElapsed = CycleDurationSeconds;
            _ = EnterBossPhaseAsync();
        }

        private async Task EnterBossPhaseAsync()
        {
            if (_bossPhase || IsRunFinished || _bossSpawnInFlight)
                return;

            _bossPhase = true;
            _cycleTimerRunning = false;
            StopEnemySpawning();
            _inGameUi?.ShowBossWarning(2f, "경고\n보스 출현");

            _bossSpawnInFlight = true;
            try
            {
                await SpawnBossAsync();
            }
            finally
            {
                _bossSpawnInFlight = false;
            }

            if (_activeBoss == null && !IsRunFinished)
            {
                Debug.LogWarning("[SurvivorsRunManager] 보스 스폰 실패 → 즉시 클리어 처리.");
                ShowClearUi();
            }
        }

        private async Task SpawnBossAsync()
        {
            await EnsureEnemySoAsync();

            SurvivorsRunUnitData data = null;
            var bossId = _selectedMapData != null ? _selectedMapData.PickRandomBossUnitId() : null;
            if (!string.IsNullOrWhiteSpace(bossId))
                data = GetEnemyData(bossId);
            if (data == null)
                data = GetDefaultEnemyData();
            if (data == null)
            {
                Debug.LogError("[SurvivorsRunManager] 보스용 적 데이터가 없습니다.");
                return;
            }

            if (!TryGetEnemySpawnPosition(out var position, useSpawnLaneCenterY: true))
            {
                Debug.LogError("[SurvivorsRunManager] 보스 스폰 위치를 못 구했습니다.");
                return;
            }

            var boss = await SpawnEnemyAsync(data, position) as SurvivorsRunEnemyBase;
            if (boss == null)
                return;

            // 스탯·스케일은 EnemySO UnitData 그대로 (맵에서 보스 전용 배수 없음).
            _activeBoss = boss;
            Debug.Log(
                $"[SurvivorsRun] 사이클 목표 적 스폰 map={_selectedMapData?.MapId} tid={data.UnitId} hp={data.UnitHP}");
        }

        public void NotifyBossDefeated(SurvivorsRunUnitBase boss)
        {
            if (boss == null || _activeBoss == null)
                return;
            if (boss != _activeBoss)
                return;

            _activeBoss = null;
            if (IsRunFinished)
                return;

            ShowClearUi();
        }

        public void ShowClearUi()
        {
            if (IsRunFinished)
                return;

            _clearShown = true;
            _cycleTimerRunning = false;
            _bossPhase = true;
            StopEnemySpawning();
            ClosePauseUiSilent();
            CloseLevelUpUi();
            _pendingLevelUps = 0;
            _levelUpShowing = false;
            TryGrantClearReward();
            PauseForOverlay();
            _ = ShowClearUiAsync();
        }

        private void TryGrantClearReward()
        {
            if (_clearRewardGranted)
                return;

            _clearRewardGranted = true;
            var reward = ClearGoldReward;
            if (reward <= 0)
                return;

            var gm = GameManager.Instance;
            if (gm == null)
                return;

            gm.AddGold(reward);
            Debug.Log($"[SurvivorsRun] 클리어 보상 +{reward}G → haveGold={gm.PlayerData?.haveGold}");
        }

        private async Task ShowClearUiAsync()
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
            _resultUi?.SetupClear(this);
        }
    }
}
