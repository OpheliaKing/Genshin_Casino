using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// Address/키 단위 오브젝트 풀. 프로젝타일·몬스터·히트 이펙트 등이 공유한다.
    /// 빌릴 때 비어 있으면 <see cref="_poolExpandBatch"/>개씩 만들어 채운다.
    /// </summary>
    public partial class SurvivorsRunManager
    {
        [SerializeField]
        [Tooltip("풀이 비었을 때 한 번에 추가로 개수.")]
        private int _poolExpandBatch = 8;

        [SerializeField]
        [Tooltip("풀 보관용 루트. 비우면 런타임에 PoolRoot 생성.")]
        private Transform _poolRoot;

        private readonly Dictionary<string, Queue<GameObject>> _pools = new();
        private readonly Dictionary<string, List<GameObject>> _poolCreated = new();
        private readonly Dictionary<GameObject, string> _instanceToPoolKey = new();
        private readonly HashSet<GameObject> _pooledAvailable = new();

        public int PoolExpandBatch => Mathf.Max(1, _poolExpandBatch);

        private void EnsurePoolRoot()
        {
            if (_poolRoot != null)
                return;

            var existing = transform.Find("PoolRoot");
            if (existing != null)
            {
                _poolRoot = existing;
                return;
            }

            var go = new GameObject("PoolRoot");
            go.transform.SetParent(transform, false);
            go.SetActive(false);
            _poolRoot = go.transform;
        }

        /// <summary>
        /// 이미 로드된 프리팹으로 대여. 발사 틱 등 동기 경로용.
        /// </summary>
        public GameObject RentFromPrefab(
            string poolKey,
            GameObject prefab,
            Transform activeParent = null,
            int expandBatch = -1,
            bool activate = true)
        {
            if (string.IsNullOrWhiteSpace(poolKey) || prefab == null)
                return null;

            var key = poolKey.Trim();
            EnsurePoolRoot();
            var queue = GetOrCreateQueue(key);

            if (queue.Count == 0)
                ExpandFromPrefab(key, prefab, expandBatch > 0 ? expandBatch : PoolExpandBatch);

            if (queue.Count == 0)
                return null;

            var instance = DequeueValid(queue, key);
            if (instance == null)
                return null;

            PrepareRentedInstance(instance, activeParent, activate);
            return instance;
        }

        /// <summary>
        /// Addressables 주소로 대여. 몬스터·이펙트 등 비동기 스폰용.
        /// </summary>
        public async Task<GameObject> RentAsync(
            string poolKeyOrAddress,
            Transform activeParent = null,
            int expandBatch = -1,
            bool activate = true)
        {
            if (string.IsNullOrWhiteSpace(poolKeyOrAddress))
                return null;

            var key = poolKeyOrAddress.Trim();
            EnsurePoolRoot();
            var queue = GetOrCreateQueue(key);

            if (queue.Count == 0)
                await ExpandFromAddressAsync(key, expandBatch > 0 ? expandBatch : PoolExpandBatch);

            if (this == null)
                return null;

            if (queue.Count == 0)
                return null;

            var instance = DequeueValid(queue, key);
            if (instance == null)
                return null;

            PrepareRentedInstance(instance, activeParent, activate);
            return instance;
        }

        /// <summary>사용 끝난 인스턴스를 풀에 반환한다. Destroy 하지 않는다.</summary>
        public void ReturnPooled(string poolKey, GameObject instance)
        {
            if (instance == null || string.IsNullOrWhiteSpace(poolKey))
                return;

            // 이미 대기열에 있으면 이중 enqueue 방지 (사망 Prune 중복 등).
            if (_pooledAvailable.Contains(instance))
                return;

            var key = poolKey.Trim();
            EnsurePoolRoot();

            instance.SetActive(false);
            instance.transform.SetParent(_poolRoot, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;

            _instanceToPoolKey[instance] = key;
            _pooledAvailable.Add(instance);
            GetOrCreateQueue(key).Enqueue(instance);
            TrackCreated(key, instance);
        }

        /// <summary>인스턴스에 묶인 키로 반환. 키를 모를 때.</summary>
        public bool TryReturnPooled(GameObject instance)
        {
            if (instance == null)
                return false;

            if (!_instanceToPoolKey.TryGetValue(instance, out var key) || string.IsNullOrEmpty(key))
                return false;

            ReturnPooled(key, instance);
            return true;
        }

        /// <summary>지정 키 풀을 미리 채운다.</summary>
        public void PrewarmFromPrefab(string poolKey, GameObject prefab, int count)
        {
            if (string.IsNullOrWhiteSpace(poolKey) || prefab == null || count <= 0)
                return;

            var key = poolKey.Trim();
            EnsurePoolRoot();
            ExpandFromPrefab(key, prefab, count);
        }

        public async Task PrewarmAsync(string address, int count)
        {
            if (string.IsNullOrWhiteSpace(address) || count <= 0)
                return;

            var key = address.Trim();
            EnsurePoolRoot();
            await ExpandFromAddressAsync(key, count);
        }

        public void ClearAllPools()
        {
            foreach (var pair in _poolCreated)
            {
                var list = pair.Value;
                if (list == null)
                    continue;

                for (var i = 0; i < list.Count; i++)
                {
                    var go = list[i];
                    if (go == null)
                        continue;

                    _instanceToPoolKey.Remove(go);
                    Destroy(go);
                }

                list.Clear();
            }

            _poolCreated.Clear();
            _pools.Clear();
            _instanceToPoolKey.Clear();
            _pooledAvailable.Clear();
        }

        private Queue<GameObject> GetOrCreateQueue(string key)
        {
            if (_pools.TryGetValue(key, out var queue))
                return queue;

            queue = new Queue<GameObject>();
            _pools[key] = queue;
            return queue;
        }

        private void ExpandFromPrefab(string key, GameObject prefab, int count)
        {
            var queue = GetOrCreateQueue(key);
            for (var i = 0; i < count; i++)
            {
                var instance = Object.Instantiate(prefab, _poolRoot);
                instance.SetActive(false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                _instanceToPoolKey[instance] = key;
                _pooledAvailable.Add(instance);
                TrackCreated(key, instance);
                queue.Enqueue(instance);
            }
        }

        private async Task ExpandFromAddressAsync(string address, int count)
        {
            var resourceManager = GameManager.Instance?.ResourceManager;
            if (resourceManager == null)
            {
                Debug.LogError("[SurvivorsRunManager.Pool] ResourceManager가 없어 풀을 확장할 수 없습니다.");
                return;
            }

            var queue = GetOrCreateQueue(address);
            for (var i = 0; i < count; i++)
            {
                var instance = await resourceManager.InstantiateAsync(
                    address,
                    parent: _poolRoot,
                    startInactive: true);

                if (this == null)
                {
                    if (instance != null)
                        resourceManager.ReleaseInstance(instance);
                    return;
                }

                if (instance == null)
                {
                    Debug.LogError($"[SurvivorsRunManager.Pool] 풀 확장 실패: {address}");
                    continue;
                }

                instance.transform.SetParent(_poolRoot, false);
                instance.SetActive(false);
                _instanceToPoolKey[instance] = address;
                _pooledAvailable.Add(instance);
                TrackCreated(address, instance);
                queue.Enqueue(instance);
            }
        }

        private GameObject DequeueValid(Queue<GameObject> queue, string key)
        {
            while (queue.Count > 0)
            {
                var instance = queue.Dequeue();
                if (instance == null)
                    continue;

                _pooledAvailable.Remove(instance);
                _instanceToPoolKey[instance] = key;
                return instance;
            }

            return null;
        }

        private void PrepareRentedInstance(GameObject instance, Transform activeParent, bool activate)
        {
            if (activeParent != null)
                instance.transform.SetParent(activeParent, false);
            else
                instance.transform.SetParent(transform, false);

            if (activate)
                instance.SetActive(true);
            else
                instance.SetActive(false);
        }

        private void TrackCreated(string key, GameObject instance)
        {
            if (!_poolCreated.TryGetValue(key, out var list))
            {
                list = new List<GameObject>();
                _poolCreated[key] = list;
            }

            if (!list.Contains(instance))
                list.Add(instance);
        }
    }
}
