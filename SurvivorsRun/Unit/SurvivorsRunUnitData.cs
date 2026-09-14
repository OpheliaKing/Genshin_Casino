using System;
using UnityEngine;

namespace SHIN
{
    [Serializable]
    public class SurvivorsRunUnitData
    {
        [SerializeField]
        private string _unitId;
        public string UnitId => _unitId;

        [SerializeField]
        private string _unitName;
        public string UnitName => _unitName;

        [SerializeField]
        private string _unitSpritePath;
        public string UnitSpritePath => _unitSpritePath;

        [SerializeField]
        [Tooltip("Addressables 프리팹 주소")]
        private string _unitPrefabPath;
        public string UnitPrefabPath => _unitPrefabPath;

        [SerializeField]
        private int _unitHP;
        public int UnitHP => _unitHP;

        [SerializeField]
        private float _unitAttack;
        public float UnitAttack => _unitAttack;

        [SerializeField]
        private float _unitDefense;
        public float UnitDefense => _unitDefense;

        [SerializeField]
        private float _unitSpeed;
        public float UnitSpeed => _unitSpeed;

        [SerializeField]
        [Tooltip("몬스터 전용. 비우면 돌진(Contact)만. 플레이어 데이터는 비워 둔다.")]
        private SurvivorsRunEnemyLoadout _enemyLoadout = new();
        public SurvivorsRunEnemyLoadout EnemyLoadout => _enemyLoadout;
    }
}
