using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 뱀서식 카메라: 타겟을 화면 중앙에 두고, 맵 clamp로 뷰포트가 밖으로 나가지 않게 한다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class SurvivorsRunCameraFollow : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private float _smoothTime = 0f;
        [SerializeField] private bool _followEnabled;

        private Transform _target;
        private SurvivorsRunMap _map;
        private Vector3 _velocity;

        public Camera Camera => _camera != null ? _camera : (_camera = GetComponent<Camera>());
        public bool FollowEnabled => _followEnabled;

        private void Awake()
        {
            if (_camera == null)
                _camera = GetComponent<Camera>();
        }

        public void Bind(Transform target, SurvivorsRunMap map, bool enableFollow = true)
        {
            _target = target;
            _map = map;
            _followEnabled = enableFollow && target != null;
            _velocity = Vector3.zero;

            if (_followEnabled)
                SnapToTarget();
        }

        public void SetTarget(Transform target)
        {
            _target = target;
            _velocity = Vector3.zero;
        }

        public void SetMap(SurvivorsRunMap map)
        {
            _map = map;
        }

        public void SetFollowEnabled(bool enabled)
        {
            _followEnabled = enabled && _target != null;
            if (_followEnabled)
                SnapToTarget();
        }

        public void SnapToTarget()
        {
            if (_target == null)
                return;

            transform.position = ResolveDesiredPosition(_target.position);
            _velocity = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (!_followEnabled || _target == null)
                return;

            var desired = ResolveDesiredPosition(_target.position);
            if (_smoothTime <= 0f)
            {
                transform.position = desired;
                return;
            }

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desired,
                ref _velocity,
                _smoothTime);
        }

        private Vector3 ResolveDesiredPosition(Vector3 targetWorld)
        {
            var desired = transform.position;
            desired.x = targetWorld.x;
            desired.y = targetWorld.y;

            var cam = Camera;
            if (_map == null || cam == null)
                return desired;

            GetHalfExtents(cam, out var halfW, out var halfH);
            return _map.ClampCameraCenter(desired, halfW, halfH);
        }

        private static void GetHalfExtents(Camera cam, out float halfW, out float halfH)
        {
            if (cam.orthographic)
            {
                halfH = cam.orthographicSize;
                halfW = halfH * cam.aspect;
                return;
            }

            var dist = Mathf.Abs(cam.transform.position.z);
            halfH = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * dist;
            halfW = halfH * cam.aspect;
        }
    }
}
