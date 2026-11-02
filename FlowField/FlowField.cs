using UnityEngine;
using UnityEngine.InputSystem;

namespace SHIN.FlowField
{
    /// <summary>
    /// 목표 칸까지의 비용과, 각 칸에서 갈 방향을 계산한다.
    /// 왼쪽 클릭으로 목표를 바꾼다.
    /// </summary>
    public class FlowField : MonoBehaviour
    {
        [SerializeField] private FlowFieldGrid _grid;
        [SerializeField] private int _goalX = 16;
        [SerializeField] private int _goalY = 16;

        private int[] _cost = System.Array.Empty<int>();
        private Vector3[] _direction = System.Array.Empty<Vector3>();

        private static readonly int[] NeighborX = { 1, -1, 0, 0, 1, 1, -1, -1 };
        private static readonly int[] NeighborY = { 0, 0, 1, -1, 1, -1, 1, -1 };
        private const int OrthogonalCost = 10;
        private const int DiagonalCost = 14;
        private const int Unreachable = int.MaxValue;

        public FlowFieldGrid Grid => _grid;
        public int GoalX => _goalX;
        public int GoalY => _goalY;

        private void Awake()
        {
            if (_grid == null)
                _grid = GetComponent<FlowFieldGrid>();
        }

        private void Start()
        {
            _grid.RefreshBlocked();
            Build(_goalX, _goalY);
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
                return;

            var camera = Camera.main;
            if (camera == null)
                return;

            var ray = camera.ScreenPointToRay(mouse.position.ReadValue());
            if (!Physics.Raycast(ray, out var hit))
                return;

            if (!_grid.TryWorldToCell(hit.point, out var x, out var y))
                return;

            Build(x, y);
        }

        public void Build(int goalX, int goalY)
        {
            _goalX = goalX;
            _goalY = goalY;

            var count = _grid.Width * _grid.Height;
            if (_cost.Length != count)
            {
                _cost = new int[count];
                _direction = new Vector3[count];
            }

            for (var i = 0; i < count; i++)
            {
                _cost[i] = Unreachable;
                _direction[i] = Vector3.zero;
            }

            if (!_grid.InBounds(goalX, goalY) || _grid.IsBlocked(goalX, goalY))
                return;

            FillCost(goalX, goalY);
            FillDirection();
        }

        public bool TryGetDirection(Vector3 world, out Vector3 direction)
        {
            direction = Vector3.zero;
            if (_direction.Length == 0)
                return false;

            if (!_grid.TryWorldToCell(world, out var x, out var y))
                return false;

            if (_grid.IsBlocked(x, y))
                return false;

            direction = _direction[_grid.Index(x, y)];
            return direction.sqrMagnitude > 0.0001f;
        }

        private void FillCost(int goalX, int goalY)
        {
            var open = new System.Collections.Generic.List<int>(128);
            var goal = _grid.Index(goalX, goalY);
            _cost[goal] = 0;
            open.Add(goal);

            while (open.Count > 0)
            {
                var best = 0;
                for (var i = 1; i < open.Count; i++)
                {
                    if (_cost[open[i]] < _cost[open[best]])
                        best = i;
                }

                var current = open[best];
                open.RemoveAt(best);
                var x = current % _grid.Width;
                var y = current / _grid.Width;

                for (var n = 0; n < 8; n++)
                {
                    var nx = x + NeighborX[n];
                    var ny = y + NeighborY[n];
                    if (!CanStep(x, y, nx, ny))
                        continue;

                    var step = NeighborX[n] != 0 && NeighborY[n] != 0 ? DiagonalCost : OrthogonalCost;
                    var nextCost = _cost[current] + step;
                    var next = _grid.Index(nx, ny);
                    if (nextCost >= _cost[next])
                        continue;

                    var wasUnreachable = _cost[next] == Unreachable;
                    _cost[next] = nextCost;
                    if (wasUnreachable || !open.Contains(next))
                        open.Add(next);
                }
            }
        }

        private void FillDirection()
        {
            for (var y = 0; y < _grid.Height; y++)
            {
                for (var x = 0; x < _grid.Width; x++)
                {
                    if (_grid.IsBlocked(x, y))
                        continue;

                    var current = _grid.Index(x, y);
                    if (_cost[current] == Unreachable)
                        continue;

                    var bestCost = _cost[current];
                    var bestX = x;
                    var bestY = y;
                    for (var n = 0; n < 8; n++)
                    {
                        var nx = x + NeighborX[n];
                        var ny = y + NeighborY[n];
                        if (!CanStep(x, y, nx, ny))
                            continue;

                        var neighborCost = _cost[_grid.Index(nx, ny)];
                        if (neighborCost >= bestCost)
                            continue;

                        bestCost = neighborCost;
                        bestX = nx;
                        bestY = ny;
                    }

                    if (bestX == x && bestY == y)
                        continue;

                    var direction = _grid.CellCenter(bestX, bestY) - _grid.CellCenter(x, y);
                    direction.y = 0f;
                    _direction[current] = direction.normalized;
                }
            }
        }

        private bool CanStep(int x, int y, int nx, int ny)
        {
            if (!_grid.InBounds(nx, ny) || _grid.IsBlocked(nx, ny))
                return false;

            var dx = nx - x;
            var dy = ny - y;
            if (dx != 0 && dy != 0)
            {
                if (_grid.IsBlocked(x + dx, y) || _grid.IsBlocked(x, y + dy))
                    return false;
            }

            return true;
        }

        private void OnDrawGizmos()
        {
            if (_grid == null || _direction.Length != _grid.Width * _grid.Height)
                return;

            Gizmos.color = new Color(0.2f, 0.45f, 1f, 0.9f);
            for (var y = 0; y < _grid.Height; y++)
            {
                for (var x = 0; x < _grid.Width; x++)
                {
                    var direction = _direction[_grid.Index(x, y)];
                    if (direction.sqrMagnitude < 0.0001f)
                        continue;

                    var start = _grid.CellCenter(x, y) + Vector3.up * 0.08f;
                    Gizmos.DrawLine(start, start + direction * (_grid.CellSize * 0.35f));
                }
            }

            if (_grid.InBounds(_goalX, _goalY))
            {
                Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.95f);
                Gizmos.DrawWireSphere(_grid.CellCenter(_goalX, _goalY) + Vector3.up * 0.2f, 0.25f);
            }
        }
    }
}
