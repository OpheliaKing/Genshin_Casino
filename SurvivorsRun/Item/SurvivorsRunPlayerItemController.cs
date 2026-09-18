using System.Collections.Generic;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 플레이어 아이템 목록 컨트롤러. 컴포넌트 하나에서 여러 아이템을 Tick한다.
    /// </summary>
    [RequireComponent(typeof(SurvivorsRunUnitBase))]
    public class SurvivorsRunPlayerItemController : MonoBehaviour
    {
        private SurvivorsRunUnitBase _owner;
        private readonly List<SurvivorsRunItemBase> _items = new();

        public IReadOnlyList<SurvivorsRunItemBase> Items => _items;

        private void Awake()
        {
            _owner = GetComponent<SurvivorsRunUnitBase>();
        }

        private void Update()
        {
            if (_owner == null || _owner.IsDead)
                return;

            var scale = _owner.Manager != null ? _owner.Manager.TimeScale : 1f;
            if (scale <= 0f)
                return;

            var dt = Time.deltaTime * scale;
            for (var i = 0; i < _items.Count; i++)
                _items[i]?.Tick(dt);
        }

        private void OnDestroy()
        {
            ClearAll();
        }

        public void BindOwner(SurvivorsRunUnitBase owner)
        {
            _owner = owner != null ? owner : GetComponent<SurvivorsRunUnitBase>();
        }

        /// <summary>
        /// 같은 tid면 중첩만 올리고, 없으면 새 아이템을 추가한다.
        /// </summary>
        public SurvivorsRunItemBase AddOrStackItem(SurvivorsRunItemData itemData, int stack = 1)
        {
            if (itemData == null || _owner == null)
                return null;

            if (!itemData.HasAttackPattern && itemData.ItemType == SURVIVORSRUN_ITEM_TYPE.WEAPON)
            {
                Debug.LogWarning($"[PlayerItem] 무기 패턴이 없습니다: {itemData.Tid}", this);
                return null;
            }

            var existing = FindByTid(itemData.Tid);
            if (existing != null)
            {
                existing.AddStack(Mathf.Max(1, stack));
                return existing;
            }

            var item = CreateItem(itemData.AttackPattern);
            if (item == null)
            {
                Debug.LogWarning($"[PlayerItem] 미지원 패턴: {itemData.AttackPattern} ({itemData.Tid})", this);
                return null;
            }

            item.Setup(itemData, _owner, Mathf.Max(1, stack));
            _items.Add(item);
            return item;
        }

        public SurvivorsRunItemBase FindByTid(string tid)
        {
            if (string.IsNullOrEmpty(tid))
                return null;

            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item != null && item.Tid == tid)
                    return item;
            }

            return null;
        }

        public bool RemoveItem(string tid)
        {
            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item == null || item.Tid != tid)
                    continue;

                item.Dispose();
                _items.RemoveAt(i);
                return true;
            }

            return false;
        }

        public void ClearAll()
        {
            for (var i = 0; i < _items.Count; i++)
                _items[i]?.Dispose();
            _items.Clear();
        }

        private static SurvivorsRunItemBase CreateItem(SURVIVORSRUN_ATTACK_PATTERN pattern)
        {
            return pattern switch
            {
                SURVIVORSRUN_ATTACK_PATTERN.ORBIT => new SurvivorsRunOrbitItem(),
                SURVIVORSRUN_ATTACK_PATTERN.PULSE => new SurvivorsRunPulseItem(),
                // AURA / PROJECTILE: 이후 추가
                _ => null,
            };
        }
    }
}
