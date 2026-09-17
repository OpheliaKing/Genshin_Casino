using System.Collections.Generic;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 몬스터 로드아웃(패턴 정보)을 런타임 타격에 연결한다.
    /// CONTACT → 프리팹에 배치된 Contact <see cref="SurvivorsRunDamageObject"/> 활성/셋업.
    /// 피격은 Unit 레이어 몸 콜라이더, 공격은 Unit이 아닌 자식 히트박스로 분리한다.
    /// 원거리 등은 이후 탄 스폰으로 확장.
    /// </summary>
    [RequireComponent(typeof(SurvivorsRunUnitBase))]
    public class SurvivorsRunEnemyLoadoutController : MonoBehaviour
    {
        [SerializeField]
        private SurvivorsRunEnemyLoadout _loadout = new();

        [SerializeField]
        [Tooltip("CONTACT 공격 히트박스. 프리팹 자식에 두고 콜라이더 크기를 조절한다. Unit 레이어면 안 된다.")]
        private SurvivorsRunDamageObject _contactDamageObject;

        [SerializeField]
        [Tooltip("AttackSpeed가 0일 때 Contact 히트 쿨 기본값(초).")]
        private float _defaultHitCooldown = 0.5f;

        private SurvivorsRunUnitBase _owner;
        private readonly List<SURVIVORSRUN_ATTACK_PATTERN> _activePatterns = new();
        private bool _missingContactLogged;

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

            ResolveContactDamageObject();
        }

        /// <summary>
        /// 프리팹에 배치된 Contact DamageObject만 사용한다. 런타임 AddComponent 하지 않는다.
        /// </summary>
        private void ResolveContactDamageObject()
        {
            if (_contactDamageObject != null)
                return;

            _contactDamageObject = GetComponentInChildren<SurvivorsRunDamageObject>(true);
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
            ResolveContactDamageObject();

            var useContact = _activePatterns.Contains(SURVIVORSRUN_ATTACK_PATTERN.CONTACT);
            if (!useContact)
            {
                if (_contactDamageObject != null)
                    _contactDamageObject.SetDamageEnabled(false);
                return;
            }

            if (_contactDamageObject == null)
            {
                if (!_missingContactLogged)
                {
                    _missingContactLogged = true;
                    Debug.LogError(
                        $"[EnemyLoadout] CONTACT 패턴인데 Contact DamageObject가 없습니다. " +
                        $"프리팹 자식에 SurvivorsRunDamageObject(+Trigger Collider)를 두세요. unit={_owner?.Tid ?? name}",
                        this);
                }

                return;
            }

            if (_owner == null)
                return;

            WarnIfContactOnUnitLayer();

            var hitCooldown = _owner.AttackSpeed > 0f
                ? 1f / _owner.AttackSpeed
                : Mathf.Max(0.05f, _defaultHitCooldown);

            _contactDamageObject.Setup(_owner, _owner.Attack, hitCooldown);
            _contactDamageObject.SetDamageEnabled(true);
        }

        private void WarnIfContactOnUnitLayer()
        {
            if (_contactDamageObject == null)
                return;

            var unitLayer = LayerMask.NameToLayer(PublicVariable.Layer.Unit);
            if (unitLayer < 0)
                return;

            if (_contactDamageObject.gameObject.layer != unitLayer)
                return;

            Debug.LogWarning(
                $"[EnemyLoadout] Contact 히트박스가 Unit 레이어입니다. " +
                $"피격과 공격이 같은 판정이 됩니다. Default 등으로 바꿔 주세요. ({name})",
                _contactDamageObject);
        }

        public bool UsesOnlyContact()
        {
            return _activePatterns.Count == 1 &&
                   _activePatterns[0] == SURVIVORSRUN_ATTACK_PATTERN.CONTACT;
        }
    }
}
