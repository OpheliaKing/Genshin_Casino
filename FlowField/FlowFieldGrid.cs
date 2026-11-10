using UnityEngine;

namespace SHIN.FlowField
{
    /// <summary>
    /// 바닥 메시 위의 가상 칸. 칸 번호와 막힘 여부만 갖는다.
    /// </summary>
    public class FlowFieldGrid : MonoBehaviour
    {
        [SerializeField] private int _width = 20;
        [SerializeField] private int _height = 20;
        [SerializeField] private float _cellSize = 1f;
        [SerializeField] private Vector3 _origin;

        private bool[] _blocked = System.Array.Empty<bool>();

        public int Width => _width;
        public int Height => _height;
        public float CellSize => _cellSize;
        public Vector3 Origin => _origin;

        private void Awake()
        {
            RefreshBlocked();
        }

        public bool InBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < _width && y < _height;
        }

        public int Index(int x, int y)
        {
            return y * _width + x;
        }

        public bool IsBlocked(int x, int y)
        {
            if (!InBounds(x, y) || _blocked.Length != _width * _height)
                return true;

            return _blocked[Index(x, y)];
        }

        public bool TryWorldToCell(Vector3 world, out int x, out int y)
        {
            var local = world - _origin;
            x = Mathf.FloorToInt(local.x / _cellSize);
            y = Mathf.FloorToInt(local.z / _cellSize);
            return InBounds(x, y);
        }

        public Vector3 CellCenter(int x, int y)
        {
            return _origin + new Vector3((x + 0.5f) * _cellSize, 0f, (y + 0.5f) * _cellSize);
        }

        public void RefreshBlocked()
        {
            var count = _width * _height;
            if (_blocked.Length != count)
                _blocked = new bool[count];

            System.Array.Clear(_blocked, 0, count);

            var blocks = FindObjectsByType<FlowFieldBlock>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (var i = 0; i < blocks.Length; i++)
            {
                if (blocks[i] == null || !blocks[i].isActiveAndEnabled)
                    continue;

                if (!blocks[i].TryGetBlockBounds(out var bounds))
                    continue;

                MarkBlocked(bounds);
            }
        }

        private void MarkBlocked(Bounds bounds)
        {
            var min = bounds.min - _origin;
            var max = bounds.max - _origin;

            var x0 = Mathf.FloorToInt(min.x / _cellSize);
            var x1 = Mathf.CeilToInt(max.x / _cellSize) - 1;
            var y0 = Mathf.FloorToInt(min.z / _cellSize);
            var y1 = Mathf.CeilToInt(max.z / _cellSize) - 1;

            for (var y = y0; y <= y1; y++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    if (!InBounds(x, y))
                        continue;

                    _blocked[Index(x, y)] = true;
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                RefreshBlocked();

            for (var y = 0; y < _height; y++)
            {
                for (var x = 0; x < _width; x++)
                {
                    Gizmos.color = IsBlocked(x, y)
                        ? new Color(0.9f, 0.25f, 0.2f, 0.85f)
                        : new Color(0.4f, 0.85f, 0.45f, 0.35f);
                    Gizmos.DrawWireCube(CellCenter(x, y) + Vector3.up * 0.02f, new Vector3(_cellSize, 0.02f, _cellSize));
                }
            }
        }
    }
}
