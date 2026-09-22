using System;
using System.Collections.Generic;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 런 중 보유 아이템 한 칸. UI·저장용 데이터 정본이다.
    /// </summary>
    public sealed class SurvivorsRunPlayerOwnedItem
    {
        public string Tid { get; internal set; }
        public int Stack { get; internal set; }
        public SurvivorsRunItemData ItemData { get; internal set; }
    }

    /// <summary>
    /// 이번 판 플레이어 진행 정보. <see cref="SurvivorsRunManager"/>가 소유한다.
    /// 아이템 데이터 정본이며, 런타임 발동은 <see cref="SurvivorsRunPlayerItemController"/>에 반영한다.
    /// </summary>
    public sealed class SurvivorsRunPlayerInfo
    {
        private readonly List<SurvivorsRunPlayerOwnedItem> _ownedItems = new();
        private SurvivorsRunPlayerItemController _itemController;

        public int Exp { get; private set; }
        public int Level { get; private set; } = 1;
        public int KillCount { get; private set; }
        public IReadOnlyList<SurvivorsRunPlayerOwnedItem> OwnedItems => _ownedItems;

        public event Action<int, int> ExpChanged;      // current, toNext
        public event Action<int> LevelChanged;
        public event Action<int> KillCountChanged;
        public event Action OwnedItemsChanged;

        public void BindItemController(SurvivorsRunPlayerItemController itemController)
        {
            _itemController = itemController;
        }

        /// <summary>런 시작·캐릭터 재선택 시 진행도를 초기화한다.</summary>
        public void Reset()
        {
            Exp = 0;
            Level = 1;
            KillCount = 0;
            _ownedItems.Clear();
            _itemController?.ClearAll();

            ExpChanged?.Invoke(Exp, GetExpToNextLevel());
            LevelChanged?.Invoke(Level);
            KillCountChanged?.Invoke(KillCount);
            OwnedItemsChanged?.Invoke();
        }

        public void AddKill(int amount = 1)
        {
            if (amount <= 0)
                return;

            KillCount += amount;
            KillCountChanged?.Invoke(KillCount);
        }

        public void AddExp(int amount)
        {
            if (amount <= 0)
                return;

            Exp += amount;
            while (true)
            {
                var need = GetExpToNextLevel();
                if (Exp < need)
                    break;

                Exp -= need;
                Level += 1;
                LevelChanged?.Invoke(Level);
            }

            ExpChanged?.Invoke(Exp, GetExpToNextLevel());
        }

        /// <summary>
        /// 보유 목록을 갱신한 뒤 ItemController에 런타임을 반영한다.
        /// </summary>
        public SurvivorsRunPlayerOwnedItem AddItem(SurvivorsRunItemData itemData, int stack = 1)
        {
            if (itemData == null || string.IsNullOrWhiteSpace(itemData.Tid))
                return null;

            stack = Mathf.Max(1, stack);
            var tid = itemData.Tid.Trim();
            var owned = FindOwned(tid);
            if (owned != null)
            {
                owned.Stack += stack;
                owned.ItemData = itemData;
            }
            else
            {
                owned = new SurvivorsRunPlayerOwnedItem
                {
                    Tid = tid,
                    Stack = stack,
                    ItemData = itemData,
                };
                _ownedItems.Add(owned);
            }

            if (_itemController != null)
                _itemController.AddOrStackItem(itemData, stack);
            else
                Debug.LogWarning("[PlayerInfo] ItemController가 없어 런타임 아이템을 반영하지 못했습니다.");

            OwnedItemsChanged?.Invoke();
            return owned;
        }

        public SurvivorsRunPlayerOwnedItem FindOwned(string tid)
        {
            if (string.IsNullOrEmpty(tid))
                return null;

            for (var i = 0; i < _ownedItems.Count; i++)
            {
                var item = _ownedItems[i];
                if (item != null && item.Tid == tid)
                    return item;
            }

            return null;
        }

        /// <summary>다음 레벨까지 필요한 경험치. 이후 곡선 SO로 빼도 된다.</summary>
        public int GetExpToNextLevel()
        {
            // 임시: 레벨당 10, 20, 30...
            return Mathf.Max(1, Level * 10);
        }
    }
}
