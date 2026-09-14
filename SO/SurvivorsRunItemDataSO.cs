using System.Collections.Generic;
using UnityEngine;

namespace SHIN
{
    [CreateAssetMenu(fileName = "SurvivorsRunItemDataSO", menuName = "SHIN/SurvivorsRun Item Data SO")]
    public class SurvivorsRunItemDataSO : ScriptableObject
    {
        [SerializeField]
        private List<SurvivorsRunItemData> _itemList = new();

        public IReadOnlyList<SurvivorsRunItemData> ItemList => _itemList;

        public SurvivorsRunItemData GetByTid(string tid)
        {
            if (string.IsNullOrEmpty(tid) || _itemList == null)
                return null;

            for (var i = 0; i < _itemList.Count; i++)
            {
                var data = _itemList[i];
                if (data != null && data.Tid == tid)
                    return data;
            }

            Debug.LogWarning($"[SurvivorsRunItemDataSO] tid '{tid}'에 해당하는 SurvivorsRunItemData가 없습니다.");
            return null;
        }
    }
}
