using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 세션 카메라. 고정 디펜스에서는 follow 없이 맵 기준으로 한 번 스냅한다.
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

        /// <summary>플레이어를 따라가지 않고, 맵/스폰 기준으로 카메라를 고정한다.</summary>
        private void BindCameraFixed()
        {
            EnsureCameraFollow();
            if (_cameraFollow == null)
                return;

            _cameraFollow.Bind(_playerUnit != null ? _playerUnit.transform : null, _activeMap, enableFollow: false);

            var snapWorld = GetPlayerSpawnWorldPosition();
            if (_activeMap != null)
                snapWorld = _activeMap.GetClampCenterWorld(snapWorld.z);

            _cameraFollow.SnapToWorld(snapWorld);
        }

        private void StopCameraFollow()
        {
            if (_cameraFollow != null)
                _cameraFollow.SetFollowEnabled(false);
        }
    }
}
