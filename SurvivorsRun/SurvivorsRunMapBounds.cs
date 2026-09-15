using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 레거시 clamp. 신규는 <see cref="SurvivorsRunMap"/>를 사용한다.
    /// </summary>
    public class SurvivorsRunMapBounds : MonoBehaviour
    {
        [SerializeField] private Vector2 _min = new(-20f, -20f);
        [SerializeField] private Vector2 _max = new(20f, 20f);

        public Vector2 Min => _min;
        public Vector2 Max => _max;

        public Vector3 ClampPosition(Vector3 position)
        {
            position.x = Mathf.Clamp(position.x, _min.x, _max.x);
            position.y = Mathf.Clamp(position.y, _min.y, _max.y);
            return position;
        }

        public bool Contains(Vector3 position)
        {
            return position.x >= _min.x && position.x <= _max.x &&
                   position.y >= _min.y && position.y <= _max.y;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.4f, 0.35f);
            var center = new Vector3((_min.x + _max.x) * 0.5f, (_min.y + _max.y) * 0.5f, 0f);
            var size = new Vector3(_max.x - _min.x, _max.y - _min.y, 0.1f);
            Gizmos.DrawWireCube(center, size);
        }
#endif
    }
}
