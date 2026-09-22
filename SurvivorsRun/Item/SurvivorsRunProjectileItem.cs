using System.Threading.Tasks;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// Projectile 무기: FireCooldown마다 사거리 안 최근접 적 방향으로 직선 투사체를 발사한다.
    /// 중첩 시 한 번에 나가는 탄 수가 늘어난다.
    /// </summary>
    public class SurvivorsRunProjectileItem : SurvivorsRunItemBase
    {
        private const float DefaultProjectileSpeed = 10f;
        private const float DefaultLifetimeLifetime = 2.5f;
        private const float MultiShotSpreadDegrees = 12f;
        /// <summary>조준 사거리. 이 밖 적은 후보에서 제외.</summary>
        private const float DefaultMaxRange = 12f;

        private GameObject _projectilePrefab;
        private float _cooldownRemaining;
        private int _rebuildVersion;

        public override void Setup(SurvivorsRunItemData itemData, SurvivorsRunUnitBase owner, int stack = 1)
        {
            base.Setup(itemData, owner, stack);
            _cooldownRemaining = 0f;
        }

        public override void Tick(float dt)
        {
            if (Owner == null || Owner.IsDead)
                return;

            if (dt <= 0f || _projectilePrefab == null)
                return;

            _cooldownRemaining -= dt;
            if (_cooldownRemaining > 0f)
                return;

            // 사거리 안 타겟이 없으면 쿨을 소모하지 않고 다음 프레임에 재시도.
            if (!TryFireProjectiles())
                return;

            _cooldownRemaining = ResolveFireCooldown();
        }

        public override void Dispose()
        {
            _rebuildVersion++;
            _projectilePrefab = null;
            base.Dispose();
        }

        protected override void RebuildDamageObjects()
        {
            _rebuildVersion++;
            _ = LoadPrefabAsync(_rebuildVersion);
        }

        private async Task LoadPrefabAsync(int version)
        {
            EnsureItemRoot();

            var path = ItemData != null ? ItemData.DamagePrefabPath : null;
            if (string.IsNullOrWhiteSpace(path))
            {
                Debug.LogError(
                    $"[SurvivorsRunProjectileItem] DamagePrefabPath가 비어 있습니다. tid={Tid}");
                return;
            }

            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager == null)
            {
                Debug.LogError(
                    $"[SurvivorsRunProjectileItem] ResourceManager가 없어 투사체를 로드할 수 없습니다. tid={Tid}");
                return;
            }

            var address = path.Trim();
            var prefab = await resourceManager.LoadAsync<GameObject>(address);
            if (version != _rebuildVersion)
                return;

            if (prefab == null)
            {
                Debug.LogError(
                    $"[SurvivorsRunProjectileItem] 투사체 프리팹 로드 실패. tid={Tid}, path={address}");
                return;
            }

            if (prefab.GetComponent<SurvivorsRunProjectile>() == null)
            {
                Debug.LogError(
                    $"[SurvivorsRunProjectileItem] 프리팹에 SurvivorsRunProjectile이 없습니다. tid={Tid}, path={address}");
                return;
            }

            _projectilePrefab = prefab;
        }

        private bool TryFireProjectiles()
        {
            if (_projectilePrefab == null || Owner == null || Owner.IsDead)
                return false;

            var origin = Owner.transform.position;
            var target = FindNearestEnemyInRange(origin, DefaultMaxRange);
            if (target == null)
                return false;

            var delta = (Vector2)(target.transform.position - origin);
            if (delta.sqrMagnitude <= 0.0001f)
                return false;

            var aim = delta.normalized;
            var count = ResolveObjectCount();
            var damage = ResolveDamage();
            var hitCooldown = ResolveHitCooldown();
            var parent = ResolveSpawnParent();

            for (var i = 0; i < count; i++)
            {
                var direction = aim;
                if (count > 1)
                {
                    var offset = (i - (count - 1) * 0.5f) * MultiShotSpreadDegrees;
                    var rotated = Quaternion.Euler(0f, 0f, offset) * new Vector3(aim.x, aim.y, 0f);
                    direction = new Vector2(rotated.x, rotated.y);
                }

                var instance = Object.Instantiate(_projectilePrefab, origin, Quaternion.identity, parent);
                instance.SetActive(true);

                var projectile = instance.GetComponent<SurvivorsRunProjectile>();
                if (projectile == null)
                {
                    Debug.LogError(
                        $"[SurvivorsRunProjectileItem] 인스턴스에 SurvivorsRunProjectile이 없습니다. tid={Tid}");
                    Object.Destroy(instance);
                    continue;
                }

                projectile.Launch(
                    Owner,
                    damage,
                    hitCooldown,
                    direction,
                    DefaultProjectileSpeed,
                    DefaultLifetimeLifetime);
            }

            return true;
        }

        private SurvivorsRunUnitBase FindNearestEnemyInRange(Vector3 origin, float maxRange)
        {
            var manager = Owner != null ? Owner.Manager : null;
            if (manager == null)
                return null;

            var maxRangeSqr = maxRange * maxRange;
            SurvivorsRunUnitBase nearest = null;
            var bestSqr = float.MaxValue;
            var enemies = manager.ActiveEnemies;
            for (var i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || enemy.IsDead)
                    continue;

                if (enemy.UnitType != SURVIVORSRUN_UNIT_TYPE.ENEMY)
                    continue;

                var sqr = (enemy.transform.position - origin).sqrMagnitude;
                if (sqr > maxRangeSqr || sqr >= bestSqr)
                    continue;

                bestSqr = sqr;
                nearest = enemy;
            }

            return nearest;
        }

        private Transform ResolveSpawnParent()
        {
            if (Owner != null && Owner.Manager != null)
                return Owner.Manager.transform;

            return null;
        }
    }
}
