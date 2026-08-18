using UnityEngine;

namespace SHIN
{
    public abstract class UIBase : MonoBehaviour
    {
        [SerializeField] private UIType _uiType = UIType.FullScreen;
        [SerializeField] private UIReleasePolicy _releasePolicy = UIReleasePolicy.ByUIType;

        public UIType UIType => _uiType;
        public UIReleasePolicy ReleasePolicy => _releasePolicy;

        public bool ShouldReleaseOnClose =>
            _releasePolicy switch
            {
                UIReleasePolicy.ReleaseOnClose => true,
                UIReleasePolicy.KeepHidden => false,
                UIReleasePolicy.ByUIType => _uiType == UIType.Popup,
                _ => true
            };

        public virtual void OnShow() { }

        public virtual void OnHide() { }
    }
}
