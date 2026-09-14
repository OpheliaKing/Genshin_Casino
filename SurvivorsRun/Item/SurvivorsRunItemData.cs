using UnityEngine;

namespace SHIN
{
    [System.Serializable]
    public class SurvivorsRunItemData
    {
        [SerializeField] private string _tid;
        [SerializeField] private string _name;
        [SerializeField] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private SURVIVORSRUN_ITEM_TYPE _itemType;
    }

    public enum SURVIVORSRUN_ITEM_TYPE
    {
        NONE,
        PASSIVE,
        ORBITPATTERN,
        PULSEPATTERN,
        AURAPATTERN,
        PROJECTILEPATTERN,
    }
}
