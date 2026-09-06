using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SHIN
{
    public class OpponentSelectUI : UIBase
    {
        [SerializeField]
        private Transform _opponentSelectParent;

        private readonly List<GameObject> _spawnedItems = new();
        private Task _readyTask;

        public override void OnShow()
        {
            EnsureReadyStarted();
        }

        /// <summary>아이템 생성·초상화 로드까지 끝난 뒤 완료된다.</summary>
        public Task WaitUntilReadyAsync()
        {
            EnsureReadyStarted();
            return _readyTask ?? Task.CompletedTask;
        }

        private void EnsureReadyStarted()
        {
            if (_readyTask != null)
                return;

            if (_spawnedItems.Count > 0)
            {
                _readyTask = Task.CompletedTask;
                return;
            }

            _readyTask = PopulateAsync();
        }

        private async Task PopulateAsync()
        {
            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager == null)
            {
                Debug.LogError("[OpponentSelectUI] ResourceManager가 없습니다.");
                return;
            }

            var opponentDataSO = await resourceManager.LoadAsync<OpponentDataSO>(PublicVariable.Address.OpponentDataSO);
            if (this == null)
                return;

            if (opponentDataSO == null)
            {
                Debug.LogError("[OpponentSelectUI] OpponentDataSO 로드에 실패했습니다.");
                return;
            }

            var parent = _opponentSelectParent != null ? _opponentSelectParent : transform;
            var opponentList = opponentDataSO.OpponentList;
            if (opponentList == null)
                return;

            var bindTasks = new List<Task>();

            for (var i = 0; i < opponentList.Count; i++)
            {
                var data = opponentList[i];
                if (data == null)
                    continue;

                var instance = await resourceManager.InstantiateAsync(
                    PublicVariable.Address.OpponentSelectItem,
                    parent,
                    startInactive: false);

                if (this == null)
                {
                    if (instance != null)
                        resourceManager.ReleaseInstance(instance);
                    return;
                }

                if (instance == null)
                    continue;

                _spawnedItems.Add(instance);

                var item = instance.GetComponent<OpponentSelectItem>();
                if (item != null)
                    bindTasks.Add(item.BindAsync(data, OnOpponentSelected));
            }

            if (bindTasks.Count > 0)
                await Task.WhenAll(bindTasks);
        }

        private void OnOpponentSelected(OpponentData data)
        {
            InGamePokerUI.GameStart(data);
        }

        private void ClearItems()
        {
            var resourceManager = GameManager.Instance?.ResourceManager;
            for (var i = 0; i < _spawnedItems.Count; i++)
            {
                var instance = _spawnedItems[i];
                if (instance == null)
                    continue;

                if (resourceManager != null)
                    resourceManager.ReleaseInstance(instance);
                else
                    Destroy(instance);
            }

            _spawnedItems.Clear();
            _readyTask = null;
        }

        private void OnDestroy()
        {
            ClearItems();
        }
    }
}
