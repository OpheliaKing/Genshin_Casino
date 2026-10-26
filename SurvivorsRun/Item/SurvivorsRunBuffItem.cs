using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 버프 전용 아이템. 발동 시 Effect 목록의 BUFF를 CombatMods에 적용한다.
    /// </summary>
    public class SurvivorsRunBuffItem : SurvivorsRunItemBase
    {
        private float _cooldownRemaining;
        private SurvivorsRunHitEffect _activeVfx;
        private int _vfxSpawnToken;

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

            // FixedLifetime으로 이미 꺼진 핸들 정리
            if (_activeVfx != null && !_activeVfx.IsPlaying)
                _activeVfx = null;
        }

        public override bool TryActivate()
        {
            if (!CanActivate)
                return false;

            var mods = EnsureCombatMods();
            if (mods == null)
                return false;

            var applied = 0;
            var maxDuration = 0f;
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

                var duration = Mathf.Max(0f, effect.BuffDuration);
                mods.ApplyBuff(
                    effect.BuffStat,
                    effect.ResolveBuffMultiplier(Stack),
                    duration);
                if (duration > maxDuration)
                    maxDuration = duration;
                applied++;
            }

            if (applied <= 0)
            {
                Debug.LogWarning($"[BuffItem] 적용할 버프 효과가 없습니다: {Tid}");
                return false;
            }

            // 스킬 쿨은 공속 버프와 무관하게 SO FireCooldown 그대로.
            _cooldownRemaining = ItemData != null ? Mathf.Max(0.1f, ItemData.FireCooldown) : 1f;
            PlayActivateVfx(maxDuration);
            return true;
        }

        public override float CooldownDuration =>
            ItemData != null ? Mathf.Max(0.1f, ItemData.FireCooldown) : 1f;

        public override void Dispose()
        {
            CancelActivateVfx();
            base.Dispose();
        }

        protected override void RebuildDamageObjects()
        {
            // 버프 아이템은 DamageObject 없음.
        }

        private void PlayActivateVfx(float duration)
        {
            if (ItemData == null || !ItemData.HasActivateEffect || Owner == null || duration <= 0f)
            {
                CancelActivateVfx();
                return;
            }

            // 이미 켜져 있으면 수명만 리셋 (오브젝트 재생성 X)
            if (_activeVfx != null && _activeVfx.IsPlaying)
            {
                _activeVfx.RefreshLifetime(duration);
                return;
            }

            var manager = Owner.Manager;
            if (manager == null)
                return;

            var address = ItemData.ActivateEffectPrefabPath.Trim();
            StopCurrentActivateVfx();
            var token = ++_vfxSpawnToken;
            PlayActivateVfxAsync(manager, address, duration, token);
        }

        private async void PlayActivateVfxAsync(
            SurvivorsRunManager manager,
            string address,
            float duration,
            int token)
        {
            var effect = await manager.PlayHitEffectAsync(
                address,
                Owner != null ? Owner.transform.position : Vector3.zero,
                duration,
                Owner != null ? Owner.transform : null);

            if (token != _vfxSpawnToken)
            {
                effect?.StopAndReturn();
                return;
            }

            if (effect == null || Owner == null || Owner.IsDead)
            {
                effect?.StopAndReturn();
                return;
            }

            _activeVfx = effect;
        }

        private void CancelActivateVfx()
        {
            _vfxSpawnToken++;
            StopCurrentActivateVfx();
        }

        private void StopCurrentActivateVfx()
        {
            if (_activeVfx == null)
                return;

            if (_activeVfx.IsPlaying)
                _activeVfx.StopAndReturn();

            _activeVfx = null;
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
