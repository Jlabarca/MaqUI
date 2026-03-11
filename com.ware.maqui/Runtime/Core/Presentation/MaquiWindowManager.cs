using System;
using System.Collections.Generic;
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
    /// and freeze propagation. Implements IUIService for HybridFrame service locator integration.
    /// Created by CoreBootstrap — do not add to a scene manually.
    /// </summary>
    public class MaquiWindowManager : MonoBehaviour, IUIService
    {
        public static MaquiWindowManager Instance { get; private set; }

        private readonly Dictionary<UILayer, Canvas> _layerCanvases = new();
        private Canvas _modalMaskCanvas;
        private int _modalCount;

        // All active ReactiveBaseView instances registered for freeze notification
        private readonly List<IFreezableView> _freezableViews = new();

        // Per-plugin handle tracking for cleanup-on-unload
        private readonly Dictionary<string, List<IWindowHandle>> _pluginHandles = new();

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BuildLayerCanvases();
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

        // ── IUIService ─────────────────────────────────────────────────────────

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
            return await ShowWindowAsync<TView, TViewModel>(assetKey, layer, vm, ct);
        }

        public async UniTask<IWindowHandle> ShowWindowAsync<TView, TViewModel>(
            string assetKey,
            UILayer layer,
            TViewModel viewModel,
            CancellationToken ct = default)
            where TView : ReactiveBaseView<TViewModel>
            where TViewModel : ViewModel
        {
            var prefab = await MaquiAssetProviderBridge.Current.LoadPrefabAsync(assetKey, ct);
            if (prefab == null)
                throw new InvalidOperationException($"[Maqui] Could not load prefab: '{assetKey}'");

            ct.ThrowIfCancellationRequested();

            var layerCanvas = _layerCanvases[layer];
            var go = Instantiate(prefab, layerCanvas.transform);

            var view = go.GetComponent<TView>();
            if (view == null)
            {
                Destroy(go);
                throw new InvalidOperationException(
                    $"[Maqui] Prefab '{assetKey}' does not have a {typeof(TView).Name} component.");
            }

            // Call OnPreShowAsync before binding — lets the view fetch async data
            await view.InvokePreShowAsync(ct);
            ct.ThrowIfCancellationRequested();

            view.Initialize(viewModel);

            var handle = new WindowHandle(go, layer, pluginId: null, this);
            OnWindowShown(layer);
            return handle;
        }

        public async UniTask<IWindowHandle> ShowPrefabAsync(
            string assetKey,
            UILayer layer,
            string pluginId = null,
            CancellationToken ct = default)
        {
            var prefab = await MaquiAssetProviderBridge.Current.LoadPrefabAsync(assetKey, ct);
            if (prefab == null)
                throw new InvalidOperationException($"[Maqui] Could not load prefab: '{assetKey}'");

            ct.ThrowIfCancellationRequested();

            var layerCanvas = _layerCanvases[layer];
            var go = Instantiate(prefab, layerCanvas.transform);

            var handle = new WindowHandle(go, layer, pluginId, this);

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
            // Remove from plugin tracking if applicable
            if (handle.PluginId != null &&
                _pluginHandles.TryGetValue(handle.PluginId, out var list))
            {
                list.Remove(handle);
            }

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

        // ── View freeze registration ───────────────────────────────────────────

        internal void RegisterFreezable(IFreezableView view)
        {
            if (!_freezableViews.Contains(view))
                _freezableViews.Add(view);
        }

        internal void UnregisterFreezable(IFreezableView view)
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

        public UILayer Layer { get; }
        public GameObject Root { get; }
        public bool IsVisible => Root != null && Root.activeSelf;

        public WindowHandle(GameObject root, UILayer layer, string pluginId, MaquiWindowManager manager)
        {
            Root     = root;
            Layer    = layer;
            PluginId = pluginId;
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
            if (Root) UnityEngine.Object.Destroy(Root);
            _manager.OnHandleDisposed(this);
        }
    }

    // ── IFreezableView ─────────────────────────────────────────────────────────

    /// <summary>
    /// Internal interface implemented by ReactiveBaseView to receive freeze notifications
    /// from MaquiWindowManager without creating a hard generic dependency.
    /// </summary>
    internal interface IFreezableView
    {
        void InvokeFreeze();
        void InvokeUnfreeze();
    }
}
