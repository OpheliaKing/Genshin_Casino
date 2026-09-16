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
        [Tooltip("WEAPON일 때 사용. PASSIVE면 NONE.")]
        private SURVIVORSRUN_ATTACK_PATTERN _attackPattern = SURVIVORSRUN_ATTACK_PATTERN.NONE;
        public SURVIVORSRUN_ATTACK_PATTERN AttackPattern => _attackPattern;

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
        [Tooltip("중첩 1일 때 생성할 DamageObject 개수(Orbit 등).")]
        private int _baseObjectCount = 1;
        public int BaseObjectCount => _baseObjectCount;

        public bool HasAttackPattern =>
            _itemType == SURVIVORSRUN_ITEM_TYPE.WEAPON &&
            _attackPattern != SURVIVORSRUN_ATTACK_PATTERN.NONE &&
            _attackPattern != SURVIVORSRUN_ATTACK_PATTERN.CONTACT;
    }

    /// <summary>
    /// 아이템 분류. 공격 방식은 <see cref="SURVIVORSRUN_ATTACK_PATTERN"/>을 본다.
    /// </summary>
    public enum SURVIVORSRUN_ITEM_TYPE
    {
        NONE,
        PASSIVE,
        WEAPON,
    }
}
