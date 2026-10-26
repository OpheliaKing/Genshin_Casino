using System.Collections.Generic;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 영구 패시브. 획득·중첩 시 CombatMods에 상시 배율을 올린다.
    /// Effects의 BuffDuration은 무시한다(기간제 아님).
    /// </summary>
    public class SurvivorsRunPassiveItem : SurvivorsRunItemBase
    {
        private readonly List<(SURVIVORSRUN_BUFF_STAT stat, float multiplier)> _buffer = new();

        public override void Setup(SurvivorsRunItemData itemData, SurvivorsRunUnitBase owner, int stack = 1)
        {
            base.Setup(itemData, owner, stack);
            ApplyPermanentMods();
        }

        public override void Dispose()
        {
            ClearPermanentMods();
            base.Dispose();
        }

        protected override void OnStackChanged(int stack)
        {
            ApplyPermanentMods();
        }

        protected override void RebuildDamageObjects()
        {
            // 패시브는 DamageObject 없음.
        }

        private void ApplyPermanentMods()
        {
            var mods = EnsureCombatMods();
            if (mods == null || string.IsNullOrEmpty(Tid))
                return;

            _buffer.Clear();
            var effects = ItemData != null ? ItemData.Effects : null;
            if (effects != null)
            {
                for (var i = 0; i < effects.Count; i++)
                {
                    var effect = effects[i];
                    if (effect == null || !effect.HasBuffStat)
                        continue;

                    _buffer.Add((effect.BuffStat, effect.ResolveBuffMultiplier(Stack)));
                }
            }

            if (_buffer.Count == 0)
            {
                Debug.LogWarning($"[PassiveItem] 적용할 버프 효과가 없습니다: {Tid}");
                mods.ClearPermanentBuffs(Tid);
                return;
            }

            mods.SetPermanentBuffs(Tid, _buffer);
        }

        private void ClearPermanentMods()
        {
            if (Owner == null || string.IsNullOrEmpty(Tid))
                return;

            var mods = Owner.GetComponent<SurvivorsRunPlayerCombatMods>();
            mods?.ClearPermanentBuffs(Tid);
        }

        private SurvivorsRunPlayerCombatMods EnsureCombatMods()
        {
            if (Owner == null)
                return null;

            var mods = Owner.GetComponent<SurvivorsRunPlayerCombatMods>();
            if (mods == null)
                mods = Owner.gameObject.AddComponent<SurvivorsRunPlayerCombatMods>();
            return mods;
        }
    }
}
