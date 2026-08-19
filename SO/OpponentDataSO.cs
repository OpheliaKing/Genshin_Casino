using System;
using System.Collections.Generic;
using UnityEngine;

namespace SHIN
{
    [Serializable]
    public class OpponentData
    {
        public string tid;
        public string name;
        public string description;
        public string atlasAddress;
        public string spriteName;
        public string modelPath;
        public int haveGold;
    }

    [CreateAssetMenu(fileName = "OpponentDataSO", menuName = "SHIN/Opponent Data SO")]
    public class OpponentDataSO : ScriptableObject
    {
        [SerializeField] private List<OpponentData> _opponentList = new();

        public IReadOnlyList<OpponentData> OpponentList => _opponentList;

        public OpponentData GetByTid(string tid)
        {
            if (string.IsNullOrEmpty(tid) || _opponentList == null)
                return null;

            for (int i = 0; i < _opponentList.Count; i++)
            {
                var data = _opponentList[i];
                if (data != null && data.tid == tid)
                    return data;
            }

            Debug.LogWarning($"[OpponentDataSO] tid '{tid}'에 해당하는 OpponentData가 없습니다.");
            return null;
        }
    }
}
