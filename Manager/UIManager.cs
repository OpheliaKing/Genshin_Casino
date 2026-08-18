using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// UIBase 스택 + Addressables UI 로드/해제.
    /// </summary>
    public class UIManager : ManagerBase
    {
        [SerializeField] private Transform _uiRoot;

        private readonly Stack<UIStackEntry> _uiStack = new();
        private readonly Dictionary<string, GameObject> _cachedInstances = new();

        public UIBase Current => _uiStack.Count > 0 ? _uiStack.Peek().UI : null;
        public int Count => _uiStack.Count;

        public async Task PreloadInGameUIAsync()
        {
            var resourceManager = ResolveResourceManager();
            if (resourceManager == null)
                return;

            await resourceManager.PreloadLabelAsync(PublicVariable.Label.Preload);
            await resourceManager.PreloadLabelAsync(PublicVariable.Label.InGame);
        }

        public void Show(string address)
        {
            Show(address, null);
        }

        public void Show(string address, Action<UIBase> onComplete)
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError("[UIManager] address가 비어 있습니다.");
                return;
            }

            if (_cachedInstances.TryGetValue(address, out var cached) && cached != null)
            {
                if (TryGetUIBase(cached, out var cachedUi))
                {
                    OpenLoaded(address, cachedUi, cached);
                    onComplete?.Invoke(cachedUi);
                    return;
                }

                _cachedInstances.Remove(address);
            }

            var parent = ResolveUIRoot();
            if (parent == null)
                return;

            var resourceManager = ResolveResourceManager();
            if (resourceManager == null)
                return;

            resourceManager.InstantiateAsync(address, parent, go =>
            {
                if (go == null)
                {
                    Debug.LogError($"[UIManager] UI 생성 실패: {address}");
                    return;
                }

                if (!TryGetUIBase(go, out var ui))
                {
                    resourceManager.ReleaseInstance(go);
                    Debug.LogError($"[UIManager] UIBase가 없습니다: {address}");
                    return;
                }

                CacheIfNeeded(address, ui, go);
                OpenLoaded(address, ui, go);
                onComplete?.Invoke(ui);
            });
        }

        /// <summary>씬에 이미 배치된 UI를 스택에 올린다.</summary>
        public void Open(UIBase ui)
        {
            if (ui == null || _uiStack.Exists(entry => entry.UI == ui))
                return;

            if (ui.UIType == UIType.FullScreen)
                DeactivateAll();

            PushEntry(new UIStackEntry(null, ui, ui.gameObject, false));
            ShowEntry(_uiStack.Peek());
        }

        public void Close()
        {
            if (_uiStack.Count == 0)
                return;

            var entry = _uiStack.Pop();
            HideEntry(entry);
            ReleaseEntry(entry);
            RestoreVisibleStack();
        }

        public void Close(UIBase ui)
        {
            if (ui == null)
                return;

            if (_uiStack.Count > 0 && _uiStack.Peek().UI == ui)
            {
                Close();
                return;
            }

            var temp = new Stack<UIStackEntry>();
            while (_uiStack.Count > 0)
            {
                var current = _uiStack.Pop();
                if (current.UI == ui)
                {
                    HideEntry(current);
                    ReleaseEntry(current);
                    break;
                }

                temp.Push(current);
            }

            while (temp.Count > 0)
                _uiStack.Push(temp.Pop());

            RestoreVisibleStack();
        }

        public void CloseAll()
        {
            while (_uiStack.Count > 0)
            {
                var entry = _uiStack.Pop();
                HideEntry(entry);
                ReleaseEntry(entry, forceRelease: true);
            }

            _cachedInstances.Clear();
        }

        private void OpenLoaded(string address, UIBase ui, GameObject go)
        {
            if (ui.UIType == UIType.FullScreen)
                DeactivateAll();

            PushEntry(new UIStackEntry(address, ui, go, true));
            ShowEntry(_uiStack.Peek());
        }

        private void PushEntry(UIStackEntry entry)
        {
            _uiStack.Push(entry);
        }

        private void DeactivateAll()
        {
            foreach (var entry in _uiStack)
                HideEntry(entry);
        }

        private void RestoreVisibleStack()
        {
            if (_uiStack.Count == 0)
                return;

            var stackArray = _uiStack.ToArray();
            var topFullScreenIndex = -1;

            for (var i = 0; i < stackArray.Length; i++)
            {
                if (stackArray[i].UI.UIType != UIType.FullScreen)
                    continue;

                topFullScreenIndex = i;
                break;
            }

            var activateUntil = topFullScreenIndex >= 0 ? topFullScreenIndex : stackArray.Length - 1;

            for (var i = 0; i < stackArray.Length; i++)
            {
                if (i <= activateUntil)
                    ShowEntry(stackArray[i]);
                else
                    HideEntry(stackArray[i]);
            }
        }

        private static void ShowEntry(UIStackEntry entry)
        {
            if (!entry.GameObject.activeSelf)
                entry.GameObject.SetActive(true);

            entry.UI.OnShow();
        }

        private static void HideEntry(UIStackEntry entry)
        {
            entry.UI.OnHide();

            if (entry.GameObject.activeSelf)
                entry.GameObject.SetActive(false);
        }

        private void ReleaseEntry(UIStackEntry entry, bool forceRelease = false)
        {
            var shouldRelease = forceRelease || entry.ShouldReleaseOnClose;

            if (entry.IsAddressable && shouldRelease)
            {
                if (!string.IsNullOrEmpty(entry.Address))
                    _cachedInstances.Remove(entry.Address);

                ResolveResourceManager()?.ReleaseInstance(entry.GameObject);
                return;
            }

            if (entry.IsAddressable && !string.IsNullOrEmpty(entry.Address))
                _cachedInstances[entry.Address] = entry.GameObject;
        }

        private void CacheIfNeeded(string address, UIBase ui, GameObject go)
        {
            if (!ui.ShouldReleaseOnClose)
                _cachedInstances[address] = go;
        }

        private static ResourceManager ResolveResourceManager()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("[UIManager] GameManager.Instance가 없습니다.");
                return null;
            }

            return GameManager.Instance.ResourceManager;
        }

        private Transform ResolveUIRoot()
        {
            if (_uiRoot != null)
                return _uiRoot;

            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[UIManager] Canvas를 찾을 수 없습니다.");
                return null;
            }

            return canvas.transform;
        }

        private static bool TryGetUIBase(GameObject go, out UIBase ui)
        {
            ui = go.GetComponent<UIBase>() ?? go.GetComponentInChildren<UIBase>(true);
            return ui != null;
        }

        private void OnDestroy()
        {
            CloseAll();
        }

        private sealed class UIStackEntry
        {
            public readonly string Address;
            public readonly UIBase UI;
            public readonly GameObject GameObject;
            public readonly bool IsAddressable;

            public bool ShouldReleaseOnClose => UI != null && UI.ShouldReleaseOnClose;

            public UIStackEntry(string address, UIBase ui, GameObject gameObject, bool isAddressable)
            {
                Address = address;
                UI = ui;
                GameObject = gameObject;
                IsAddressable = isAddressable;
            }
        }
    }

    internal static class StackExtensions
    {
        public static bool Exists<T>(this Stack<T> stack, Func<T, bool> predicate)
        {
            foreach (var item in stack)
            {
                if (predicate(item))
                    return true;
            }

            return false;
        }
    }
}
