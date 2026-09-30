using System.Collections.Generic;
using UnityEngine;

namespace SHIN
{
    [System.Serializable]
    public class SurvivorsRunItemData
    {
        [SerializeField] private string _tid;
        public string Tid => _tid;

        [SerializeField] private string _name;
        public string Name => _name;

        [SerializeField] private string _description;
        public string Description => _description;

        [SerializeField] private Sprite _icon;
        public Sprite Icon => _icon;

        [SerializeField] private SURVIVORSRUN_ITEM_TYPE _itemType;
        public SURVIVORSRUN_ITEM_TYPE ItemType => _itemType;

        [SerializeField]
        [Tooltip("WEAPON / ACTIVE / UNIQUE일 때 주 패턴. PASSIVE면 NONE.")]
        private SURVIVORSRUN_ATTACK_PATTERN _attackPattern = SURVIVORSRUN_ATTACK_PATTERN.NONE;
        public SURVIVORSRUN_ATTACK_PATTERN AttackPattern => _attackPattern;

        [SerializeField]
        [Tooltip("복합 효과 목록. BUFF 아이템은 여기에 공속·사거리 등을 넣는다.")]
        private List<SurvivorsRunItemEffectEntry> _effects = new();
        public IReadOnlyList<SurvivorsRunItemEffectEntry> Effects => _effects;

        [SerializeField]
        [Tooltip("최대 중첩. 도달하면 레벨업 후보에서 제외되고 추가 중첩도 막는다. 1 미만이면 1로 취급.")]
        private int _maxStack = 5;
        public int MaxStack => Mathf.Max(1, _maxStack);

        [SerializeField]
        [Tooltip("DamageObject 프리팹 Addressables 주소. 비우면 런타임에 기본 히트박스를 생성한다.")]
        private string _damagePrefabPath;
        public string DamagePrefabPath => _damagePrefabPath;

        [SerializeField]
        [Tooltip("기본 피해량. DamageObject Setup 시 주입.")]
        private int _baseDamage = 5;
        public int BaseDamage => _baseDamage;

        [SerializeField]
        [Tooltip("동일 대상 재타격 쿨(초). DamageObject 타겟 리스트 면역 시간.")]
        private float _hitCooldown = 0.5f;
        public float HitCooldown => _hitCooldown;

        [SerializeField]
        [Tooltip("발동 쿨타임(초). Pulse·Projectile 공격 주기 / ACTIVE·UNIQUE 버튼 쿨.")]
        private float _fireCooldown = 1f;
        public float FireCooldown => _fireCooldown;

        [SerializeField]
        [Tooltip("중첩 1일 때 생성할 DamageObject 개수(Orbit 등).")]
        private int _baseObjectCount = 1;
        public int BaseObjectCount => _baseObjectCount;

        [SerializeField]
        [Tooltip("Projectile 탄속. PROJECTILE 패턴에서만 사용. 0 이하면 런타임 기본값.")]
        private float _projectileSpeed = 10f;
        public float ProjectileSpeed => _projectileSpeed;

        [SerializeField]
        [Tooltip("Projectile 수명(초). PROJECTILE 패턴에서만 사용. 0 이하면 런타임 기본값.")]
        private float _projectileLifetime = 2.5f;
        public float ProjectileLifetime => _projectileLifetime;

        [SerializeField]
        [Tooltip("Projectile 조준 사거리. PROJECTILE 패턴에서만 사용. 0 이하면 런타임 기본값.")]
        private float _maxRange = 12f;
        public float MaxRange => _maxRange;

        [SerializeField]
        [Tooltip("명중 시 재생할 히트 이펙트 프리팹 Addressables 주소. 비우면 이펙트 없음.")]
        private string _hitEffectPrefabPath;
        public string HitEffectPrefabPath => _hitEffectPrefabPath;

        [SerializeField]
        [Tooltip("히트 이펙트 수명/안전 상한(초). 0 이하면 HitEffect 컴포넌트 기본값.")]
        private float _hitEffectLifetime = 1.5f;
        public float HitEffectLifetime => _hitEffectLifetime;

        [SerializeField]
        [Tooltip("ACTIVE/UNIQUE 발동 시 재생할 이펙트 Addressables 주소. BUFF면 지속시간 동안 루프.")]
        private string _activateEffectPrefabPath;
        public string ActivateEffectPrefabPath => _activateEffectPrefabPath;

        public bool HasActivateEffect => !string.IsNullOrWhiteSpace(_activateEffectPrefabPath);

        /// <summary>자동 무기(WEAPON)로 장착·발동 가능한 공격 패턴인지.</summary>
        public bool HasAttackPattern =>
            _itemType == SURVIVORSRUN_ITEM_TYPE.WEAPON &&
            UsesAttackPattern;

        /// <summary>입력 발동(ACTIVE / UNIQUE) 아이템인지.</summary>
        public bool IsInputTriggered =>
            _itemType == SURVIVORSRUN_ITEM_TYPE.ACTIVE ||
            _itemType == SURVIVORSRUN_ITEM_TYPE.UNIQUE;

        /// <summary>공격 패턴·데미지 오브젝트를 쓰는 타입인지.</summary>
        public bool UsesCombatPattern =>
            _itemType == SURVIVORSRUN_ITEM_TYPE.WEAPON ||
            _itemType == SURVIVORSRUN_ITEM_TYPE.ACTIVE ||
            _itemType == SURVIVORSRUN_ITEM_TYPE.UNIQUE;

        public bool UsesAttackPattern =>
            UsesCombatPattern &&
            _attackPattern != SURVIVORSRUN_ATTACK_PATTERN.NONE &&
            _attackPattern != SURVIVORSRUN_ATTACK_PATTERN.CONTACT;

        public bool IsBuffPattern => _attackPattern == SURVIVORSRUN_ATTACK_PATTERN.BUFF;

        public bool HasHitEffect => !string.IsNullOrWhiteSpace(_hitEffectPrefabPath);

        public bool IsAtMaxStack(int currentStack) => currentStack >= MaxStack;

        public int GetRemainingStackRoom(int currentStack) => Mathf.Max(0, MaxStack - Mathf.Max(0, currentStack));
    }

    /// <summary>
    /// 아이템 분류. 공격 방식은 <see cref="SURVIVORSRUN_ATTACK_PATTERN"/>을 본다.
    /// </summary>
    public enum SURVIVORSRUN_ITEM_TYPE
    {
        NONE,
        PASSIVE,
        WEAPON,
        /// <summary>런 중 획득하는 수동 발동 아이템.</summary>
        ACTIVE,
        /// <summary>캐릭터 고정 고유 스킬.</summary>
        UNIQUE,
    }
}
