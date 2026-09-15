using System.Collections.Generic;
using UnityEngine;

namespace SHIN
{
    [CreateAssetMenu(fileName = "SurvivorsRunMapSO", menuName = "SHIN/SurvivorsRun Map SO")]
    public class SurvivorsRunMapSO : ScriptableObject
    {
        [SerializeField]
        private List<SurvivorsRunMapData> _mapList = new();

        public IReadOnlyList<SurvivorsRunMapData> MapList => _mapList;

        public SurvivorsRunMapData GetByMapId(string mapId)
        {
            if (string.IsNullOrEmpty(mapId) || _mapList == null)
                return null;

            for (var i = 0; i < _mapList.Count; i++)
            {
                var data = _mapList[i];
                if (data != null && data.MapId == mapId)
                    return data;
            }

            Debug.LogWarning($"[SurvivorsRunMapSO] mapId '{mapId}'에 해당하는 SurvivorsRunMapData가 없습니다.");
            return null;
        }

        /// <summary>목록의 첫 유효 맵. 선택 규칙 전까지의 기본값.</summary>
        public SurvivorsRunMapData GetDefaultMap()
        {
            if (_mapList == null)
                return null;

            for (var i = 0; i < _mapList.Count; i++)
            {
                if (_mapList[i] != null)
                    return _mapList[i];
            }

            return null;
        }
    }
}
