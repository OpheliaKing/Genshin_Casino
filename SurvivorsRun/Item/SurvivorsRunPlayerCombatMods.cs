using System.Collections.Generic;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 플레이어 전투 배율(공속·사거리·데미지).
    /// 기간제 버프 + 영구 패시브(소스 tid별)를 합산한다.
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
        private readonly Dictionary<string, Dictionary<SURVIVORSRUN_BUFF_STAT, float>> _permanents = new();
        private SurvivorsRunUnitBase _owner;

        public float AttackSpeedMult => ResolveMult(SURVIVORSRUN_BUFF_STAT.ATTACK_SPEED);
        public float RangeMult => ResolveMult(SURVIVORSRUN_BUFF_STAT.RANGE);
        public float DamageMult => ResolveMult(SURVIVORSRUN_BUFF_STAT.DAMAGE);

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

        /// <summary>
        /// 기간제 버프. 같은 Stat이 이미 있으면 덮어쓰고 지속시간을 다시 채운다.
        /// </summary>
        public void ApplyBuff(SURVIVORSRUN_BUFF_STAT stat, float multiplier, float duration)
        {
            if (stat == SURVIVORSRUN_BUFF_STAT.NONE || multiplier <= 0f || duration <= 0f)
                return;

            for (var i = 0; i < _buffs.Count; i++)
            {
                if (_buffs[i].Stat != stat)
                    continue;

                _buffs[i] = new TimedBuff
                {
                    Stat = stat,
                    Multiplier = multiplier,
                    Remaining = duration,
                };
                return;
            }

            _buffs.Add(new TimedBuff
            {
                Stat = stat,
                Multiplier = multiplier,
                Remaining = duration,
            });
        }

        /// <summary>
        /// 영구 패시브 한 소스(보통 아이템 tid)의 배율을 통째로 교체한다.
        /// 빈 목록이면 해당 소스 제거.
        /// </summary>
        public void SetPermanentBuffs(string sourceId, IReadOnlyList<(SURVIVORSRUN_BUFF_STAT stat, float multiplier)> buffs)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
                return;

            if (buffs == null || buffs.Count == 0)
            {
                _permanents.Remove(sourceId);
                return;
            }

            if (!_permanents.TryGetValue(sourceId, out var map) || map == null)
            {
                map = new Dictionary<SURVIVORSRUN_BUFF_STAT, float>();
                _permanents[sourceId] = map;
            }
            else
            {
                map.Clear();
            }

            for (var i = 0; i < buffs.Count; i++)
            {
                var (stat, multiplier) = buffs[i];
                if (stat == SURVIVORSRUN_BUFF_STAT.NONE || multiplier <= 0f)
                    continue;

                map[stat] = Mathf.Max(0.01f, multiplier);
            }

            if (map.Count == 0)
                _permanents.Remove(sourceId);
        }

        public void ClearPermanentBuffs(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
                return;

            _permanents.Remove(sourceId);
        }

        public void ClearAll()
        {
            _buffs.Clear();
            _permanents.Clear();
        }

        private float ResolveMult(SURVIVORSRUN_BUFF_STAT stat)
        {
            var mult = 1f;

            foreach (var pair in _permanents)
            {
                var map = pair.Value;
                if (map != null && map.TryGetValue(stat, out var permanent))
                    mult *= Mathf.Max(0.01f, permanent);
            }

            for (var i = 0; i < _buffs.Count; i++)
            {
                if (_buffs[i].Stat != stat)
                    continue;

                mult *= Mathf.Max(0.01f, _buffs[i].Multiplier);
                break;
            }

            return Mathf.Max(0.01f, mult);
        }
    }
}
