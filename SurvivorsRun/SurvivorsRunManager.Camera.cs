using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 세션 카메라 follow 바인딩.
    /// </summary>
    public partial class SurvivorsRunManager
    {
        private SurvivorsRunCameraFollow _cameraFollow;

        public SurvivorsRunCameraFollow CameraFollow => _cameraFollow;

        private void EnsureCameraFollow()
        {
            if (_cameraFollow != null)
                return;

            EnsureEnemyRuntimeRefs();

            if (_runCamera != null)
            {
                _cameraFollow = _runCamera.GetComponent<SurvivorsRunCameraFollow>();
                if (_cameraFollow == null)
                    _cameraFollow = _runCamera.gameObject.AddComponent<SurvivorsRunCameraFollow>();
                return;
            }

            _cameraFollow = GetComponentInChildren<SurvivorsRunCameraFollow>(true);
            if (_cameraFollow != null)
                return;

            var cam = GetComponentInChildren<Camera>(true);
            if (cam == null)
            {
                Debug.LogWarning("[SurvivorsRunManager] 세션에 Camera가 없어 follow를 붙일 수 없습니다.");
                return;
            }

            _runCamera = cam;
            _cameraFollow = cam.GetComponent<SurvivorsRunCameraFollow>();
            if (_cameraFollow == null)
                _cameraFollow = cam.gameObject.AddComponent<SurvivorsRunCameraFollow>();
        }

        private void BindCameraToPlayer()
        {
            EnsureCameraFollow();
            if (_cameraFollow == null)
                return;

            var target = _playerUnit != null ? _playerUnit.transform : null;
            _cameraFollow.Bind(target, _activeMap, enableFollow: target != null);
        }

        private void StopCameraFollow()
        {
            if (_cameraFollow != null)
                _cameraFollow.SetFollowEnabled(false);
        }
    }
}
