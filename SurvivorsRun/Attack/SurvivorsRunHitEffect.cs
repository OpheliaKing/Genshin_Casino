using UnityEngine;

namespace SHIN
{
    public enum SurvivorsRunHitEffectEndMode
    {
        /// <summary>하위 파티클이 전부 꺼질 때까지 대기. 루프 파티클 대비 maxLifetime로 강제 종료.</summary>
        WaitForParticles = 0,

        /// <summary>고정 시간 후 반납.</summary>
        FixedLifetime = 1,
    }

    /// <summary>
    /// 범용 히트/일회성 VFX. Play 후 수명이 끝나면 풀 반환(또는 Destroy).
    /// 하위 ParticleSystem이 여러 개여도 GetComponentsInChildren으로 모아
    /// <b>하나라도 IsAlive면 유지</b>, 전부 꺼지면 반납한다.
    /// </summary>
    public class SurvivorsRunHitEffect : MonoBehaviour
    {
        [SerializeField] private SurvivorsRunHitEffectEndMode _endMode = SurvivorsRunHitEffectEndMode.WaitForParticles;

        [SerializeField]
        [Tooltip("FixedLifetime 모드, 또는 WaitForParticles의 안전 상한(초). 0 이하면 WaitForParticles에서 상한 없음.")]
        private float _lifetime = 1.5f;

        [SerializeField]
        [Tooltip("Play 시 하위 ParticleSystem을 Clear 후 다시 Play.")]
        private bool _restartParticles = true;

        [SerializeField]
        [Tooltip("반납 직전 Stop+Clear. 풀에서 잔상 방지.")]
        private bool _clearParticlesOnDespawn = true;

        private SurvivorsRunManager _manager;
        private string _poolKey;
        private ParticleSystem[] _particleSystems;
        private bool _particlesCached;
        private float _lifeRemaining;
        private bool _playing;
        private bool _despawning;

        private void Awake()
        {
            CacheParticles();
        }

        /// <summary>
        /// 풀에서 꺼낸 뒤 호출. manager·poolKey가 있으면 종료 시 ReturnPooled.
        /// </summary>
        public void Play(
            SurvivorsRunManager manager,
            Vector3 worldPosition,
            string poolKey = null,
            float lifetimeOverride = -1f,
            Quaternion? worldRotation = null)
        {
            _manager = manager;
            _poolKey = poolKey;
            _despawning = false;
            _playing = true;

            transform.position = worldPosition;
            if (worldRotation.HasValue)
                transform.rotation = worldRotation.Value;

            var life = lifetimeOverride > 0f ? lifetimeOverride : _lifetime;
            _lifeRemaining = life;

            if (_restartParticles)
                RestartParticles();
        }

        /// <summary>간단 오버로드. 매니저만 알고 주소는 TryReturn에 맡긴다.</summary>
        public void Play(SurvivorsRunManager manager, Vector3 worldPosition)
        {
            Play(manager, worldPosition, null);
        }

        public void StopAndReturn()
        {
            Despawn();
        }

        private void Update()
        {
            if (!_playing || _despawning)
                return;

            var scale = _manager != null ? _manager.TimeScale : 1f;
            if (scale <= 0f)
                return;

            switch (_endMode)
            {
                case SurvivorsRunHitEffectEndMode.FixedLifetime:
                    _lifeRemaining -= Time.deltaTime * scale;
                    if (_lifeRemaining <= 0f)
                        Despawn();
                    break;

                case SurvivorsRunHitEffectEndMode.WaitForParticles:
                default:
                    // 안전 상한: 루프/무한 파티클이 있어도 풀에 영원히 안 묶이게
                    if (_lifetime > 0f)
                    {
                        _lifeRemaining -= Time.deltaTime * scale;
                        if (_lifeRemaining <= 0f)
                        {
                            Despawn();
                            break;
                        }
                    }

                    if (!AnyParticleAlive())
                        Despawn();
                    break;
            }
        }

        private void CacheParticles()
        {
            // includeInactive: 풀에 있을 때 비활성 자식도 포함
            _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            _particlesCached = true;
        }

        private void RestartParticles()
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

        /// <summary>
        /// 하위 파티클이 N개여도: 배열을 돌며 <b>하나라도</b> 살아 있으면 true.
        /// IsAlive(false)로 각 시스템을  individually 본다 (withChildren true면 중복 체크만 늘어남).
        /// </summary>
        private bool AnyParticleAlive()
        {
            if (!_particlesCached)
                CacheParticles();

            if (_particleSystems == null || _particleSystems.Length == 0)
                return false;

            for (var i = 0; i < _particleSystems.Length; i++)
            {
                var ps = _particleSystems[i];
                if (ps == null)
                    continue;

                if (ps.IsAlive(false))
                    return true;
            }

            return false;
        }

        private void ClearParticles()
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

        private void Despawn()
        {
            if (_despawning)
                return;

            _despawning = true;
            _playing = false;

            if (_clearParticlesOnDespawn)
                ClearParticles();

            if (_manager != null)
            {
                if (!string.IsNullOrEmpty(_poolKey))
                {
                    _manager.ReturnPooled(_poolKey, gameObject);
                    _poolKey = null;
                    return;
                }

                if (_manager.TryReturnPooled(gameObject))
                {
                    _poolKey = null;
                    return;
                }
            }

            Destroy(gameObject);
        }
    }
}
