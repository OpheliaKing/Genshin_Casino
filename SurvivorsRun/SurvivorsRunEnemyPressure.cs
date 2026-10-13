using System;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 적 압박(스폰 텀·이동속도·체력) 튜닝값.
    /// 초반 거의 평탄 → 중반 킥 이후 급상승.
    /// </summary>
    [Serializable]
    public class SurvivorsRunEnemyPressureSettings
    {
        [Header("Spawn Interval")]
        [Tooltip("시작 스폰 간격(초). 클수록 초반에 천천히 나옴.")]
        [Min(0.05f)]
        public float BaseSpawnInterval = 1.15f;

        [Tooltip("스폰 간격 하한(초). 곡선 끝에서 도달.")]
        [Min(0.05f)]
        public float MinSpawnInterval = 0.14f;

        [Header("Move Speed (잡몹만, 보스 제외)")]
        [Tooltip("시작 이동속도 배율.")]
        [Min(0.01f)]
        public float BaseMoveSpeedMult = 1f;

        [Tooltip("이동속도 배율 상한.")]
        [Min(0.01f)]
        public float MaxMoveSpeedMult = 3f;

        [Header("HP (스폰 시점 배율)")]
        [Tooltip("시작 체력 배율. 초반은 1(평타 2대).")]
        [Min(0.01f)]
        public float BaseHpMult = 1f;

        [Tooltip("체력 배율 상한. 후반 난이도.")]
        [Min(0.01f)]
        public float MaxHpMult = 4.5f;

        [Header("Difficulty Curve")]
        [Tooltip("맵이 없을 때만 쓰는 폴백 길이(초). 실제 런에서는 MapData.CycleDurationSeconds(보스전까지)를 쓴다.")]
        [Min(1f)]
        public float CurveDurationSeconds = 120f;

        [Tooltip("이 비율(0~1)까지는 초반 완만. 예: 0.4 = 사이클 40%부터 급상승.")]
        [Range(0.05f, 0.85f)]
        public float MidKickNormalized = 0.4f;

        [Tooltip("중반 킥 직전까지 허용하는 최대 압박(0~1). 낮을수록 초·중반이 싱거움.")]
        [Range(0f, 0.5f)]
        public float EarlyPressureCap = 0.12f;

        [Tooltip("중반 이후 급상승 지수. 1=선형, 클수록 끝으로 갈수록 더 세게.")]
        [Min(1f)]
        public float LateCurvePower = 1.55f;

        [Header("Cap")]
        [Tooltip("동시 생존 적 상한.")]
        [Min(1)]
        public int MaxAliveEnemies = 70;

        public void ClampSelf()
        {
            BaseSpawnInterval = Mathf.Max(0.05f, BaseSpawnInterval);
            MinSpawnInterval = Mathf.Clamp(MinSpawnInterval, 0.05f, BaseSpawnInterval);
            BaseMoveSpeedMult = Mathf.Max(0.01f, BaseMoveSpeedMult);
            MaxMoveSpeedMult = Mathf.Max(BaseMoveSpeedMult, MaxMoveSpeedMult);
            BaseHpMult = Mathf.Max(0.01f, BaseHpMult);
            MaxHpMult = Mathf.Max(BaseHpMult, MaxHpMult);
            CurveDurationSeconds = Mathf.Max(1f, CurveDurationSeconds);
            MidKickNormalized = Mathf.Clamp(MidKickNormalized, 0.05f, 0.85f);
            EarlyPressureCap = Mathf.Clamp(EarlyPressureCap, 0f, 0.5f);
            LateCurvePower = Mathf.Max(1f, LateCurvePower);
            MaxAliveEnemies = Mathf.Max(1, MaxAliveEnemies);
        }

        /// <summary>폴백 기준. 실제 런은 Map CycleDuration을 쓴다.</summary>
        public float EstimateSecondsToSpawnFloor() => Mathf.Max(1f, CurveDurationSeconds);

        /// <summary>
        /// 0~1 정규화 시간 → 압박 가중치.
        /// MidKick 이전은 EarlyPressureCap까지 거의 평탄, 이후 1.0까지 급상승.
        /// </summary>
        public float EvaluateEase(float normalized01)
        {
            var t = Mathf.Clamp01(normalized01);
            var kick = MidKickNormalized;
            var earlyCap = EarlyPressureCap;

            if (t <= kick)
            {
                if (kick <= 0.0001f)
                    return earlyCap;
                return earlyCap * (t / kick);
            }

            var u = (t - kick) / Mathf.Max(0.0001f, 1f - kick);
            var late = Mathf.Pow(Mathf.Clamp01(u), LateCurvePower);
            return Mathf.Lerp(earlyCap, 1f, late);
        }
    }

    /// <summary>
    /// 런타임 압박. 경과 시간 기반 ease-in으로 스폰·이속·체력을 동시에 올린다.
    /// </summary>
    public class SurvivorsRunEnemyPressure
    {
        private SurvivorsRunEnemyPressureSettings _settings;
        private float _curveDurationSeconds = 120f;
        private float _elapsed;
        private float _spawnInterval;
        private float _moveSpeedMult;
        private float _hpMult;

        public float SpawnInterval => _spawnInterval;
        public float MoveSpeedMult => _moveSpeedMult;
        public float HpMult => _hpMult;
        public float Elapsed => _elapsed;
        public float CurveDurationSeconds => _curveDurationSeconds;
        public float CurveProgress01
        {
            get
            {
                if (_curveDurationSeconds <= 0f)
                    return 0f;
                return Mathf.Clamp01(_elapsed / _curveDurationSeconds);
            }
        }

        public int MaxAliveEnemies =>
            _settings != null ? Mathf.Max(1, _settings.MaxAliveEnemies) : 40;

        /// <summary>디버그용. 곡선 진행을 10등분한 단계.</summary>
        public int RampLevel => Mathf.Clamp(Mathf.FloorToInt(CurveProgress01 * 10f), 0, 10);

        public SurvivorsRunEnemyPressureSettings Settings => _settings;

        /// <param name="curveDurationOverride">
        /// 스테이지 사이클 시간(보스전까지). null이면 설정의 폴백 값.
        /// </param>
        public void Reset(SurvivorsRunEnemyPressureSettings settings, float? curveDurationOverride = null)
        {
            _settings = settings ?? new SurvivorsRunEnemyPressureSettings();
            _settings.ClampSelf();
            _curveDurationSeconds = curveDurationOverride.HasValue
                ? Mathf.Max(1f, curveDurationOverride.Value)
                : _settings.CurveDurationSeconds;
            _elapsed = 0f;
            ApplyCurve(0f);
        }

        public void Tick(float dt)
        {
            if (_settings == null || dt <= 0f)
                return;

            if (_elapsed >= _curveDurationSeconds)
            {
                ApplyCurve(1f);
                return;
            }

            _elapsed = Mathf.Min(_curveDurationSeconds, _elapsed + dt);
            ApplyCurve(CurveProgress01);
        }

        private void ApplyCurve(float normalized01)
        {
            var eased = _settings.EvaluateEase(normalized01);

            _spawnInterval = Mathf.Lerp(
                _settings.BaseSpawnInterval,
                _settings.MinSpawnInterval,
                eased);

            _moveSpeedMult = Mathf.Lerp(
                _settings.BaseMoveSpeedMult,
                _settings.MaxMoveSpeedMult,
                eased);

            _hpMult = Mathf.Lerp(
                _settings.BaseHpMult,
                _settings.MaxHpMult,
                eased);
        }
    }
}
