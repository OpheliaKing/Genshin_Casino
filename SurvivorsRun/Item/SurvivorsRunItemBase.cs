using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 플레이어 장착 아이템 런타임(비 MonoBehaviour).
    /// 정보·중첩·스폰을 담당하고, 실제 타격은 <see cref="SurvivorsRunDamageObject"/>가 한다.
    /// </summary>
    public abstract class SurvivorsRunItemBase
    {
        private string _tid;
        private int _stack = 1;
        private SurvivorsRunItemData _itemData;
        private SurvivorsRunUnitBase _owner;
        private Transform _itemRoot;

        public string Tid => _tid;
        public int Stack => _stack;
        public SurvivorsRunItemData ItemData => _itemData;
        public SurvivorsRunUnitBase Owner => _owner;
        public SURVIVORSRUN_ATTACK_PATTERN Pattern =>
            _itemData != null ? _itemData.AttackPattern : SURVIVORSRUN_ATTACK_PATTERN.NONE;

        protected Transform ItemRoot => _itemRoot;

        public virtual void Setup(SurvivorsRunItemData itemData, SurvivorsRunUnitBase owner, int stack = 1)
        {
            _itemData = itemData;
            _tid = itemData != null ? itemData.Tid : null;
            _owner = owner;
            EnsureItemRoot();
            SetStack(stack);
        }

        public void SetStack(int stack)
        {
            stack = Mathf.Max(1, stack);
            if (_itemData != null)
                stack = Mathf.Min(stack, _itemData.MaxStack);

            _stack = stack;
            OnStackChanged(_stack);
        }

        public void AddStack(int amount = 1)
        {
            if (amount == 0)
                return;

            SetStack(_stack + amount);
        }

        /// <summary>컨트롤러 Update에서 호출.</summary>
        public virtual void Tick(float dt)
        {
        }

        /// <summary>ACTIVE / UNIQUE 수동 발동용. 무기(자동)는 false.</summary>
        public virtual bool CanActivate => false;

        /// <summary>ACTIVE / UNIQUE 수동 발동. 성공 시 true.</summary>
        public virtual bool TryActivate() => false;

        /// <summary>수동 발동 쿨 남은 시간(초). 자동 무기는 0.</summary>
        public virtual float CooldownRemaining => 0f;

        /// <summary>수동 발동 쿨 전체(초).</summary>
        public virtual float CooldownDuration => ResolveFireCooldown();

        /// <summary>장착 해제·플레이어 제거 시 생성물 정리.</summary>
        public virtual void Dispose()
        {
            if (_itemRoot == null)
                return;

            Object.Destroy(_itemRoot.gameObject);
            _itemRoot = null;
        }

        protected virtual void OnStackChanged(int stack)
        {
            RebuildDamageObjects();
        }

        /// <summary>중첩·데이터에 맞춰 DamageObject 구성을 다시 만든다.</summary>
        protected abstract void RebuildDamageObjects();

        protected void EnsureItemRoot()
        {
            if (_itemRoot != null || _owner == null)
                return;

            var go = new GameObject($"Item_{_tid ?? "unknown"}");
            go.transform.SetParent(_owner.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            _itemRoot = go.transform;
        }

        /// <summary>
        /// 아이템 기본 피해(배율 미포함). 최종 피해는 DamageObject가 CombatMods.DamageMult를 곱한다.
        /// </summary>
        protected int ResolveDamage()
        {
            if (_itemData != null && _itemData.BaseDamage > 0)
                return _itemData.BaseDamage;

            return _owner != null ? Mathf.Max(0, _owner.Attack) : 0;
        }

        /// <summary>Pulse·Projectile 발동 주기. HitCooldown(재타격 면역)과 별개. 공속 버프 반영.</summary>
        protected float ResolveFireCooldown()
        {
            var baseCd = _itemData != null ? Mathf.Max(0f, _itemData.FireCooldown) : 1f;
            if (baseCd <= 0f)
                return 0f;

            var speedMult = ResolveOwnerAttackSpeedMult();
            return baseCd / speedMult;
        }

        protected float ResolveHitCooldown()
        {
            return _itemData != null ? Mathf.Max(0f, _itemData.HitCooldown) : 0.5f;
        }

        protected int ResolveObjectCount()
        {
            var baseCount = _itemData != null ? Mathf.Max(1, _itemData.BaseObjectCount) : 1;
            return baseCount * _stack;
        }

        /// <summary>Projectile 탄속. SO 값이 없으면 기본 10.</summary>
        protected float ResolveProjectileSpeed()
        {
            if (_itemData != null && _itemData.ProjectileSpeed > 0f)
                return _itemData.ProjectileSpeed;
            return 10f;
        }

        /// <summary>Projectile 수명(초). SO 값이 없으면 기본 2.5.</summary>
        protected float ResolveProjectileLifetime()
        {
            if (_itemData != null && _itemData.ProjectileLifetime > 0f)
                return _itemData.ProjectileLifetime;
            return 2.5f;
        }

        /// <summary>Projectile 조준 사거리. 사거리 버프 반영.</summary>
        protected float ResolveMaxRange()
        {
            float baseRange;
            if (_itemData != null && _itemData.MaxRange > 0f)
                baseRange = _itemData.MaxRange;
            else
                baseRange = 12f;

            return baseRange * ResolveOwnerRangeMult();
        }

        private float ResolveOwnerAttackSpeedMult()
        {
            if (_owner == null)
                return 1f;

            var mods = _owner.GetComponent<SurvivorsRunPlayerCombatMods>();
            return mods != null ? mods.AttackSpeedMult : 1f;
        }

        private float ResolveOwnerRangeMult()
        {
            if (_owner == null)
                return 1f;

            var mods = _owner.GetComponent<SurvivorsRunPlayerCombatMods>();
            return mods != null ? mods.RangeMult : 1f;
        }

        /// <summary>ItemData 히트 이펙트를 DamageObject에 주입.</summary>
        protected void ApplyHitEffect(SurvivorsRunDamageObject damageObject)
        {
            if (damageObject == null)
                return;

            if (_itemData == null || !_itemData.HasHitEffect)
            {
                damageObject.SetHitEffect(null);
                return;
            }

            damageObject.SetHitEffect(_itemData.HitEffectPrefabPath, _itemData.HitEffectLifetime);
        }

        /// <summary>
        /// DamagePrefabPath 없이 임시 검증용 히트박스를 만든다.
        /// Orbit 등 실전 무기는 프리팹 경로를 쓰고, 경로가 없으면 에러 로그 후 생성하지 않는다.
        /// 생성 오브젝트는 Default 레이어를 유지한다(Unit=피격 전용).
        /// Rigidbody2D·CircleCollider2D는 <see cref="SurvivorsRunDamageObject"/> 추가 시 자동 부착된다.
        /// </summary>
        protected SurvivorsRunDamageObject CreateDefaultDamageObject(Transform parent, float radius = 0.35f)
        {
            var go = new GameObject("DamageObject");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            // Unit 레이어에 두면 다른 DamageObject의 피격 대상이 되므로 Default 유지.
            go.layer = 0;

            var damageObject = go.AddComponent<SurvivorsRunDamageObject>();
            var col = go.GetComponent<CircleCollider2D>();
            if (col != null)
                col.radius = radius;

            damageObject.Setup(_owner, ResolveDamage(), ResolveHitCooldown());
            ApplyHitEffect(damageObject);
            return damageObject;
        }
    }
}
