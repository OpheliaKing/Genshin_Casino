using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SHIN
{
    /// <summary>
    /// Addressables Load / Instantiate / Release 진입점.
    /// </summary>
    public class ResourceManager : ManagerBase
    {
        private readonly Dictionary<string, AsyncOperationHandle> _assetHandles = new();
        private readonly Dictionary<string, AsyncOperationHandle> _labelHandles = new();

        public async Task<T> LoadAsync<T>(string address) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError("[ResourceManager] address가 비어 있습니다.");
                return null;
            }

            if (_assetHandles.TryGetValue(address, out var cached) && cached.IsValid())
            {
                if (!cached.IsDone)
                    await cached.Task;

                return cached.Status == AsyncOperationStatus.Succeeded ? cached.Result as T : null;
            }

            var handle = Addressables.LoadAssetAsync<T>(address);
            _assetHandles[address] = handle;
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[ResourceManager] 로드 실패: {address}");
                _assetHandles.Remove(address);
                return null;
            }

            return handle.Result;
        }

        public void LoadAsync<T>(string address, Action<T> onComplete) where T : UnityEngine.Object
        {
            LoadInternalAsync(address, onComplete);
        }

        private async void LoadInternalAsync<T>(string address, Action<T> onComplete) where T : UnityEngine.Object
        {
            var result = await LoadAsync<T>(address);
            onComplete?.Invoke(result);
        }

        public async Task<GameObject> InstantiateAsync(
            string address,
            Transform parent = null,
            bool startInactive = true)
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError("[ResourceManager] address가 비어 있습니다.");
                return null;
            }

            var handle = Addressables.InstantiateAsync(address, parent);
            var instance = await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded || instance == null)
            {
                Debug.LogError($"[ResourceManager] 생성 실패: {address}");
                return null;
            }

            if (startInactive)
                instance.SetActive(false);

            return instance;
        }

        public void InstantiateAsync(
            string address,
            Transform parent,
            Action<GameObject> onComplete,
            bool startInactive = true)
        {
            InstantiateInternalAsync(address, parent, onComplete, startInactive);
        }

        private async void InstantiateInternalAsync(
            string address,
            Transform parent,
            Action<GameObject> onComplete,
            bool startInactive)
        {
            var instance = await InstantiateAsync(address, parent, startInactive);
            onComplete?.Invoke(instance);
        }

        /// <summary>
        /// 라벨에 붙은 에셋을 한 번에 프리로드한다. 씬/전투 진입 전 호출.
        /// </summary>
        public async Task PreloadLabelAsync(string label)
        {
            if (string.IsNullOrEmpty(label))
                return;

            if (_labelHandles.TryGetValue(label, out var existing) && existing.IsValid())
            {
                if (!existing.IsDone)
                    await existing.Task;
                return;
            }

            var handle = Addressables.LoadAssetsAsync<UnityEngine.Object>(label, null);
            _labelHandles[label] = handle;
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[ResourceManager] 라벨 프리로드 실패: {label}");
                _labelHandles.Remove(label);
            }
        }

        public void Release(string address)
        {
            if (!_assetHandles.TryGetValue(address, out var handle))
                return;

            if (handle.IsValid())
                Addressables.Release(handle);

            _assetHandles.Remove(address);
        }

        public void ReleaseInstance(GameObject instance)
        {
            if (instance == null)
                return;

            Addressables.ReleaseInstance(instance);
        }

        public void ReleaseLabel(string label)
        {
            if (!_labelHandles.TryGetValue(label, out var handle))
                return;

            if (handle.IsValid())
                Addressables.Release(handle);

            _labelHandles.Remove(label);
        }

        public void ReleaseAllAssets()
        {
            foreach (var handle in _assetHandles.Values)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
            }

            _assetHandles.Clear();
        }

        public void ReleaseAllLabels()
        {
            foreach (var handle in _labelHandles.Values)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
            }

            _labelHandles.Clear();
        }

        private void OnDestroy()
        {
            ReleaseAllLabels();
            ReleaseAllAssets();
        }
    }
}
