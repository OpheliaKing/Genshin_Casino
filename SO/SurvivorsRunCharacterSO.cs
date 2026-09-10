using System.Collections.Generic;
using UnityEngine;

namespace SHIN
{
    [CreateAssetMenu(fileName = "SurvivorsRunCharacterSO", menuName = "SHIN/SurvivorsRun Character SO")]
    public class SurvivorsRunCharacterSO : ScriptableObject
    {
        [SerializeField]
        private List<SurvivorsRunUnitData> _characterList = new();

        public IReadOnlyList<SurvivorsRunUnitData> CharacterList => _characterList;

        public SurvivorsRunUnitData GetByUnitId(string unitId)
        {
            if (string.IsNullOrEmpty(unitId) || _characterList == null)
                return null;

            for (var i = 0; i < _characterList.Count; i++)
            {
                var data = _characterList[i];
                if (data != null && data.UnitId == unitId)
                    return data;
            }

            Debug.LogWarning($"[SurvivorsRunCharacterSO] unitId '{unitId}'에 해당하는 SurvivorsRunUnitData가 없습니다.");
            return null;
        }
    }
}
