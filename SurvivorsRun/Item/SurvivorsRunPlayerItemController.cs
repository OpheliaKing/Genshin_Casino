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

            if (itemData.IsPassive)
            {
                if (!itemData.HasPassiveBuffEffects)
                {
                    Debug.LogWarning($"[PlayerItem] 패시브 Effects(버프)가 없습니다: {itemData.Tid}", this);
                    return null;
                }
            }
            else if (itemData.ItemType == SURVIVORSRUN_ITEM_TYPE.WEAPON && !itemData.HasAttackPattern)
            {
                Debug.LogWarning($"[PlayerItem] 무기 패턴이 없습니다: {itemData.Tid}", this);
                return null;
            }
            else if (itemData.IsInputTriggered && !itemData.UsesAttackPattern)
            {
                Debug.LogWarning($"[PlayerItem] 고유/액티브 패턴이 없습니다: {itemData.Tid}", this);
                return null;
            }

            var existing = FindByTid(itemData.Tid);
            if (existing != null)
            {
                var room = itemData.GetRemainingStackRoom(existing.Stack);
                if (room <= 0)
                    return existing;

                existing.AddStack(Mathf.Min(Mathf.Max(1, stack), room));
                return existing;
            }

            var item = CreateItem(itemData);
            if (item == null)
            {
                Debug.LogWarning(
                    $"[PlayerItem] 미지원 아이템: type={itemData.ItemType}, pattern={itemData.AttackPattern} ({itemData.Tid})",
                    this);
                return null;
            }

            item.Setup(itemData, _owner, Mathf.Min(Mathf.Max(1, stack), itemData.MaxStack));
            _items.Add(item);
            return item;
        }

        /// <summary>ACTIVE / UNIQUE 수동 발동.</summary>
        public bool TryActivateItem(string tid)
        {
            var item = FindByTid(tid);
            return item != null && item.TryActivate();
        }

        public SurvivorsRunItemBase FindUniqueSkill()
        {
            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item?.ItemData != null && item.ItemData.ItemType == SURVIVORSRUN_ITEM_TYPE.UNIQUE)
                    return item;
            }

            return null;
        }

        /// <summary>보유 ACTIVE 중 index번째를 발동. 없으면 false.</summary>
        public bool TryActivateActiveAt(int index)
        {
            var found = 0;
            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item?.ItemData == null || item.ItemData.ItemType != SURVIVORSRUN_ITEM_TYPE.ACTIVE)
                    continue;

                if (found == index)
                    return item.TryActivate();

                found++;
            }

            return false;
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

            if (_owner != null)
            {
                var mods = _owner.GetComponent<SurvivorsRunPlayerCombatMods>();
                mods?.ClearAll();
            }
        }

        private static SurvivorsRunItemBase CreateItem(SurvivorsRunItemData itemData)
        {
            if (itemData == null)
                return null;

            if (itemData.IsPassive)
                return new SurvivorsRunPassiveItem();

            return itemData.AttackPattern switch
            {
                SURVIVORSRUN_ATTACK_PATTERN.ORBIT => new SurvivorsRunOrbitItem(),
                SURVIVORSRUN_ATTACK_PATTERN.PULSE => new SurvivorsRunPulseItem(),
                SURVIVORSRUN_ATTACK_PATTERN.AURA => new SurvivorsRunAuraItem(),
                SURVIVORSRUN_ATTACK_PATTERN.PROJECTILE => new SurvivorsRunProjectileItem(),
                SURVIVORSRUN_ATTACK_PATTERN.BUFF => new SurvivorsRunBuffItem(),
                _ => null,
            };
        }
    }
}
