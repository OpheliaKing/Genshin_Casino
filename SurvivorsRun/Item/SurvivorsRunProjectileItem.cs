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
        /// <summary>
        /// 멀티샷 탄 사이 월드 간격(조준 방향에 수직).
        /// 각도 스프레드는 먼 적에서 빗나가므로 평행 발사 + 위치 오프셋만 쓴다.
        /// </summary>
        private const float MultiShotLateralSpacing = 0.35f;

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

            var manager = Owner != null ? Owner.Manager : null;
            var poolKey = ResolvePoolKey();
            if (manager != null && !string.IsNullOrEmpty(poolKey))
                manager.PrewarmFromPrefab(poolKey, prefab, manager.PoolExpandBatch);
        }

        private bool TryFireProjectiles()
        {
            if (_projectilePrefab == null || Owner == null || Owner.IsDead)
                return false;

            var origin = Owner.transform.position;
            var target = ResolveFireTarget(origin, ResolveMaxRange());
            if (target == null)
                return false;

            var speed = ResolveProjectileSpeed();
            var aim = ResolveLeadAimDirection(origin, target, speed);
            if (aim.sqrMagnitude <= 0.0001f)
                return false;

            var count = ResolveObjectCount();
            var damage = ResolveDamage();
            var hitCooldown = ResolveHitCooldown();
            var lifetime = ResolveProjectileLifetime();
            var parent = ResolveSpawnParent();
            var manager = Owner.Manager;
            var poolKey = ResolvePoolKey();
            // 조준 방향에 수직 단위벡터 (2D). 멀티샷은 같은 방향으로 평행 비행.
            var lateral = new Vector2(-aim.y, aim.x);

            for (var i = 0; i < count; i++)
            {
                var spawnPos = (Vector2)origin;
                if (count > 1)
                {
                    var lane = i - (count - 1) * 0.5f;
                    spawnPos += lateral * (lane * MultiShotLateralSpacing);
                }

                GameObject instance;
                if (manager != null && !string.IsNullOrEmpty(poolKey))
                {
                    instance = manager.RentFromPrefab(
                        poolKey,
                        _projectilePrefab,
                        parent,
                        activate: false);
                }
                else
                {
                    instance = Object.Instantiate(
                        _projectilePrefab,
                        spawnPos,
                        Quaternion.identity,
                        parent);
                }

                if (instance == null)
                {
                    Debug.LogError(
                        $"[SurvivorsRunProjectileItem] 투사체 대여/생성 실패. tid={Tid}");
                    continue;
                }

                instance.transform.position = spawnPos;
                instance.transform.rotation = Quaternion.identity;
                instance.SetActive(true);

                var projectile = instance.GetComponent<SurvivorsRunProjectile>();
                if (projectile == null)
                {
                    Debug.LogError(
                        $"[SurvivorsRunProjectileItem] 인스턴스에 SurvivorsRunProjectile이 없습니다. tid={Tid}");
                    if (manager != null && !string.IsNullOrEmpty(poolKey))
                        manager.ReturnPooled(poolKey, instance);
                    else
                        Object.Destroy(instance);
                    continue;
                }

                projectile.Launch(
                    Owner,
                    damage,
                    hitCooldown,
                    aim,
                    speed,
                    lifetime,
                    poolKey,
                    ItemData != null ? ItemData.HitEffectPrefabPath : null,
                    ItemData != null ? ItemData.HitEffectLifetime : -1f);
            }

            return true;
        }

        private string ResolvePoolKey()
        {
            var path = ItemData != null ? ItemData.DamagePrefabPath : null;
            return string.IsNullOrWhiteSpace(path) ? null : path.Trim();
        }

        /// <summary>
        /// 적 행진을 고려한 선행 조준. 탄이 도착할 예상 지점으로 쏜다.
        /// (위쪽 적처럼 비행 시간이 길수록 현재 좌표만 조준하면 빗나감)
        /// </summary>
        private static Vector2 ResolveLeadAimDirection(
            Vector3 origin,
            SurvivorsRunUnitBase target,
            float projectileSpeed)
        {
            var origin2 = (Vector2)origin;
            var targetPos = (Vector2)target.transform.position;
            var toTarget = targetPos - origin2;
            if (toTarget.sqrMagnitude <= 0.0001f)
                return Vector2.right;

            var targetVel = Vector2.zero;
            if (target is SurvivorsRunEnemyBase enemy)
                targetVel = enemy.GetMarchVelocity();

            if (projectileSpeed <= 0.01f || targetVel.sqrMagnitude <= 0.0001f)
                return toTarget.normalized;

            if (!TrySolveInterceptTime(origin2, targetPos, targetVel, projectileSpeed, out var t))
                return toTarget.normalized;

            var intercept = targetPos + targetVel * t;
            var aim = intercept - origin2;
            return aim.sqrMagnitude > 0.0001f ? aim.normalized : toTarget.normalized;
        }

        /// <summary>
        /// |targetPos + vel*t - origin| = speed*t 의 최소 양수 t.
        /// </summary>
        private static bool TrySolveInterceptTime(
            Vector2 origin,
            Vector2 targetPos,
            Vector2 targetVel,
            float projectileSpeed,
            out float time)
        {
            time = 0f;
            var d = targetPos - origin;
            var speedSq = projectileSpeed * projectileSpeed;
            var a = Vector2.Dot(targetVel, targetVel) - speedSq;
            var b = 2f * Vector2.Dot(d, targetVel);
            var c = Vector2.Dot(d, d);

            const float maxLeadTime = 3f;

            if (Mathf.Abs(a) < 0.0001f)
            {
                if (Mathf.Abs(b) < 0.0001f)
                    return false;

                var linearT = -c / b;
                if (linearT <= 0f || linearT > maxLeadTime)
                    return false;

                time = linearT;
                return true;
            }

            var disc = b * b - 4f * a * c;
            if (disc < 0f)
                return false;

            var sqrt = Mathf.Sqrt(disc);
            var t1 = (-b - sqrt) / (2f * a);
            var t2 = (-b + sqrt) / (2f * a);

            var best = float.MaxValue;
            if (t1 > 0f && t1 <= maxLeadTime)
                best = t1;
            if (t2 > 0f && t2 <= maxLeadTime && t2 < best)
                best = t2;

            if (best >= float.MaxValue)
                return false;

            time = best;
            return true;
        }

        /// <summary>
        /// Manager 우선 타겟이 사거리 안이면 그걸 쓰고, 아니면 최근접.
        /// 우선 타겟이 사거리 밖이면 발사하지 않고 쿨도 안 깎는다(호출부 false).
        /// </summary>
        private SurvivorsRunUnitBase ResolveFireTarget(Vector3 origin, float maxRange)
        {
            var manager = Owner != null ? Owner.Manager : null;
            if (manager == null)
                return null;

            var locked = manager.AttackTarget;
            if (locked != null && !locked.IsDead)
            {
                var lockedSqr = (locked.transform.position - origin).sqrMagnitude;
                var maxRangeSqr = maxRange * maxRange;
                if (lockedSqr <= maxRangeSqr)
                    return locked;

                return null;
            }

            return FindNearestEnemyInRange(origin, maxRange);
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
