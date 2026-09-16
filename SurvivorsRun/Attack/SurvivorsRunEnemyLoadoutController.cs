using System.Collections.Generic;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 몬스터 로드아웃(패턴 정보)을 런타임 타격에 연결한다.
    /// CONTACT → 몸 <see cref="SurvivorsRunDamageObject"/> 활성.
    /// 원거리 등은 이후 탄 스폰으로 확장.
    /// </summary>
    [RequireComponent(typeof(SurvivorsRunUnitBase))]
    public class SurvivorsRunEnemyLoadoutController : MonoBehaviour
    {
        [SerializeField]
        private SurvivorsRunEnemyLoadout _loadout = new();

        [SerializeField]
        [Tooltip("AttackSpeed가 0일 때 Contact 히트 쿨 기본값(초).")]
        private float _defaultHitCooldown = 0.5f;

        private SurvivorsRunUnitBase _owner;
        private SurvivorsRunDamageObject _contactDamageObject;
        private readonly List<SURVIVORSRUN_ATTACK_PATTERN> _activePatterns = new();

        public SurvivorsRunEnemyLoadout Loadout => _loadout;
        public IReadOnlyList<SURVIVORSRUN_ATTACK_PATTERN> ActivePatterns => _activePatterns;

        private void Awake()
        {
            _owner = GetComponent<SurvivorsRunUnitBase>();
            EnsureRuntimeModules();
        }

        /// <summary>
        /// 스폰 후 UnitData의 로드아웃을 넣을 때 호출.
        /// </summary>
        public void Setup(SurvivorsRunEnemyLoadout loadout)
        {
            _loadout = loadout ?? new SurvivorsRunEnemyLoadout();
            EnsureRuntimeModules();
            RebuildActivePatterns();
        }

        private void Start()
        {
            if (_activePatterns.Count == 0)
                RebuildActivePatterns();
        }

        private void EnsureRuntimeModules()
        {
            if (_owner == null)
                _owner = GetComponent<SurvivorsRunUnitBase>();

            if (GetComponent<SurvivorsRunEnemyChase>() == null)
                gameObject.AddComponent<SurvivorsRunEnemyChase>();

            EnsureContactDamageObject();
        }

        private void EnsureContactDamageObject()
        {
            _contactDamageObject = GetComponent<SurvivorsRunDamageObject>();
            if (_contactDamageObject == null)
                _contactDamageObject = gameObject.AddComponent<SurvivorsRunDamageObject>();
        }

        private void RebuildActivePatterns()
        {
            _loadout.GetEffectivePatterns(_activePatterns);
            SyncContactDamageObject();

            Debug.Log(
                $"[EnemyLoadout] {_owner?.Tid ?? name}: patterns={string.Join(", ", _activePatterns)}",
                this);
        }

        private void SyncContactDamageObject()
        {
            EnsureContactDamageObject();
            if (_contactDamageObject == null || _owner == null)
                return;

            var useContact = _activePatterns.Contains(SURVIVORSRUN_ATTACK_PATTERN.CONTACT);
            if (!useContact)
            {
                _contactDamageObject.SetDamageEnabled(false);
                return;
            }

            var hitCooldown = _owner.AttackSpeed > 0f
                ? 1f / _owner.AttackSpeed
                : Mathf.Max(0.05f, _defaultHitCooldown);

            _contactDamageObject.Setup(_owner, _owner.Attack, hitCooldown);
            _contactDamageObject.SetDamageEnabled(true);
        }

        public bool UsesOnlyContact()
        {
            return _activePatterns.Count == 1 &&
                   _activePatterns[0] == SURVIVORSRUN_ATTACK_PATTERN.CONTACT;
        }
    }
}
