namespace SHIN
{
    /// <summary>
    /// 상점 슬롯 표시용 임시 뷰 모델. SO 확정 전 UI 바인딩에만 쓴다.
    /// </summary>
    public sealed class ShopEntryView
    {
        public string Tid;
        public string DisplayName;
        public string Description;
        public int Price;
        public bool Owned;
        public bool CanAfford;
    }
}
