using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// Orbit 무기: 회전 피벗 아래 DamageObject를 중첩 수만큼 배치한다.
    /// </summary>
    public class SurvivorsRunOrbitItem : SurvivorsRunItemBase
    {
        private const float DefaultOrbitRadius = 1.25f;
        private const float DefaultOrbitSpeed = 180f;

        private Transform _orbitPivot;
        private readonly List<SurvivorsRunDamageObject> _damageObjects = new();
        private float _orbitRadius = DefaultOrbitRadius;
        private float _orbitSpeedDegrees = DefaultOrbitSpeed;
        private int _rebuildVersion;

        public override void Tick(float dt)
        {
            if (_orbitPivot == null)
                return;

            _orbitPivot.Rotate(0f, 0f, -_orbitSpeedDegrees * dt, Space.Self);
        }

        public override void Dispose()
        {
            _rebuildVersion++;
            ClearDamageObjects();
            _orbitPivot = null;
            base.Dispose();
        }

        protected override void RebuildDamageObjects()
        {
            _rebuildVersion++;
            _ = RebuildDamageObjectsAsync(_rebuildVersion);
        }

        private async Task RebuildDamageObjectsAsync(int version)
        {
            EnsureItemRoot();
            EnsureOrbitPivot();
            ClearDamageObjects();

            if (version != _rebuildVersion)
                return;

            var path = ItemData != null ? ItemData.DamagePrefabPath : null;
            if (string.IsNullOrWhiteSpace(path))
            {
                Debug.LogError(
                    $"[SurvivorsRunOrbitItem] DamagePrefabPath가 비어 있습니다. tid={Tid}");
                return;
            }

            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager == null)
            {
                Debug.LogError(
                    $"[SurvivorsRunOrbitItem] ResourceManager가 없어 DamageObject를 생성할 수 없습니다. tid={Tid}");
                return;
            }

            var address = path.Trim();
            var prefab = await resourceManager.LoadAsync<GameObject>(address);
            if (version != _rebuildVersion)
                return;

            if (prefab == null)
            {
                Debug.LogError(
                    $"[SurvivorsRunOrbitItem] DamageObject 프리팹 로드 실패. tid={Tid}, path={address}");
                return;
            }

            var count = ResolveObjectCount();
            var damage = ResolveDamage();
            var hitCooldown = ResolveHitCooldown();

            for (var i = 0; i < count; i++)
            {
                if (version != _rebuildVersion)
                {
                    ClearDamageObjects();
                    return;
                }

                var angle = (360f / count) * i;
                var rad = angle * Mathf.Deg2Rad;
                var local = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * _orbitRadius;

                var instance = Object.Instantiate(prefab, _orbitPivot, false);
                instance.transform.localPosition = local;
                instance.transform.localRotation = Quaternion.identity;
                instance.SetActive(true);

                var damageObject = instance.GetComponent<SurvivorsRunDamageObject>();
                if (damageObject == null)
                {
                    Debug.LogError(
                        $"[SurvivorsRunOrbitItem] 프리팹에 SurvivorsRunDamageObject가 없습니다. tid={Tid}, path={address}");
                    Object.Destroy(instance);
                    continue;
                }

                damageObject.Setup(Owner, damage, hitCooldown);
                _damageObjects.Add(damageObject);
            }
        }

        private void EnsureOrbitPivot()
        {
            if (_orbitPivot != null)
                return;

            var go = new GameObject("OrbitPivot");
            go.transform.SetParent(ItemRoot, false);
            go.transform.localPosition = Vector3.zero;
            _orbitPivot = go.transform;
        }

        private void ClearDamageObjects()
        {
            for (var i = 0; i < _damageObjects.Count; i++)
            {
                var damageObject = _damageObjects[i];
                if (damageObject == null)
                    continue;

                Object.Destroy(damageObject.gameObject);
            }

            _damageObjects.Clear();

            if (_orbitPivot == null)
                return;

            for (var i = _orbitPivot.childCount - 1; i >= 0; i--)
                Object.Destroy(_orbitPivot.GetChild(i).gameObject);
        }
    }
}
