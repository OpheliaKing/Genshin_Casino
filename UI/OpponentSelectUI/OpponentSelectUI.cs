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
        private bool _isPopulating;

        public override void OnShow()
        {
            if (_isPopulating || _spawnedItems.Count > 0)
                return;

            _ = PopulateAsync();
        }

        private async Task PopulateAsync()
        {
            _isPopulating = true;

            try
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

                for (int i = 0; i < opponentList.Count; i++)
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
                        item.Bind(data, OnOpponentSelected);
                }
            }
            finally
            {
                _isPopulating = false;
            }
        }

        private void OnOpponentSelected(OpponentData data)
        {
            GameManager.Instance?.GameStart(data);
        }

        private void ClearItems()
        {
            var resourceManager = GameManager.Instance?.ResourceManager;
            for (int i = 0; i < _spawnedItems.Count; i++)
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
        }

        private void OnDestroy()
        {
            ClearItems();
        }
    }
}
