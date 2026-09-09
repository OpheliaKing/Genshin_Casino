using UnityEngine;

namespace SHIN
{
    public class SurvivorsRunManager : MonoBehaviour
    {
        private float _timeScale = 1f;
        private SurvivorsRunCombat _combat;

        public float TimeScale => _timeScale;
        public SurvivorsRunCombat Combat => _combat;

        private void Awake()
        {
            _combat = new SurvivorsRunCombat(this);
        }

        public void SetTimeScale(float timeScale)
        {
            _timeScale = Mathf.Max(0f, timeScale);
        }
    }
}
