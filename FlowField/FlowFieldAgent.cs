using System.Collections.Generic;
using UnityEngine;

namespace SHIN.FlowField
{
    /// <summary>
    /// 플로우 방향을 따라가고, 같은 칸에 겹치지 않게 약간 밀려난다.
    /// </summary>
    public class FlowFieldAgent : MonoBehaviour
    {
        [SerializeField] private FlowField _field;
        [SerializeField] private float _speed = 3.5f;
        [SerializeField] private float _separationRadius = 0.65f;
        [SerializeField] private float _arriveDistance = 0.08f;

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

            var next = transform.position + move * (_speed * Time.deltaTime);
            next.y = transform.position.y;
            transform.position = next;
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
