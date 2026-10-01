using System;
using System.Collections.Generic;
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

        [Header("1 Cycle Clear")]
        [SerializeField]
        [Tooltip("일반 스폰 구간 길이(초). 끝나면 사이클 목표 적 페이즈.")]
        private float _cycleDurationSeconds = 120f;
        public float CycleDurationSeconds => Mathf.Max(1f, _cycleDurationSeconds);

        [SerializeField]
        [Tooltip("사이클 종료 시 스폰할 적 unitId 후보. 그중 랜덤 1. 비면 기본 적. 스탯은 EnemySO 그대로.")]
        private List<string> _bossUnitIds = new();
        public IReadOnlyList<string> BossUnitIds => _bossUnitIds;

        [SerializeField]
        [Tooltip("클리어 시 PlayerData.haveGold에 더할 칩.")]
        private int _clearGoldReward = 50;
        public int ClearGoldReward => Mathf.Max(0, _clearGoldReward);

        /// <summary>후보 중 유효한 unitId 하나. 없으면 null.</summary>
        public string PickRandomBossUnitId()
        {
            if (_bossUnitIds == null || _bossUnitIds.Count == 0)
                return null;

            var valid = 0;
            for (var i = 0; i < _bossUnitIds.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(_bossUnitIds[i]))
                    valid++;
            }

            if (valid <= 0)
                return null;

            var pick = UnityEngine.Random.Range(0, valid);
            for (var i = 0; i < _bossUnitIds.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(_bossUnitIds[i]))
                    continue;
                if (pick == 0)
                    return _bossUnitIds[i].Trim();
                pick--;
            }

            return null;
        }
    }
}
