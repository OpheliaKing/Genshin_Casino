using UnityEngine;

namespace SHIN.FlowField
{
    /// <summary>
    /// 이 오브젝트 콜라이더가 덮는 그리드 칸을 막힌 길로 표시한다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class FlowFieldBlock : MonoBehaviour
    {
        [SerializeField] private Collider _collider;
        [SerializeField] private float _boundsShrink = 0.05f;

        private void Awake()
        {
            if (_collider == null)
                _collider = GetComponent<Collider>();
        }

        private void OnEnable()
        {
            var grid = FindFirstObjectByType<FlowFieldGrid>();
            if (grid != null)
                grid.RefreshBlocked();
        }

        private void OnDisable()
        {
            var grid = FindFirstObjectByType<FlowFieldGrid>();
            if (grid != null)
                grid.RefreshBlocked();
        }

        public bool TryGetBlockBounds(out Bounds bounds)
        {
            if (_collider == null)
                _collider = GetComponent<Collider>();

            if (_collider == null)
            {
                bounds = default;
                return false;
            }

            bounds = _collider.bounds;
            if (_boundsShrink > 0f)
                bounds.Expand(-_boundsShrink * 2f);
            return true;
        }
    }
}
