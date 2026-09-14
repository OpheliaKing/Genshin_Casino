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
        private readonly List<SURVIVORSRUN_ATTACK_PATTERN> _activePatterns = new();

        public SurvivorsRunEnemyLoadout Loadout => _loadout;
        public IReadOnlyList<SURVIVORSRUN_ATTACK_PATTERN> ActivePatterns => _activePatterns;

        private void Awake()
        {
            _owner = GetComponent<SurvivorsRunUnitBase>();
            _attack = GetComponent<SurvivorsRunAttack>();
        }

        /// <summary>
        /// 스폰 후 UnitData의 로드아웃을 넣을 때 호출.
        /// </summary>
        public void Setup(SurvivorsRunEnemyLoadout loadout)
        {
            _loadout = loadout ?? new SurvivorsRunEnemyLoadout();
            RebuildActivePatterns();
        }

        private void Start()
        {
            if (_activePatterns.Count == 0)
                RebuildActivePatterns();
        }

        private void RebuildActivePatterns()
        {
            _loadout.GetEffectivePatterns(_activePatterns);

            // MVP: 패턴 실행체는 아직 없고, 어떤 패턴으로 싸울지만 확정한다.
            // 예) Contact만 → 접촉 시 _attack.TryAttack(player)
            // 예) Orbit 포함 → 이후 OrbitPattern 컴포넌트 활성화
            Debug.Log(
                $"[EnemyLoadout] {_owner?.Tid ?? name}: patterns={string.Join(", ", _activePatterns)}",
                this);
        }

        /// <summary>
        /// 접촉 돌진용. Active에 Contact가 있을 때만 시도.
        /// </summary>
        public bool TryContactAttack(SurvivorsRunUnitBase target)
        {
            if (!_activePatterns.Contains(SURVIVORSRUN_ATTACK_PATTERN.CONTACT))
                return false;

            if (_attack == null)
                return false;

            return _attack.TryAttack(target);
        }

        public bool UsesOnlyContact()
        {
            return _activePatterns.Count == 1 &&
                   _activePatterns[0] == SURVIVORSRUN_ATTACK_PATTERN.CONTACT;
        }
    }
}
