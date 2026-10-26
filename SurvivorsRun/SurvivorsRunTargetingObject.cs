using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 공격 타겟 표시용 마커. 세션에 하나 두고, 타겟 몬스터의 자식으로 붙인다.
    /// </summary>
    public class SurvivorsRunTargetingObject : MonoBehaviour
    {
        [SerializeField] private Vector3 _localOffset = new(0f, -0.35f, 0f);
        [SerializeField] private bool _resetLocalScale = true;
        [SerializeField] private bool _resetLocalRotation = true;

        private Transform _homeParent;
        private ParticleSystem[] _particleSystems;
        private bool _particlesCached;

        public SurvivorsRunEnemyBase CurrentHost { get; private set; }

        public void SetLocalOffset(Vector3 localOffset)
        {
            _localOffset = localOffset;
        }

        public void BindHome(Transform homeParent)
        {
            _homeParent = homeParent;
        }

        /// <summary>시각 프리팹을 자식으로 한 번만 붙인다.</summary>
        public void EnsureVisual(GameObject visualPrefab)
        {
            if (visualPrefab == null)
                return;

            if (transform.childCount > 0)
                return;

            var visual = Instantiate(visualPrefab, transform);
            visual.name = visualPrefab.name;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            visual.SetActive(true);
            CacheParticles();
        }

        public void AttachTo(SurvivorsRunEnemyBase enemy)
        {
            if (enemy == null || enemy.IsDead)
            {
                Detach();
                return;
            }

            CurrentHost = enemy;
            transform.SetParent(enemy.transform, false);
            transform.localPosition = _localOffset;

            if (_resetLocalRotation)
                transform.localRotation = Quaternion.identity;

            if (_resetLocalScale)
                transform.localScale = Vector3.one;

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            PlayParticles();
        }

        public void Detach()
        {
            CurrentHost = null;

            if (_homeParent != null)
                transform.SetParent(_homeParent, false);
            else
                transform.SetParent(null, false);

            transform.localPosition = Vector3.zero;
            if (_resetLocalRotation)
                transform.localRotation = Quaternion.identity;
            if (_resetLocalScale)
                transform.localScale = Vector3.one;

            StopParticles();

            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        private void CacheParticles()
        {
            _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            _particlesCached = true;
        }

        private void PlayParticles()
        {
            if (!_particlesCached)
                CacheParticles();

            if (_particleSystems == null)
                return;

            for (var i = 0; i < _particleSystems.Length; i++)
            {
                var ps = _particleSystems[i];
                if (ps == null)
                    continue;

                ps.Clear(true);
                ps.Play(true);
            }
        }

        private void StopParticles()
        {
            if (!_particlesCached)
                CacheParticles();

            if (_particleSystems == null)
                return;

            for (var i = 0; i < _particleSystems.Length; i++)
            {
                var ps = _particleSystems[i];
                if (ps == null)
                    continue;

                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
