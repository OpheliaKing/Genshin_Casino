using System;
using UnityEngine;

namespace SHIN
{
    /// <summary>버프가 올리는 전투 스탯.</summary>
    public enum SURVIVORSRUN_BUFF_STAT
    {
        NONE = 0,
        /// <summary>발사 주기 배율. 1.3 = 30% 공속(쿨 1/1.3).</summary>
        ATTACK_SPEED,
        /// <summary>무기 사거리 배율. 1.3 = 30% 증가.</summary>
        RANGE,
    }

    /// <summary>
    /// 아이템 효과 한 줄. 지금은 BUFF 위주이며, 이후 공격+버프 복합에 확장한다.
    /// </summary>
    [Serializable]
    public class SurvivorsRunItemEffectEntry
    {
        [SerializeField]
        private SURVIVORSRUN_ATTACK_PATTERN _pattern = SURVIVORSRUN_ATTACK_PATTERN.BUFF;
        public SURVIVORSRUN_ATTACK_PATTERN Pattern => _pattern;

        [SerializeField]
        private SURVIVORSRUN_BUFF_STAT _buffStat = SURVIVORSRUN_BUFF_STAT.NONE;
        public SURVIVORSRUN_BUFF_STAT BuffStat => _buffStat;

        [SerializeField]
        [Tooltip("배율. 1 = 변화 없음, 1.3 = +30%. 1스택 기준값.")]
        private float _buffMultiplier = 1.25f;
        public float BuffMultiplier => _buffMultiplier;

        [SerializeField]
        [Tooltip("추가 스택마다 배율에 더함. 예: 1스택 1.4, StackBonus 0.15 → 2스택 1.55.")]
        private float _buffStackBonus = 0f;
        public float BuffStackBonus => _buffStackBonus;

        [SerializeField]
        [Tooltip("버프 지속 시간(초).")]
        private float _buffDuration = 5f;
        public float BuffDuration => _buffDuration;

        public bool IsBuff =>
            _pattern == SURVIVORSRUN_ATTACK_PATTERN.BUFF &&
            _buffStat != SURVIVORSRUN_BUFF_STAT.NONE &&
            _buffMultiplier > 0f &&
            _buffDuration > 0f;

        /// <summary>스택을 반영한 최종 버프 배율.</summary>
        public float ResolveBuffMultiplier(int stack)
        {
            stack = Mathf.Max(1, stack);
            return Mathf.Max(0.01f, _buffMultiplier + _buffStackBonus * (stack - 1));
        }
    }
}
