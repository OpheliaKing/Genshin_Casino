using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 아이템 획득 플로팅 텍스트. Addressables 없이 런타임 템플릿 + 공용 풀.
    /// </summary>
    public partial class SurvivorsRunManager
    {
        private const string ItemAcquirePopupPoolKey = "sr_item_acquire_popup";

        private GameObject _itemAcquirePopupTemplate;
        private bool _itemAcquireEventsBound;

        private void BindItemAcquireEvents()
        {
            if (_itemAcquireEventsBound || _playerInfo == null)
                return;

            _playerInfo.ItemAcquired += OnPlayerItemAcquired;
            _itemAcquireEventsBound = true;
        }

        private void UnbindItemAcquireEvents()
        {
            if (!_itemAcquireEventsBound || _playerInfo == null)
                return;

            _playerInfo.ItemAcquired -= OnPlayerItemAcquired;
            _itemAcquireEventsBound = false;
        }

        private void OnPlayerItemAcquired(SurvivorsRunItemData itemData, int stackAfter)
        {
            if (itemData == null || IsRunFinished)
                return;

            var pos = _playerUnit != null
                ? _playerUnit.transform.position
                : transform.position;
            ShowItemAcquirePopup(pos, itemData.ResolveAcquirePopupText(stackAfter));
        }

        public void ShowItemAcquirePopup(Vector3 worldPosition, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            var prefab = EnsureItemAcquirePopupTemplate();
            if (prefab == null)
                return;

            var instance = RentFromPrefab(
                ItemAcquirePopupPoolKey,
                prefab,
                activeParent: transform,
                activate: false);
            if (instance == null)
                return;

            instance.transform.SetParent(transform, true);
            instance.SetActive(true);

            var popup = instance.GetComponent<SurvivorsRunItemAcquirePopup>();
            if (popup == null)
            {
                Debug.LogError("[SurvivorsRunManager] ItemAcquirePopup 컴포넌트가 없습니다.");
                ReturnPooled(ItemAcquirePopupPoolKey, instance);
                return;
            }

            popup.Play(this, worldPosition, message, ItemAcquirePopupPoolKey);
        }

        private GameObject EnsureItemAcquirePopupTemplate()
        {
            if (_itemAcquirePopupTemplate != null)
                return _itemAcquirePopupTemplate;

            var go = new GameObject("sr_item_acquire_popup_template");
            go.transform.SetParent(transform, false);
            go.SetActive(false);

            var popup = go.AddComponent<SurvivorsRunItemAcquirePopup>();
            popup.EnsureBuilt();

            _itemAcquirePopupTemplate = go;
            PrewarmFromPrefab(ItemAcquirePopupPoolKey, go, PoolExpandBatch);
            return go;
        }
    }
}
