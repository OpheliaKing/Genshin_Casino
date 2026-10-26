using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 처치 골드 플로팅 텍스트. Addressables 없이 런타임 템플릿 + 공용 풀.
    /// </summary>
    public partial class SurvivorsRunManager
    {
        private const string GoldPopupPoolKey = "sr_gold_popup";

        private GameObject _goldPopupTemplate;

        /// <summary>적 사망 지점에 +N 칩 팝업을 띄운다.</summary>
        public void ShowGoldPopup(Vector3 worldPosition, int amount)
        {
            if (amount <= 0)
                return;

            var prefab = EnsureGoldPopupTemplate();
            if (prefab == null)
                return;

            var instance = RentFromPrefab(
                GoldPopupPoolKey,
                prefab,
                activeParent: transform,
                activate: false);
            if (instance == null)
                return;

            instance.transform.SetParent(transform, true);
            instance.SetActive(true);

            var popup = instance.GetComponent<SurvivorsRunGoldPopup>();
            if (popup == null)
            {
                Debug.LogError("[SurvivorsRunManager] GoldPopup 인스턴스에 SurvivorsRunGoldPopup이 없습니다.");
                ReturnPooled(GoldPopupPoolKey, instance);
                return;
            }

            popup.Play(this, worldPosition, amount, GoldPopupPoolKey);
        }

        private GameObject EnsureGoldPopupTemplate()
        {
            if (_goldPopupTemplate != null)
                return _goldPopupTemplate;

            var go = new GameObject("sr_gold_popup_template");
            go.transform.SetParent(transform, false);
            go.SetActive(false);

            var popup = go.AddComponent<SurvivorsRunGoldPopup>();
            popup.EnsureBuilt();

            _goldPopupTemplate = go;
            PrewarmFromPrefab(GoldPopupPoolKey, go, PoolExpandBatch);
            return go;
        }
    }
}
