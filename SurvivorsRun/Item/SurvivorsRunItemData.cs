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
