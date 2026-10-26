using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 투사체 런타임. DamageObject + StraightMover를 묶고, 수명·명중 시 풀 반환(또는 Destroy)을 처리한다.
    /// 패시브 PROJECTILE_CHAIN이 있으면 명중 지점에서 주변 적으로 튕긴다.
    /// </summary>
    [RequireComponent(typeof(SurvivorsRunDamageObject))]
    [RequireComponent(typeof(SurvivorsRunProjectileStraightMover))]
    public class SurvivorsRunProjectile : MonoBehaviour
    {
        private const float DefaultChainSearchRange = 8f;

        [SerializeField] private float _lifetime = 2.5f;
        [SerializeField] private bool _destroyOnHit = true;

        private SurvivorsRunDamageObject _damageObject;
        private SurvivorsRunProjectileStraightMover _mover;
        private string _poolKey;
        private GameObject _sourcePrefab;
        private float _launchSpeed;
        private float _launchLifetime;
        private string _hitEffectPrefabPath;
        private float _hitEffectLifetime = -1f;
        private int _chainRemaining;
        private float _lifeRemaining;
        private bool _launched;
        private bool _destroying;

        private void Awake()
        {
            _damageObject = GetComponent<SurvivorsRunDamageObject>();
            _mover = GetComponent<SurvivorsRunProjectileStraightMover>();
        }

        private void OnEnable()
        {
            if (_damageObject != null)
                _damageObject.DamageApplied += OnDamageApplied;
        }

        private void OnDisable()
        {
            if (_damageObject != null)
                _damageObject.DamageApplied -= OnDamageApplied;
        }

        public void Launch(
            SurvivorsRunUnitBase owner,
            int damage,
            float hitCooldown,
            Vector2 direction,
            float speed,
            float lifetime = -1f,
            string poolKey = null,
            string hitEffectPrefabPath = null,
            float hitEffectLifetime = -1f,
            GameObject sourcePrefab = null,
            int chainRemainingOverride = -1,
            SurvivorsRunUnitBase ignoreUnit = null)
        {
            if (_damageObject == null)
                _damageObject = GetComponent<SurvivorsRunDamageObject>();
            if (_mover == null)
                _mover = GetComponent<SurvivorsRunProjectileStraightMover>();

            _poolKey = poolKey;
            _sourcePrefab = sourcePrefab;
            _launchSpeed = Mathf.Max(0.01f, speed);
            _launchLifetime = lifetime > 0f ? lifetime : _lifetime;
            _hitEffectPrefabPath = hitEffectPrefabPath;
            _hitEffectLifetime = hitEffectLifetime;
            _destroying = false;
            _lifeRemaining = _launchLifetime;
            _mover.BindTimeOwner(owner);
            _damageObject.Setup(owner, damage, hitCooldown);
            _damageObject.SetHitEffect(hitEffectPrefabPath, hitEffectLifetime);
            _damageObject.SetDamageEnabled(true);

            if (ignoreUnit != null)
                _damageObject.MarkRecentlyHit(ignoreUnit, 10f);

            if (chainRemainingOverride >= 0)
                _chainRemaining = chainRemainingOverride;
            else
            {
                var mods = owner != null ? owner.GetComponent<SurvivorsRunPlayerCombatMods>() : null;
                _chainRemaining = mods != null ? mods.ProjectileChainBounces : 0;
            }

            _mover.Launch(direction, speed);
            _launched = true;
        }

        private void Update()
        {
            if (!_launched || _destroying)
                return;

            var scale = _damageObject != null && _damageObject.Owner != null && _damageObject.Owner.Manager != null
                ? _damageObject.Owner.Manager.TimeScale
                : 1f;
            if (scale <= 0f)
                return;

            _lifeRemaining -= Time.deltaTime * scale;
            if (_lifeRemaining <= 0f)
                Despawn();
        }

        private void OnDamageApplied(SurvivorsRunUnitBase target, Vector3 hitPosition)
        {
            if (_destroying)
                return;

            TrySpawnChain(target, hitPosition);

            if (_destroyOnHit)
                Despawn();
        }

        private void TrySpawnChain(SurvivorsRunUnitBase hitTarget, Vector3 hitPosition)
        {
            if (_chainRemaining <= 0 || hitTarget == null || _sourcePrefab == null)
                return;

            var owner = _damageObject != null ? _damageObject.Owner : null;
            if (owner == null || owner.IsDead || owner.Manager == null)
                return;

            // 적 중심이 아니라 실제 히트 좌표에서 튕긴다.
            var origin = hitPosition;
            var next = FindNearestEnemy(owner.Manager, origin, hitTarget, DefaultChainSearchRange);
            if (next == null)
                return;

            var dir = (Vector2)(next.transform.position - origin);
            if (dir.sqrMagnitude <= 0.0001f)
                return;
            dir.Normalize();

            var spawnPos = (Vector2)origin;
            var manager = owner.Manager;
            GameObject instance;
            if (!string.IsNullOrEmpty(_poolKey))
            {
                instance = manager.RentFromPrefab(
                    _poolKey,
                    _sourcePrefab,
                    manager.transform,
                    activate: false);
            }
            else
            {
                instance = Object.Instantiate(
                    _sourcePrefab,
                    spawnPos,
                    Quaternion.identity,
                    manager.transform);
            }

            if (instance == null)
                return;

            instance.transform.position = spawnPos;
            instance.transform.rotation = Quaternion.identity;
            instance.SetActive(true);

            var projectile = instance.GetComponent<SurvivorsRunProjectile>();
            if (projectile == null)
            {
                if (!string.IsNullOrEmpty(_poolKey))
                    manager.ReturnPooled(_poolKey, instance);
                else
                    Object.Destroy(instance);
                return;
            }

            projectile.Launch(
                owner,
                _damageObject.Damage,
                _damageObject.HitCooldown,
                dir,
                _launchSpeed,
                _launchLifetime,
                _poolKey,
                _hitEffectPrefabPath,
                _hitEffectLifetime,
                _sourcePrefab,
                _chainRemaining - 1,
                hitTarget);
        }

        private static SurvivorsRunUnitBase FindNearestEnemy(
            SurvivorsRunManager manager,
            Vector3 origin,
            SurvivorsRunUnitBase exclude,
            float range)
        {
            if (manager == null)
                return null;

            var maxSqr = range * range;
            SurvivorsRunUnitBase nearest = null;
            var bestSqr = float.MaxValue;
            var enemies = manager.ActiveEnemies;
            for (var i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || enemy.IsDead || enemy == exclude)
                    continue;

                if (enemy.UnitType != SURVIVORSRUN_UNIT_TYPE.ENEMY)
                    continue;

                var sqr = (enemy.transform.position - origin).sqrMagnitude;
                if (sqr > maxSqr || sqr >= bestSqr)
                    continue;

                bestSqr = sqr;
                nearest = enemy;
            }

            return nearest;
        }

        private void Despawn()
        {
            if (_destroying)
                return;

            _destroying = true;
            _launched = false;
            if (_mover != null)
                _mover.Stop();
            if (_damageObject != null)
                _damageObject.SetDamageEnabled(false);

            var manager = _damageObject != null ? _damageObject.Owner?.Manager : null;
            if (!string.IsNullOrEmpty(_poolKey) && manager != null)
            {
                manager.ReturnPooled(_poolKey, gameObject);
                _poolKey = null;
                return;
            }

            if (manager != null && manager.TryReturnPooled(gameObject))
            {
                _poolKey = null;
                return;
            }

            Destroy(gameObject);
        }
    }
}
