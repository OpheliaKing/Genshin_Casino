using System.Collections.Generic;
using UnityEngine;

namespace SHIN
{
    [CreateAssetMenu(fileName = "SurvivorsRunEnemySO", menuName = "SHIN/SurvivorsRun Enemy SO")]
    public class SurvivorsRunEnemySO : ScriptableObject
    {
        [SerializeField]
        private List<SurvivorsRunUnitData> _enemyList = new();

        public IReadOnlyList<SurvivorsRunUnitData> EnemyList => _enemyList;

        public SurvivorsRunUnitData GetByUnitId(string unitId)
        {
            if (string.IsNullOrEmpty(unitId) || _enemyList == null)
                return null;

            for (var i = 0; i < _enemyList.Count; i++)
            {
                var data = _enemyList[i];
                if (data != null && data.UnitId == unitId)
                    return data;
            }

            Debug.LogWarning($"[SurvivorsRunEnemySO] unitId '{unitId}'에 해당하는 SurvivorsRunUnitData가 없습니다.");
            return null;
        }
    }
}
