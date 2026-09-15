using System;
using UnityEngine;

namespace SHIN
{
    [Serializable]
    public class SurvivorsRunMapData
    {
        [SerializeField]
        private string _mapId;
        public string MapId => _mapId;

        [SerializeField]
        private string _mapName;
        public string MapName => _mapName;

        [SerializeField]
        [Tooltip("Addressables 맵 프리팹 주소")]
        private string _mapPrefabPath;
        public string MapPrefabPath => _mapPrefabPath;

        [SerializeField]
        [Tooltip("난이도. 선택 규칙은 나중에 붙인다.")]
        private int _difficulty = 1;
        public int Difficulty => _difficulty;

        [SerializeField]
        [Tooltip("랜덤 가중치. 0 이하면 후보에서 제외.")]
        private float _randomWeight = 1f;
        public float RandomWeight => _randomWeight;
    }
}
