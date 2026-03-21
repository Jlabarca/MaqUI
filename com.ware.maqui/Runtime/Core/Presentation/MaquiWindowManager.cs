using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Maqui.Core.Bridge;
using Maqui.Core.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace Maqui.Core.Presentation
{
    /// <summary>
    /// Core window management system for Maqui.
    /// Owns four persistent Canvas layers and manages window lifecycle, modal masking,
    /// freeze propagation, and optional per-asset-key object pooling.
    /// Implements IUIService for HybridFrame service locator integration.
    /// Created by CoreBootstrap — do not add to a scene manually.
    /// </summary>
    public class MaquiWindowManager : MonoBehaviour, IUIService
    {
        private readonly Dictionary<UILayer, Canvas> _layerCanvases = new();
        private Canvas _modalMaskCanvas;
        private int _modalCount;

        // All active ReactiveBaseView instances registered for freeze notification
        private readonly List<IFreezableView> _freezableViews = new();

        // Per-plugin handle tracking for cleanup-on-unload
        private readonly Dictionary<string, List<IWindowHandle>> _pluginHandles = new();

        // Window object pool
        private readonly WindowPool _pool = new();

        // Per-layer navigation stacks
        private readonly Dictionary<UILayer, Stack<IWindowHandle>> _navigationStacks = new();

        /// <inheritdoc/>
        public event Action<WindowLoadFailedEvent> WindowLoadFailed;

        /// <inheritdoc/>
        public event Action<UILayer> BackRequested;

        /// <inheritdoc/>
        public bool SuppressBackNavigation { get; set; }

        /// <summary>True when at least one Modal window is open.</summary>
        internal bool IsModalActive => _modalCount > 0;

        private void Awake()
        {
            BuildLayerCanvases();
            InitializeNavigationStacks();
        }

        private void OnDestroy()
        {
            _pool.DrainAll();
        }

        private void Update()
        {
            if (SuppressBackNavigation) return;

            bool escapePressed;
            var inputBridge = MaquiServices.Get<IInputBridge>();
            if (inputBridge != null)
            {
                escapePressed = inputBridge.GetButtonDown("Cancel");
            }
            else
            {
                try { escapePressed = Input.GetKeyDown(KeyCode.Escape); }
                catch (System.InvalidOperationException) { escapePressed = false; }
            }

            if (!escapePressed) return;

            // Find the highest non-empty layer
            UILayer? targetLayer = null;
            UILayer[] layerPriority = { UILayer.Modal, UILayer.Overlay, UILayer.Default, UILayer.Background };
            foreach (var layer in layerPriority)
            {
                if (_navigationStacks.TryGetValue(layer, out var stack) && stack.Count > 0)
                {
                    targetLayer = layer;
                    break;
                }
            }

            if (!targetLayer.HasValue) return;

            BackRequested?.Invoke(targetLayer.Value);

            // Give the topmost view a chance to consume the event
            var topHandle = _navigationStacks[targetLayer.Value].Peek();
            if (topHandle.Root != null)
            {
                var backRequestable = topHandle.Root.GetComponent<MaquiBaseView>() as IBackRequestable;
                if (backRequestable != null && backRequestable.InvokeBackRequested())
                    return; // consumed
            }

            PopWindow(targetLayer.Value);
        }

        // ── Navigation Stack ────────────────────────────────────────────────────

        private void InitializeNavigationStacks()
        {
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
                _navigationStacks[layer] = new Stack<IWindowHandle>();
        }

        private void PushToStack(IWindowHandle handle)
        {
            if (_navigationStacks.TryGetValue(handle.Layer, out var stack))
                stack.Push(handle);
        }

        private void RemoveFromStack(IWindowHandle handle)
        {
            if (!_navigationStacks.TryGetValue(handle.Layer, out var stack) || stack.Count == 0)
                return;

            if (stack.Peek() == handle)
            {
                stack.Pop();
                return;
            }

            // Handle was disposed out of order — rebuild the stack without it
            var temp = new Stack<IWindowHandle>(stack.Reverse().Where(h => h != handle));
            stack.Clear();
            foreach (var h in temp)
                stack.Push(h);
        }

        /// <inheritdoc/>
        public bool PopWindow(UILayer layer)
        {
            if (!_navigationStacks.TryGetValue(layer, out var stack) || stack.Count == 0)
                return false;

            var handle = stack.Peek(); // Dispose will call OnHandleDisposed → RemoveFromStack
            handle.Dispose();
            return true;
        }

        /// <inheritdoc/>
        public int GetStackDepth(UILayer layer)
        {
            if (_navigationStacks.TryGetValue(layer, out var stack))
                return stack.Count;
            return 0;
        }

        // ── Layer Canvas Construction ──────────────────────────────────────────

        private void BuildLayerCanvases()
        {
            CreateLayer(UILayer.Background, "Layer_Background", 0);
            CreateLayer(UILayer.Default,    "Layer_Default",    100);
            CreateLayer(UILayer.Overlay,    "Layer_Overlay",    200);
            CreateModalMask(290);
            CreateLayer(UILayer.Modal,      "Layer_Modal",      300);
        }

        private void CreateLayer(UILayer layer, string name, int sortOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();

            _layerCanvases[layer] = canvas;
        }

        private void CreateModalMask(int sortOrder)
        {
            var go = new GameObject("Layer_ModalMask");
            go.transform.SetParent(transform, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            canvas.overrideSorting = true;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Full-screen semi-transparent black image
            var imgGo = new GameObject("Mask");
            imgGo.transform.SetParent(go.transform, false);

            var rect = imgGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = imgGo.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.6f);
            img.raycastTarget = true;  // blocks clicks from reaching Default/Overlay layers

            // Raycaster on mask canvas to ensure input is consumed
            go.AddComponent<GraphicRaycaster>();

            _modalMaskCanvas = canvas;
            go.SetActive(false);  // starts hidden
        }

        // ── IUIService — non-pooled overloads (backward compatible) ───────────

        public async UniTask<IWindowHandle> ShowWindowAsync<TView, TViewModel>(
            string assetKey,
            UILayer layer,
            Action<TViewModel> configure = null,
            CancellationToken ct = default)
            where TView : ReactiveBaseView<TViewModel>
            where TViewModel : ViewModel, new()
        {
            var vm = new TViewModel();
            configure?.Invoke(vm);
            return await ShowWindowAsync<TView, TViewModel>(assetKey, layer, vm, WindowOptions.Default, ct);
        }

        public UniTask<IWindowHandle> ShowWindowAsync<TView, TViewModel>(
            string assetKey,
            UILayer layer,
            TViewModel viewModel,
            CancellationToken ct = default)
            where TView : ReactiveBaseView<TViewModel>
            where TViewModel : ViewModel
        {
            return ShowWindowAsync<TView, TViewModel>(assetKey, layer, viewModel, WindowOptions.Default, ct);
        }

        // ── IUIService — pooled overloads ─────────────────────────────────────

        public async UniTask<IWindowHandle> ShowWindowAsync<TView, TViewModel>(
            string assetKey,
            UILayer layer,
            Action<TViewModel> configure,
            WindowOptions options,
            CancellationToken ct = default)
            where TView : ReactiveBaseView<TViewModel>
            where TViewModel : ViewModel, new()
        {
            var vm = new TViewModel();
            configure?.Invoke(vm);
            return await ShowWindowAsync<TView, TViewModel>(assetKey, layer, vm, options, ct);
        }

        public async UniTask<IWindowHandle> ShowWindowAsync<TView, TViewModel>(
            string assetKey,
            UILayer layer,
            TViewModel viewModel,
            WindowOptions options,
            CancellationToken ct = default)
            where TView : ReactiveBaseView<TViewModel>
            where TViewModel : ViewModel
        {
            if (string.IsNullOrEmpty(assetKey))
            {
                var reason = "ShowWindowAsync called with null or empty assetKey.";
                Debug.LogError($"[Maqui] {reason}");
                NotifyLoadFailed(assetKey, layer, reason);
                return null;
            }

            // ── Pool hit path ────────────────────────────────────────────────
            if (options.Pooled && _pool.TryGet(assetKey, out var pooledGo))
            {
                _pool.EnsureMaxSize(assetKey, options.MaxPoolSize);

                pooledGo.SetActive(true);
                var pooledView = pooledGo.GetComponent<TView>();

                try
                {
                    await pooledView.InvokePreShowAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    // Return to pool on cancellation rather than destroying
                    _pool.Return(assetKey, pooledGo);
                    throw;
                }
                catch (Exception ex)
                {
                    var reason = $"OnPreShowAsync failed for pooled '{assetKey}': {ex.Message}";
                    Debug.LogError($"[Maqui] {reason}");
                    NotifyLoadFailed(assetKey, layer, reason, ex);
                    _pool.Return(assetKey, pooledGo);
                    return null;
                }

                pooledView.PrepareForReuse(viewModel);

                // If a modal is currently active, freeze the newly reused view
                if (IsModalActive && pooledView is IFreezableView fv)
                    fv.InvokeFreeze();

                var pooledHandle = new WindowHandle(pooledGo, layer, assetKey, pluginId: null, isPooled: true, this);
                OnWindowShown(layer);
                PushToStack(pooledHandle);
                return pooledHandle;
            }

            // ── Normal instantiation path ────────────────────────────────────
            if (options.Pooled)
                _pool.EnsureMaxSize(assetKey, options.MaxPoolSize);

            GameObject prefab;
            try
            {
                prefab = await MaquiAssetProviderBridge.Current.LoadPrefabAsync(assetKey, ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                var reason = $"Failed to load asset '{assetKey}': {ex.Message}";
                Debug.LogError($"[Maqui] {reason}");
                NotifyLoadFailed(assetKey, layer, reason, ex);
                return null;
            }

            if (prefab == null)
            {
                var reason = $"Asset not found: '{assetKey}'. Check the key and ensure the prefab exists.";
                Debug.LogError($"[Maqui] {reason}");
                NotifyLoadFailed(assetKey, layer, reason);
                return null;
            }

            ct.ThrowIfCancellationRequested();

            var layerCanvas = _layerCanvases[layer];
            var go = Instantiate(prefab, layerCanvas.transform);

            var view = go.GetComponent<TView>();
            if (view == null)
            {
                var reason = $"Prefab '{assetKey}' missing {typeof(TView).Name} component.";
                Debug.LogError($"[Maqui] {reason}");
                NotifyLoadFailed(assetKey, layer, reason);
                Destroy(go);
                return null;
            }

            try
            {
                await view.InvokePreShowAsync(ct);
            }
            catch (OperationCanceledException)
            {
                Destroy(go);
                throw;
            }
            catch (Exception ex)
            {
                var reason = $"OnPreShowAsync failed for '{assetKey}': {ex.Message}";
                Debug.LogError($"[Maqui] {reason}");
                NotifyLoadFailed(assetKey, layer, reason, ex);
                Destroy(go);
                return null;
            }

            ct.ThrowIfCancellationRequested();

            view.Initialize(viewModel);

            var handle = new WindowHandle(go, layer, assetKey, pluginId: null, isPooled: options.Pooled, this);
            OnWindowShown(layer);
            PushToStack(handle);
            return handle;
        }

        // ── IUIService — ShowPrefabAsync (unchanged, no pooling) ──────────────

        public async UniTask<IWindowHandle> ShowPrefabAsync(
            string assetKey,
            UILayer layer,
            string pluginId = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(assetKey))
            {
                var reason = "ShowPrefabAsync called with null or empty assetKey.";
                Debug.LogError($"[Maqui] {reason}");
                NotifyLoadFailed(assetKey, layer, reason);
                return null;
            }

            GameObject prefab;
            try
            {
                prefab = await MaquiAssetProviderBridge.Current.LoadPrefabAsync(assetKey, ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                var reason = $"Failed to load prefab '{assetKey}': {ex.Message}";
                Debug.LogError($"[Maqui] {reason}");
                NotifyLoadFailed(assetKey, layer, reason, ex);
                return null;
            }

            if (prefab == null)
            {
                var reason = $"Prefab not found: '{assetKey}'. Check the key and ensure the prefab exists.";
                Debug.LogError($"[Maqui] {reason}");
                NotifyLoadFailed(assetKey, layer, reason);
                return null;
            }

            ct.ThrowIfCancellationRequested();

            var layerCanvas = _layerCanvases[layer];
            var go = Instantiate(prefab, layerCanvas.transform);

            var handle = new WindowHandle(go, layer, assetKey, pluginId, isPooled: false, this);

            if (pluginId != null)
            {
                if (!_pluginHandles.TryGetValue(pluginId, out var list))
                {
                    list = new List<IWindowHandle>();
                    _pluginHandles[pluginId] = list;
                }
                list.Add(handle);
            }

            OnWindowShown(layer);
            PushToStack(handle);
            return handle;
        }

        public void CleanupPlugin(string pluginId)
        {
            if (!_pluginHandles.TryGetValue(pluginId, out var handles)) return;
            // Copy to avoid mutation during iteration
            foreach (var h in handles.ToArray())
                h.Dispose();
            _pluginHandles.Remove(pluginId);
        }

        public Canvas GetLayerCanvas(UILayer layer)
        {
            _layerCanvases.TryGetValue(layer, out var canvas);
            return canvas;
        }

        // ── Internal handle callbacks ──────────────────────────────────────────

        internal void OnHandleDisposed(WindowHandle handle)
        {
            // Remove from navigation stack
            RemoveFromStack(handle);

            // Remove from plugin tracking if applicable
            if (handle.PluginId != null &&
                _pluginHandles.TryGetValue(handle.PluginId, out var list))
            {
                list.Remove(handle);
            }

            if (handle.IsPooled && handle.Root != null)
            {
                // Reset the view's subscriptions and VM before pooling
                var view = handle.Root.GetComponent<MaquiBaseView>();
                if (view is IPoolResetable resetable)
                    resetable.InvokeResetForPool();

                if (_pool.Return(handle.AssetKey, handle.Root))
                {
                    // Successfully pooled — don't destroy
                    OnWindowHidden(handle.Layer);
                    return;
                }
                // Pool at capacity — fall through to destroy
            }

            if (handle.Root != null)
                Destroy(handle.Root);

            OnWindowHidden(handle.Layer);
        }

        // ── Layer state ────────────────────────────────────────────────────────

        private void OnWindowShown(UILayer layer)
        {
            if (layer == UILayer.Modal)
            {
                _modalCount++;
                if (_modalCount == 1)
                    ApplyFreezeState(true);
            }
        }

        private void OnWindowHidden(UILayer layer)
        {
            if (layer == UILayer.Modal)
            {
                _modalCount = Mathf.Max(0, _modalCount - 1);
                if (_modalCount == 0)
                    ApplyFreezeState(false);
            }
        }

        private void ApplyFreezeState(bool frozen)
        {
            // Toggle modal mask
            _modalMaskCanvas.gameObject.SetActive(frozen);

            // Disable/enable input on Default and Overlay layers
            SetLayerInputEnabled(UILayer.Default, !frozen);
            SetLayerInputEnabled(UILayer.Overlay, !frozen);

            // Notify all registered ReactiveBaseView instances
            foreach (var view in _freezableViews)
            {
                if (frozen) view.InvokeFreeze();
                else        view.InvokeUnfreeze();
            }
        }

        private void SetLayerInputEnabled(UILayer layer, bool enabled)
        {
            if (_layerCanvases.TryGetValue(layer, out var canvas))
            {
                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster) raycaster.enabled = enabled;
            }
        }

        // ── Error notification ────────────────────────────────────────────────

        private void NotifyLoadFailed(string assetKey, UILayer layer, string reason, Exception ex = null)
        {
            WindowLoadFailed?.Invoke(new WindowLoadFailedEvent(assetKey, layer, reason, ex));
        }

        // ── View freeze registration ───────────────────────────────────────────

        public void RegisterFreezable(IFreezableView view)
        {
            if (!_freezableViews.Contains(view))
                _freezableViews.Add(view);
        }

        public void UnregisterFreezable(IFreezableView view)
        {
            _freezableViews.Remove(view);
        }
    }

    // ── WindowHandle ───────────────────────────────────────────────────────────

    internal sealed class WindowHandle : IWindowHandle
    {
        private readonly MaquiWindowManager _manager;
        private bool _disposed;

        internal string PluginId { get; }
        internal string AssetKey { get; }
        internal bool IsPooled { get; }

        public UILayer Layer { get; }
        public GameObject Root { get; }
        public bool IsVisible => Root != null && Root.activeSelf;

        public WindowHandle(GameObject root, UILayer layer, string assetKey, string pluginId, bool isPooled, MaquiWindowManager manager)
        {
            Root     = root;
            Layer    = layer;
            AssetKey = assetKey;
            PluginId = pluginId;
            IsPooled = isPooled;
            _manager = manager;
        }

        public void Show()
        {
            if (Root) Root.SetActive(true);
        }

        public void Hide()
        {
            if (Root) Root.SetActive(false);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _manager.OnHandleDisposed(this);
        }
    }

    // ── IFreezableView ─────────────────────────────────────────────────────────

    /// <summary>
    /// Internal interface implemented by ReactiveBaseView to receive freeze notifications
    /// from MaquiWindowManager without creating a hard generic dependency.
    /// </summary>
    public interface IFreezableView
    {
        void InvokeFreeze();
        void InvokeUnfreeze();
    }

    /// <summary>
    /// Internal interface for pool reset. Implemented by ReactiveBaseView.
    /// Allows MaquiWindowManager to reset a view without knowing its generic type.
    /// </summary>
    internal interface IPoolResetable
    {
        void InvokeResetForPool();
    }

    /// <summary>
    /// Internal interface implemented by ReactiveBaseView to allow MaquiWindowManager
    /// to query whether a view wants to consume a back navigation event.
    /// </summary>
    internal interface IBackRequestable
    {
        /// <summary>Returns true if the view consumed the back event (prevents pop).</summary>
        bool InvokeBackRequested();
    }
}
