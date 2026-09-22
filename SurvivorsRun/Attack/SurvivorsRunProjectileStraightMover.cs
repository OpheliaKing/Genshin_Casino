using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 투사체 직선 이동. 이후 Arc 등은 별도 Mover 컴포넌트로 추가한다.
    /// </summary>
    public class SurvivorsRunProjectileStraightMover : MonoBehaviour
    {
        private Vector2 _direction = Vector2.right;
        private float _speed;
        private bool _active;
        private SurvivorsRunUnitBase _timeOwner;

        public bool IsActive => _active;
        public Vector2 Direction => _direction;
        public float Speed => _speed;

        public void BindTimeOwner(SurvivorsRunUnitBase timeOwner)
        {
            _timeOwner = timeOwner;
        }

        public void Launch(Vector2 direction, float speed)
        {
            if (direction.sqrMagnitude <= 0.0001f)
                direction = Vector2.right;

            _direction = direction.normalized;
            _speed = Mathf.Max(0f, speed);
            _active = _speed > 0f;

            var angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void Stop()
        {
            _active = false;
        }

        private void Update()
        {
            if (!_active)
                return;

            var scale = _timeOwner != null && _timeOwner.Manager != null
                ? _timeOwner.Manager.TimeScale
                : 1f;
            if (scale <= 0f)
                return;

            var delta = _direction * (_speed * Time.deltaTime * scale);
            transform.position += new Vector3(delta.x, delta.y, 0f);
        }
    }
}
