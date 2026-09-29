using System.Collections.Generic;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 플레이어 전투 배율(공속·사거리). 버프 아이템이 여기에 값을 쌓는다.
    /// </summary>
    public class SurvivorsRunPlayerCombatMods : MonoBehaviour
    {
        private struct TimedBuff
        {
            public SURVIVORSRUN_BUFF_STAT Stat;
            public float Multiplier;
            public float Remaining;
        }

        private readonly List<TimedBuff> _buffs = new();
        private SurvivorsRunUnitBase _owner;

        public float AttackSpeedMult => ResolveMult(SURVIVORSRUN_BUFF_STAT.ATTACK_SPEED);
        public float RangeMult => ResolveMult(SURVIVORSRUN_BUFF_STAT.RANGE);

        private void Awake()
        {
            _owner = GetComponent<SurvivorsRunUnitBase>();
        }

        private void Update()
        {
            if (_buffs.Count == 0)
                return;

            var scale = _owner != null && _owner.Manager != null ? _owner.Manager.TimeScale : 1f;
            if (scale <= 0f)
                return;

            var dt = Time.deltaTime * scale;
            for (var i = _buffs.Count - 1; i >= 0; i--)
            {
                var buff = _buffs[i];
                buff.Remaining -= dt;
                if (buff.Remaining <= 0f)
                    _buffs.RemoveAt(i);
                else
                    _buffs[i] = buff;
            }
        }

        public void ApplyBuff(SURVIVORSRUN_BUFF_STAT stat, float multiplier, float duration)
        {
            if (stat == SURVIVORSRUN_BUFF_STAT.NONE || multiplier <= 0f || duration <= 0f)
                return;

            _buffs.Add(new TimedBuff
            {
                Stat = stat,
                Multiplier = multiplier,
                Remaining = duration,
            });
        }

        public void ClearAll()
        {
            _buffs.Clear();
        }

        private float ResolveMult(SURVIVORSRUN_BUFF_STAT stat)
        {
            var mult = 1f;
            for (var i = 0; i < _buffs.Count; i++)
            {
                if (_buffs[i].Stat == stat)
                    mult *= _buffs[i].Multiplier;
            }

            return Mathf.Max(0.01f, mult);
        }
    }
}
