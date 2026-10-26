using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 히트 이펙트 스폰. Address 단위로 프리팹을 캐시하고 풀에서 대여한다.
    /// </summary>
    public partial class SurvivorsRunManager
    {
        private readonly Dictionary<string, GameObject> _hitEffectPrefabCache = new();
        private readonly HashSet<string> _hitEffectLoadInFlight = new();

        /// <summary>
        /// 명중 지점에 히트 이펙트를 재생한다. 주소가 비었거나 로드 실패 시 무시.
        /// </summary>
        public void PlayHitEffect(string address, Vector3 worldPosition, float lifetime = -1f)
        {
            if (string.IsNullOrWhiteSpace(address))
                return;

            _ = PlayHitEffectAsync(address.Trim(), worldPosition, lifetime, null);
        }

        /// <summary>
        /// 이펙트를 스폰하고 핸들을 반환한다. followParent가 있으면 자식으로 붙여 따라다니게 한다.
        /// </summary>
        public Task<SurvivorsRunHitEffect> PlayHitEffectAsync(
            string address,
            Vector3 worldPosition,
            float lifetime = -1f,
            Transform followParent = null)
        {
            if (string.IsNullOrWhiteSpace(address))
                return Task.FromResult<SurvivorsRunHitEffect>(null);

            return PlayHitEffectInternalAsync(address.Trim(), worldPosition, lifetime, followParent);
        }

        private async Task<SurvivorsRunHitEffect> PlayHitEffectInternalAsync(
            string address,
            Vector3 worldPosition,
            float lifetime,
            Transform followParent)
        {
            var prefab = await EnsureHitEffectPrefabAsync(address);
            if (this == null || prefab == null)
                return null;

            var parent = followParent != null ? followParent : transform;
            var instance = RentFromPrefab(address, prefab, parent, activate: false);
            if (instance == null)
                return null;

            if (followParent != null)
            {
                instance.transform.SetParent(followParent, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
            }
            else
            {
                instance.transform.position = worldPosition;
                instance.transform.rotation = Quaternion.identity;
            }

            instance.SetActive(true);

            var effect = instance.GetComponent<SurvivorsRunHitEffect>();
            if (effect == null)
                effect = instance.GetComponentInChildren<SurvivorsRunHitEffect>(true);

            if (effect == null)
            {
                Debug.LogError(
                    $"[SurvivorsRunManager] 히트 이펙트 프리팹에 SurvivorsRunHitEffect가 없습니다. path={address}");
                ReturnPooled(address, instance);
                return null;
            }

            var playPos = followParent != null ? followParent.position : worldPosition;
            effect.Play(this, playPos, address, lifetime);
            return effect;
        }

        private async Task<GameObject> EnsureHitEffectPrefabAsync(string address)
        {
            if (_hitEffectPrefabCache.TryGetValue(address, out var cached) && cached != null)
                return cached;

            if (_hitEffectLoadInFlight.Contains(address))
            {
                // 같은 주소 로딩 중이면 잠깐 폴링 (짧은 히트 연출용)
                while (_hitEffectLoadInFlight.Contains(address) && this != null)
                {
                    if (_hitEffectPrefabCache.TryGetValue(address, out cached) && cached != null)
                        return cached;
                    await Task.Yield();
                }

                return _hitEffectPrefabCache.TryGetValue(address, out cached) ? cached : null;
            }

            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager == null)
            {
                Debug.LogError("[SurvivorsRunManager] ResourceManager가 없어 히트 이펙트를 로드할 수 없습니다.");
                return null;
            }

            _hitEffectLoadInFlight.Add(address);
            try
            {
                var prefab = await resourceManager.LoadAsync<GameObject>(address);
                if (this == null)
                    return null;

                if (prefab == null)
                {
                    Debug.LogError($"[SurvivorsRunManager] 히트 이펙트 로드 실패. path={address}");
                    return null;
                }

                _hitEffectPrefabCache[address] = prefab;
                PrewarmFromPrefab(address, prefab, PoolExpandBatch);
                return prefab;
            }
            finally
            {
                _hitEffectLoadInFlight.Remove(address);
            }
        }
    }
}
