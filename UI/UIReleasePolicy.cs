namespace SHIN
{
    public enum UIReleasePolicy
    {
        /// <summary>FullScreen → 유지, Popup → 닫을 때 Release.</summary>
        ByUIType,

        /// <summary>닫아도 인스턴스 유지(숨김만).</summary>
        KeepHidden,

        /// <summary>닫을 때 Addressables Release.</summary>
        ReleaseOnClose
    }
}
