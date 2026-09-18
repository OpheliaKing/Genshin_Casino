using System.Threading.Tasks;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// Pulse 무기: FireCooldown마다 DamageObject를 활성화해 범위 피해를 준다.
    /// 판정은 먼저 끄고, 이펙트(프리팹)는 더 늦게 끈다.
    /// 중첩 시 루트 스케일로 범위가 넓어진다.
    /// </summary>
    public class SurvivorsRunPulseItem : SurvivorsRunItemBase
    {
        /// <summary>데미지 판정 유지 시간.</summary>
        private const float PulseHitDuration = 0.12f;
        /// <summary>이펙트(프리팹) 표시 시간. 판정보다 길게 둔다. (이후 VFX 재생 종료 연동 검토)</summary>
        private const float PulseVisualDuration = 2f;
        private const float BaseScale = 1f;

        private SurvivorsRunDamageObject _damageObject;
        private float _cooldownRemaining;
        private float _hitRemaining;
        private float _visualRemaining;
        private int _rebuildVersion;

        public float CurrentPulseScale => ResolvePulseScale();

        public override void Setup(SurvivorsRunItemData itemData, SurvivorsRunUnitBase owner, int stack = 1)
        {
            base.Setup(itemData, owner, stack);
            // 장착 직후 바로 한 번 나가게 둔다.
            _cooldownRemaining = 0f;
        }

        public override void Tick(float dt)
        {
            if (Owner == null || Owner.IsDead)
                return;

            if (dt <= 0f)
                return;

            if (_hitRemaining > 0f)
            {
                _hitRemaining -= dt;
                if (_hitRemaining <= 0f)
                    DisablePulseHit();
            }

            if (_visualRemaining > 0f)
            {
                _visualRemaining -= dt;
                if (_visualRemaining <= 0f)
                    HidePulseVisual();
            }

            if (_damageObject == null)
                return;

            _cooldownRemaining -= dt;
            if (_cooldownRemaining > 0f)
                return;

            FirePulse();
            _cooldownRemaining = ResolveFireCooldown();
        }

        public override void Dispose()
        {
            _rebuildVersion++;
            ClearDamageObject();
            base.Dispose();
        }

        protected override void RebuildDamageObjects()
        {
            _rebuildVersion++;
            _ = RebuildDamageObjectAsync(_rebuildVersion);
        }

        private async Task RebuildDamageObjectAsync(int version)
        {
            EnsureItemRoot();
            ClearDamageObject();

            if (version != _rebuildVersion)
                return;

            var path = ItemData != null ? ItemData.DamagePrefabPath : null;
            if (string.IsNullOrWhiteSpace(path))
            {
                Debug.LogError(
                    $"[SurvivorsRunPulseItem] DamagePrefabPath가 비어 있습니다. tid={Tid}");
                return;
            }

            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager == null)
            {
                Debug.LogError(
                    $"[SurvivorsRunPulseItem] ResourceManager가 없어 DamageObject를 생성할 수 없습니다. tid={Tid}");
                return;
            }

            var address = path.Trim();
            var prefab = await resourceManager.LoadAsync<GameObject>(address);
            if (version != _rebuildVersion)
                return;

            if (prefab == null)
            {
                Debug.LogError(
                    $"[SurvivorsRunPulseItem] DamageObject 프리팹 로드 실패. tid={Tid}, path={address}");
                return;
            }

            var instance = Object.Instantiate(prefab, ItemRoot, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * ResolvePulseScale();
            instance.SetActive(false);

            var damageObject = instance.GetComponent<SurvivorsRunDamageObject>();
            if (damageObject == null)
            {
                Debug.LogError(
                    $"[SurvivorsRunPulseItem] 프리팹에 SurvivorsRunDamageObject가 없습니다. tid={Tid}, path={address}");
                Object.Destroy(instance);
                return;
            }

            damageObject.Setup(Owner, ResolveDamage(), ResolveHitCooldown());
            damageObject.SetDamageEnabled(false);
            _damageObject = damageObject;
        }

        private void FirePulse()
        {
            if (_damageObject == null || Owner == null || Owner.IsDead)
                return;

            ApplyPulseScale();
            _damageObject.Setup(Owner, ResolveDamage(), ResolveHitCooldown());
            _damageObject.gameObject.SetActive(true);
            _damageObject.SetDamageEnabled(true);
            _hitRemaining = PulseHitDuration;
            _visualRemaining = PulseVisualDuration;
        }

        /// <summary>판정만 끈다. 이펙트는 유지.</summary>
        private void DisablePulseHit()
        {
            _hitRemaining = 0f;
            if (_damageObject == null)
                return;

            _damageObject.SetDamageEnabled(false);
        }

        /// <summary>이펙트(프리팹)를 끈다.</summary>
        private void HidePulseVisual()
        {
            _visualRemaining = 0f;
            DisablePulseHit();
            if (_damageObject == null)
                return;

            _damageObject.gameObject.SetActive(false);
        }

        private void DeactivatePulse()
        {
            HidePulseVisual();
        }

        private void ApplyPulseScale()
        {
            if (_damageObject == null)
                return;

            var scale = ResolvePulseScale();
            _damageObject.transform.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>중첩 1 → Base, 2 → Base×2 … (스케일로 범위 확대).</summary>
        private float ResolvePulseScale()
        {
            return BaseScale * Stack;
        }

        private void ClearDamageObject()
        {
            DeactivatePulse();

            if (_damageObject != null)
            {
                Object.Destroy(_damageObject.gameObject);
                _damageObject = null;
            }

            if (ItemRoot == null)
                return;

            for (var i = ItemRoot.childCount - 1; i >= 0; i--)
                Object.Destroy(ItemRoot.GetChild(i).gameObject);
        }
    }
}
