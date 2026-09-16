using System.Collections.Generic;
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

        public override void Tick(float dt)
        {
            if (_orbitPivot == null)
                return;

            _orbitPivot.Rotate(0f, 0f, -_orbitSpeedDegrees * dt, Space.Self);
        }

        public override void Dispose()
        {
            _damageObjects.Clear();
            _orbitPivot = null;
            base.Dispose();
        }

        protected override void RebuildDamageObjects()
        {
            EnsureItemRoot();
            EnsureOrbitPivot();
            ClearDamageObjects();

            var count = ResolveObjectCount();
            var damage = ResolveDamage();
            var hitCooldown = ResolveHitCooldown();

            for (var i = 0; i < count; i++)
            {
                var angle = (360f / count) * i;
                var rad = angle * Mathf.Deg2Rad;
                var local = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * _orbitRadius;

                var damageObject = CreateDefaultDamageObject(_orbitPivot, radius: 0.4f);
                damageObject.transform.localPosition = local;
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
                if (damageObject != null)
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
