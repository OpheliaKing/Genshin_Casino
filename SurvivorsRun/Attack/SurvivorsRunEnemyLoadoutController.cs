using System.Collections.Generic;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 몬스터 로드아웃을 Attack에 연결하는 베이스.
    /// - 로드아웃 비움 → Contact(돌진)만
    /// - Orbit 등이 있으면 해당 패턴 사용 (패턴 본체는 이후 구현)
    /// </summary>
    [RequireComponent(typeof(SurvivorsRunUnitBase))]
    [RequireComponent(typeof(SurvivorsRunAttack))]
    public class SurvivorsRunEnemyLoadoutController : MonoBehaviour
    {
        [SerializeField]
        private SurvivorsRunEnemyLoadout _loadout = new();

        private SurvivorsRunUnitBase _owner;
        private SurvivorsRunAttack _attack;
        private SurvivorsRunContactPattern _contactPattern;
        private readonly List<SURVIVORSRUN_ATTACK_PATTERN> _activePatterns = new();
        private SurvivorsRunAttackPatternBase[] _patternModules;

        public SurvivorsRunEnemyLoadout Loadout => _loadout;
        public IReadOnlyList<SURVIVORSRUN_ATTACK_PATTERN> ActivePatterns => _activePatterns;

        private void Awake()
        {
            _owner = GetComponent<SurvivorsRunUnitBase>();
            _attack = GetComponent<SurvivorsRunAttack>();
            EnsurePatternModules();
        }

        /// <summary>
        /// 스폰 후 UnitData의 로드아웃을 넣을 때 호출.
        /// </summary>
        public void Setup(SurvivorsRunEnemyLoadout loadout)
        {
            _loadout = loadout ?? new SurvivorsRunEnemyLoadout();
            EnsurePatternModules();
            RebuildActivePatterns();
        }

        private void Start()
        {
            if (_activePatterns.Count == 0)
                RebuildActivePatterns();
        }

        private void EnsurePatternModules()
        {
            if (_owner == null)
                _owner = GetComponent<SurvivorsRunUnitBase>();
            if (_attack == null)
                _attack = GetComponent<SurvivorsRunAttack>();

            _contactPattern = GetComponent<SurvivorsRunContactPattern>();
            if (_contactPattern == null)
                _contactPattern = gameObject.AddComponent<SurvivorsRunContactPattern>();

            _contactPattern.Setup(_owner, _attack);

            if (GetComponent<SurvivorsRunEnemyChase>() == null)
                gameObject.AddComponent<SurvivorsRunEnemyChase>();

            _patternModules = GetComponents<SurvivorsRunAttackPatternBase>();
            for (var i = 0; i < _patternModules.Length; i++)
            {
                if (_patternModules[i] != null)
                    _patternModules[i].Setup(_owner, _attack);
            }
        }

        private void RebuildActivePatterns()
        {
            _loadout.GetEffectivePatterns(_activePatterns);
            SyncPatternModules();

            Debug.Log(
                $"[EnemyLoadout] {_owner?.Tid ?? name}: patterns={string.Join(", ", _activePatterns)}",
                this);
        }

        private void SyncPatternModules()
        {
            if (_patternModules == null || _patternModules.Length == 0)
                EnsurePatternModules();

            for (var i = 0; i < _patternModules.Length; i++)
            {
                var module = _patternModules[i];
                if (module == null)
                    continue;

                var enabled = _activePatterns.Contains(module.Pattern);
                module.SetPatternEnabled(enabled);
            }
        }

        /// <summary>
        /// 접촉 돌진용. Active에 Contact가 있을 때만 시도.
        /// </summary>
        public bool TryContactAttack(SurvivorsRunUnitBase target)
        {
            if (_contactPattern == null)
                EnsurePatternModules();

            if (_contactPattern == null || !_contactPattern.IsPatternEnabled)
                return false;

            return _contactPattern.TryAttackTarget(target);
        }

        public bool UsesOnlyContact()
        {
            return _activePatterns.Count == 1 &&
                   _activePatterns[0] == SURVIVORSRUN_ATTACK_PATTERN.CONTACT;
        }
    }
}
