using System;
using System.Collections.Generic;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 공격 패턴 종류. 플레이어 Item / 몬스터 Loadout이 공통으로 참조한다.
    /// </summary>
    public enum SURVIVORSRUN_ATTACK_PATTERN
    {
        NONE = 0,
        /// <summary>기본 돌진·접촉. 로드아웃이 비었을 때 몬스터 기본값.</summary>
        CONTACT,
        ORBIT,
        PULSE,
        AURA,
        PROJECTILE,
    }

    /// <summary>
    /// 로드아웃에 들어가는 패턴 한 칸.
    /// (나중에 데미지·쿨·사거리 등 스탯을 여기 확장)
    /// </summary>
    [Serializable]
    public class SurvivorsRunPatternEntry
    {
        [SerializeField]
        private SURVIVORSRUN_ATTACK_PATTERN _pattern = SURVIVORSRUN_ATTACK_PATTERN.CONTACT;
        public SURVIVORSRUN_ATTACK_PATTERN Pattern => _pattern;
    }

    /// <summary>
    /// 몬스터가 스폰 시 들고 나오는 패턴 구성.
    /// ItemData(아이콘/드랍/UI)가 아니라, "이 적은 어떤 패턴으로 싸우는가"만 담는다.
    /// </summary>
    [Serializable]
    public class SurvivorsRunEnemyLoadout
    {
        [SerializeField]
        private List<SurvivorsRunPatternEntry> _patterns = new();

        public IReadOnlyList<SurvivorsRunPatternEntry> Patterns => _patterns;

        /// <summary>패턴이 없으면 돌진(Contact)만 하는 기본 몹.</summary>
        public bool IsEmpty => _patterns == null || _patterns.Count == 0;

        public bool HasPattern(SURVIVORSRUN_ATTACK_PATTERN pattern)
        {
            if (IsEmpty)
                return pattern == SURVIVORSRUN_ATTACK_PATTERN.CONTACT;

            for (var i = 0; i < _patterns.Count; i++)
            {
                var entry = _patterns[i];
                if (entry != null && entry.Pattern == pattern)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 실제 사용할 패턴 목록.
        /// 비어 있으면 Contact 하나만 반환한다.
        /// </summary>
        public void GetEffectivePatterns(List<SURVIVORSRUN_ATTACK_PATTERN> results)
        {
            results.Clear();

            if (IsEmpty)
            {
                results.Add(SURVIVORSRUN_ATTACK_PATTERN.CONTACT);
                return;
            }

            for (var i = 0; i < _patterns.Count; i++)
            {
                var entry = _patterns[i];
                if (entry == null || entry.Pattern == SURVIVORSRUN_ATTACK_PATTERN.NONE)
                    continue;

                results.Add(entry.Pattern);
            }

            if (results.Count == 0)
                results.Add(SURVIVORSRUN_ATTACK_PATTERN.CONTACT);
        }
    }
}
