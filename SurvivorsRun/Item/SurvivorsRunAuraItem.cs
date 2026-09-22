using System.Threading.Tasks;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// Aura 무기: 플레이어 주변에 DamageObject를 상시 유지해 지속 피해를 준다.
    /// FireCooldown은 사용하지 않으며, 재타격 간격은 HitCooldown으로만 제한한다.
    /// 중첩 시 루트 스케일로 범위가 넓어진다.
    /// </summary>
    public class SurvivorsRunAuraItem : SurvivorsRunItemBase
    {
        private const float BaseScale = 1f;

        private SurvivorsRunDamageObject _damageObject;
        private int _rebuildVersion;

        public float CurrentAuraScale => ResolveAuraScale();

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
                    $"[SurvivorsRunAuraItem] DamagePrefabPath가 비어 있습니다. tid={Tid}");
                return;
            }

            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager == null)
            {
                Debug.LogError(
                    $"[SurvivorsRunAuraItem] ResourceManager가 없어 DamageObject를 생성할 수 없습니다. tid={Tid}");
                return;
            }

            var address = path.Trim();
            var prefab = await resourceManager.LoadAsync<GameObject>(address);
            if (version != _rebuildVersion)
                return;

            if (prefab == null)
            {
                Debug.LogError(
                    $"[SurvivorsRunAuraItem] DamageObject 프리팹 로드 실패. tid={Tid}, path={address}");
                return;
            }

            var instance = Object.Instantiate(prefab, ItemRoot, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * ResolveAuraScale();
            instance.SetActive(true);

            var damageObject = instance.GetComponent<SurvivorsRunDamageObject>();
            if (damageObject == null)
            {
                Debug.LogError(
                    $"[SurvivorsRunAuraItem] 프리팹에 SurvivorsRunDamageObject가 없습니다. tid={Tid}, path={address}");
                Object.Destroy(instance);
                return;
            }

            damageObject.Setup(Owner, ResolveDamage(), ResolveHitCooldown());
            damageObject.SetDamageEnabled(true);
            _damageObject = damageObject;
        }

        /// <summary>중첩 1 → Base, 2 → Base×2 … (스케일로 범위 확대).</summary>
        private float ResolveAuraScale()
        {
            return BaseScale * Stack;
        }

        private void ClearDamageObject()
        {
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
