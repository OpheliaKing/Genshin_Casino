using System.Collections.Generic;
using UnityEngine;

namespace SHIN.FlowField
{
    /// <summary>
    /// 플로우 방향을 따라가고, 같은 칸에 겹치지 않게 약간 밀려난다.
    /// 벽에 막히면 법선 기준으로 미끄러진다.
    /// </summary>
    public class FlowFieldAgent : MonoBehaviour
    {
        [SerializeField] private FlowField _field;
        [SerializeField] private float _speed = 3.5f;
        [SerializeField] private float _separationRadius = 0.65f;
        [SerializeField] private float _arriveDistance = 0.08f;
        [SerializeField] private float _castRadius = 0.2f;
        [SerializeField] private float _castHeight = 0.45f;
        [SerializeField] private float _skin = 0.02f;

        private static readonly List<FlowFieldAgent> Active = new();

        private void Awake()
        {
            if (_field == null)
                _field = FindFirstObjectByType<FlowField>();
        }

        private void OnEnable()
        {
            Active.Add(this);
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }

        private void Update()
        {
            if (_field == null || _field.Grid == null)
                return;

            var grid = _field.Grid;
            var goal = grid.CellCenter(_field.GoalX, _field.GoalY);
            var toGoal = goal - transform.position;
            toGoal.y = 0f;

            Vector3 move;
            if (grid.TryWorldToCell(transform.position, out var x, out var y)
                && x == _field.GoalX
                && y == _field.GoalY)
            {
                if (toGoal.magnitude <= _arriveDistance)
                    return;

                move = toGoal.normalized;
            }
            else if (_field.TryGetDirection(transform.position, out var flow))
            {
                move = flow + Separation();
                if (move.sqrMagnitude < 0.0001f)
                    return;

                move.Normalize();
            }
            else
            {
                return;
            }

            var delta = move * (_speed * Time.deltaTime);
            delta.y = 0f;

            var next = transform.position + SlideMove(delta);
            next.y = transform.position.y;
            transform.position = next;
        }

        private Vector3 SlideMove(Vector3 delta)
        {
            var distance = delta.magnitude;
            if (distance < 0.0001f)
                return Vector3.zero;

            var direction = delta / distance;
            var origin = transform.position + Vector3.up * _castHeight;

            if (!TryCastWall(origin, direction, distance, out var hit))
                return delta;

            var traveled = Mathf.Max(0f, hit.distance - _skin);
            var moved = direction * traveled;

            var remaining = delta - direction * traveled;
            var slide = remaining - Vector3.Dot(remaining, hit.normal) * hit.normal;
            slide.y = 0f;

            var slideDistance = slide.magnitude;
            if (slideDistance < 0.0001f)
                return moved;

            var slideDirection = slide / slideDistance;
            var slideOrigin = origin + moved;
            if (!TryCastWall(slideOrigin, slideDirection, slideDistance, out var slideHit))
                return moved + slide;

            var slideTraveled = Mathf.Max(0f, slideHit.distance - _skin);
            return moved + slideDirection * slideTraveled;
        }

        private bool TryCastWall(Vector3 origin, Vector3 direction, float distance, out RaycastHit hit)
        {
            var hits = Physics.SphereCastAll(origin, _castRadius, direction, distance + _skin);
            var bestDistance = float.MaxValue;
            var found = false;
            hit = default;

            for (var i = 0; i < hits.Length; i++)
            {
                var candidate = hits[i];
                if (!IsWall(candidate.collider))
                    continue;

                if (candidate.distance >= bestDistance)
                    continue;

                bestDistance = candidate.distance;
                hit = candidate;
                found = true;
            }

            return found;
        }

        private static bool IsWall(Collider collider)
        {
            if (collider == null)
                return false;

            if (collider.GetComponent<FlowFieldBlock>() != null)
                return true;

            return collider.CompareTag("FlowFieldObstacle");
        }

        private Vector3 Separation()
        {
            var push = Vector3.zero;
            for (var i = 0; i < Active.Count; i++)
            {
                var other = Active[i];
                if (other == this)
                    continue;

                var away = transform.position - other.transform.position;
                away.y = 0f;
                var distance = away.magnitude;
                if (distance < 0.0001f || distance > _separationRadius)
                    continue;

                push += away.normalized * (_separationRadius - distance);
            }

            return push;
        }
    }
}
