using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 버프 전용 아이템. 발동 시 Effect 목록의 BUFF를 CombatMods에 적용한다.
    /// </summary>
    public class SurvivorsRunBuffItem : SurvivorsRunItemBase
    {
        private float _cooldownRemaining;

        private bool IsManualActivation => ItemData != null && ItemData.IsInputTriggered;

        public override bool CanActivate =>
            IsManualActivation &&
            Owner != null &&
            !Owner.IsDead &&
            _cooldownRemaining <= 0f;

        public override float CooldownRemaining => Mathf.Max(0f, _cooldownRemaining);

        public override void Setup(SurvivorsRunItemData itemData, SurvivorsRunUnitBase owner, int stack = 1)
        {
            base.Setup(itemData, owner, stack);
            _cooldownRemaining = 0f;
        }

        public override void Tick(float dt)
        {
            if (!IsManualActivation || dt <= 0f)
                return;

            if (_cooldownRemaining > 0f)
                _cooldownRemaining -= dt;
        }

        public override bool TryActivate()
        {
            if (!CanActivate)
                return false;

            var mods = EnsureCombatMods();
            if (mods == null)
                return false;

            var applied = 0;
            var effects = ItemData != null ? ItemData.Effects : null;
            if (effects == null)
            {
                Debug.LogWarning($"[BuffItem] Effects가 없습니다: {Tid}");
                return false;
            }

            for (var i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (effect == null || !effect.IsBuff)
                    continue;

                mods.ApplyBuff(
                    effect.BuffStat,
                    effect.ResolveBuffMultiplier(Stack),
                    effect.BuffDuration);
                applied++;
            }

            if (applied <= 0)
            {
                Debug.LogWarning($"[BuffItem] 적용할 버프 효과가 없습니다: {Tid}");
                return false;
            }

            // 스킬 쿨은 공속 버프와 무관하게 SO FireCooldown 그대로.
            _cooldownRemaining = ItemData != null ? Mathf.Max(0.1f, ItemData.FireCooldown) : 1f;
            return true;
        }

        public override float CooldownDuration =>
            ItemData != null ? Mathf.Max(0.1f, ItemData.FireCooldown) : 1f;

        protected override void RebuildDamageObjects()
        {
            // 버프 아이템은 DamageObject 없음.
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
