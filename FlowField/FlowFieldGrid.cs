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
        [SerializeField] private string _obstacleTag = "FlowFieldObstacle";
        [SerializeField] private float _obstaclePadding = 0.45f;

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

            var half = new Vector3(_cellSize * _obstaclePadding, 0.5f, _cellSize * _obstaclePadding);
            for (var y = 0; y < _height; y++)
            {
                for (var x = 0; x < _width; x++)
                {
                    var center = CellCenter(x, y) + Vector3.up * 0.5f;
                    var hits = Physics.OverlapBox(center, half);
                    var blocked = false;
                    for (var i = 0; i < hits.Length; i++)
                    {
                        if (hits[i].CompareTag(_obstacleTag))
                        {
                            blocked = true;
                            break;
                        }
                    }

                    _blocked[Index(x, y)] = blocked;
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
