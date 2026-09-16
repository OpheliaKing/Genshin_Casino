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
            _stack = Mathf.Max(1, stack);
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

        protected int ResolveDamage()
        {
            if (_itemData != null && _itemData.BaseDamage > 0)
                return _itemData.BaseDamage;

            return _owner != null ? Mathf.Max(0, _owner.Attack) : 0;
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

        /// <summary>
        /// 프리팹 경로가 없으면 기본 트리거 원 콜라이더 + DamageObject를 만든다.
        /// </summary>
        protected SurvivorsRunDamageObject CreateDefaultDamageObject(Transform parent, float radius = 0.35f)
        {
            var go = new GameObject("DamageObject");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = radius;

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.simulated = true;
            body.useFullKinematicContacts = true;

            var damageObject = go.AddComponent<SurvivorsRunDamageObject>();
            damageObject.Setup(_owner, ResolveDamage(), ResolveHitCooldown());
            return damageObject;
        }
    }
}
