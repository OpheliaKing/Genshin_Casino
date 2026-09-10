using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace SHIN
{
    public class SurvivorsRunCharacterSelectUI : UIBase
    {
        [FormerlySerializedAs("charSelectItemParent")]
        [SerializeField]
        private Transform _charSelectItemParent;

        private readonly List<GameObject> _spawnedItems = new();
        private SurvivorsRunManager _manager;
        private Task _readyTask;

        public override void OnShow()
        {
        }

        /// <summary>
        /// Manager Init에서 호출. SO 로드 후 아이템을 생성·바인딩한다.
        /// </summary>
        public Task SetupAsync(SurvivorsRunManager manager)
        {
            _manager = manager;
            if (_readyTask != null)
                return _readyTask;

            _readyTask = PopulateAsync();
            return _readyTask;
        }

        public Task WaitUntilReadyAsync()
        {
            return _readyTask ?? Task.CompletedTask;
        }

        private async Task PopulateAsync()
        {
            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager == null)
            {
                Debug.LogError("[SurvivorsRunCharacterSelectUI] ResourceManager가 없습니다.");
                return;
            }

            var characterSo = await resourceManager.LoadAsync<SurvivorsRunCharacterSO>(
                PublicVariable.Address.SurvivorsRunCharacterSO);
            if (this == null)
                return;

            if (characterSo == null)
            {
                Debug.LogError("[SurvivorsRunCharacterSelectUI] SurvivorsRunCharacterSO 로드에 실패했습니다.");
                return;
            }

            var parent = _charSelectItemParent != null ? _charSelectItemParent : transform;
            var list = characterSo.CharacterList;
            if (list == null)
                return;

            var bindTasks = new List<Task>();

            for (var i = 0; i < list.Count; i++)
            {
                var data = list[i];
                if (data == null)
                    continue;

                var instance = await resourceManager.InstantiateAsync(
                    PublicVariable.Address.SurvivorsRunCharacterSelectItem,
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

                var item = instance.GetComponent<SurvivorsRunCharacterSelectItem>();
                if (item != null)
                    bindTasks.Add(item.BindAsync(data, OnCharacterSelected));
            }

            if (bindTasks.Count > 0)
                await Task.WhenAll(bindTasks);
        }

        private void OnCharacterSelected(SurvivorsRunUnitData data)
        {
            if (_manager == null || data == null)
                return;

            _manager.OnCharacterSelected(data);
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
