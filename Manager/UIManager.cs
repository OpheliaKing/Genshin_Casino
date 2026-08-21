using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// UIBase 스택 + Addressables UI 로드/해제 + 범용 Fade.
    /// </summary>
    public class UIManager : ManagerBase
    {
        [SerializeField] private Transform _uiRoot;

        private readonly Stack<UIStackEntry> _uiStack = new();
        private readonly Dictionary<string, GameObject> _cachedInstances = new();
        private FadeUI _fadeUI;
        private Task<FadeUI> _fadeEnsureTask;

        public UIBase Current => _uiStack.Count > 0 ? _uiStack.Peek().UI : null;
        public int Count => _uiStack.Count;

        public async Task PreloadInGameUIAsync()
        {
            var resourceManager = ResolveResourceManager();
            if (resourceManager == null)
                return;

            await resourceManager.PreloadLabelAsync(PublicVariable.Label.Preload);
            await resourceManager.PreloadLabelAsync(PublicVariable.Label.InGame);
            await CardObject.PreloadSpritesAsync();
            await GameResultUI.PreloadSpritesAsync();
            await EnsureFadeUIAsync();
        }

        public Task FadeOutAsync(float duration = -1f)
        {
            return FadeToAsync(1f, duration);
        }

        public Task FadeInAsync(float duration = -1f)
        {
            return FadeToAsync(0f, duration);
        }

        public async Task FadeToAsync(float targetAlpha, float duration = -1f)
        {
            var fade = await EnsureFadeUIAsync();
            if (fade == null)
                return;

            await fade.FadeToAsync(targetAlpha, duration);
        }

        /// <summary>
        /// 페이드 아웃 → midAction → 페이드 인. 화면 전환용 범용 API.
        /// </summary>
        public async Task FadeTransitionAsync(Func<Task> midAction, float outDuration = -1f, float inDuration = -1f)
        {
            var fade = await EnsureFadeUIAsync();
            await FadeOutAsync(outDuration);

            if (fade != null)
            {
                fade.BringToFront();
                fade.SetAlphaImmediate(1f);
            }

            if (midAction != null)
                await midAction();

            if (fade != null)
                fade.BringToFront();

            await FadeInAsync(inDuration);

            if (fade != null)
                fade.SetAlphaImmediate(0f);
        }

        public async Task FadeTransitionAsync(Action midAction, float outDuration = -1f, float inDuration = -1f)
        {
            await FadeTransitionAsync(() =>
            {
                midAction?.Invoke();
                return Task.CompletedTask;
            }, outDuration, inDuration);
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
            Close(restoreVisibleStack: true);
        }

        public void Close(bool restoreVisibleStack)
        {
            if (_uiStack.Count == 0)
                return;

            var entry = _uiStack.Pop();
            HideEntry(entry);
            ReleaseEntry(entry);

            if (restoreVisibleStack)
                RestoreVisibleStack();
        }

        public void Close(UIBase ui)
        {
            Close(ui, restoreVisibleStack: true);
        }

        public void Close(UIBase ui, bool restoreVisibleStack)
        {
            if (ui == null)
                return;

            if (_uiStack.Count > 0 && _uiStack.Peek().UI == ui)
            {
                Close(restoreVisibleStack);
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

            if (restoreVisibleStack)
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

        private async Task<FadeUI> EnsureFadeUIAsync()
        {
            if (_fadeUI != null)
            {
                _fadeUI.BringToFront();
                return _fadeUI;
            }

            if (_fadeEnsureTask != null)
                return await _fadeEnsureTask;

            _fadeEnsureTask = CreateFadeUIAsync();
            try
            {
                return await _fadeEnsureTask;
            }
            finally
            {
                _fadeEnsureTask = null;
            }
        }

        private async Task<FadeUI> CreateFadeUIAsync()
        {
            var parent = ResolveUIRoot();
            var resourceManager = ResolveResourceManager();
            if (parent == null || resourceManager == null)
                return null;

            var go = await resourceManager.InstantiateAsync(
                PublicVariable.Address.FadeUI,
                parent,
                startInactive: false);

            if (go == null)
            {
                Debug.LogError("[UIManager] FadeUI 생성 실패");
                return null;
            }

            _fadeUI = go.GetComponent<FadeUI>() ?? go.AddComponent<FadeUI>();
            _fadeUI.SetAlphaImmediate(0f);
            _fadeUI.BringToFront();
            return _fadeUI;
        }

        private void OpenLoaded(string address, UIBase ui, GameObject go)
        {
            if (ui.UIType == UIType.FullScreen)
                DeactivateAll();

            // FadeUI 하위에 잘못 붙었으면 루트로 교정
            var root = ResolveUIRoot();
            if (root != null && go.transform.parent != root)
                go.transform.SetParent(root, false);

            _fadeUI?.BringToFront();

            PushEntry(new UIStackEntry(address, ui, go, true));
            ShowEntry(_uiStack.Peek());
            _fadeUI?.BringToFront();
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

            _fadeUI?.BringToFront();
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

            // FadeUI 등 nested Canvas가 있어도 항상 루트 Canvas만 쓴다.
            var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Canvas rootCanvas = null;
            for (var i = 0; i < canvases.Length; i++)
            {
                var canvas = canvases[i];
                if (canvas == null)
                    continue;

                // FadeUI 오버레이 Canvas는 UI 루트로 쓰지 않음
                if (canvas.GetComponent<FadeUI>() != null)
                    continue;

                if (canvas.isRootCanvas)
                {
                    rootCanvas = canvas;
                    break;
                }

                if (rootCanvas == null)
                    rootCanvas = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
            }

            if (rootCanvas == null)
            {
                Debug.LogError("[UIManager] Canvas를 찾을 수 없습니다.");
                return null;
            }

            _uiRoot = rootCanvas.transform;
            return _uiRoot;
        }

        private static bool TryGetUIBase(GameObject go, out UIBase ui)
        {
            ui = go.GetComponent<UIBase>() ?? go.GetComponentInChildren<UIBase>(true);
            return ui != null;
        }

        private void OnDestroy()
        {
            CloseAll();
            _fadeUI = null;
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
