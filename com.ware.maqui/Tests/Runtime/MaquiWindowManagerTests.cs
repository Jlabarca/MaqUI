using System.Collections;
using NUnit.Framework;
using Maqui.Core;
using Maqui.Core.Presentation;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Maqui.Tests
{
    public class MaquiWindowManagerTests
    {
        private GameObject _managerGo;
        private MaquiWindowManager _manager;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            MaquiServices.Reset();

            _managerGo = new GameObject("TestMaquiWindowManager");
            _manager = _managerGo.AddComponent<MaquiWindowManager>();
            MaquiServices.Register<IUIService>(_manager);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_managerGo != null)
            {
                Object.DestroyImmediate(_managerGo);
                yield return null;
            }
            MaquiServices.Reset();
        }

        [UnityTest]
        public IEnumerator GetLayerCanvas_ReturnsCanvasForEachLayer()
        {
            yield return null;

            Assert.IsNotNull(_manager.GetLayerCanvas(UILayer.Background));
            Assert.IsNotNull(_manager.GetLayerCanvas(UILayer.Default));
            Assert.IsNotNull(_manager.GetLayerCanvas(UILayer.Overlay));
            Assert.IsNotNull(_manager.GetLayerCanvas(UILayer.Modal));
        }

        [UnityTest]
        public IEnumerator LayerCanvases_HaveCorrectSortOrder()
        {
            yield return null;

            Assert.AreEqual(0,   _manager.GetLayerCanvas(UILayer.Background).sortingOrder);
            Assert.AreEqual(100, _manager.GetLayerCanvas(UILayer.Default).sortingOrder);
            Assert.AreEqual(200, _manager.GetLayerCanvas(UILayer.Overlay).sortingOrder);
            Assert.AreEqual(300, _manager.GetLayerCanvas(UILayer.Modal).sortingOrder);
        }

        [UnityTest]
        public IEnumerator ModalMask_StartsHidden()
        {
            yield return null;

            var mask = _managerGo.transform.Find("Layer_ModalMask");
            Assert.IsNotNull(mask, "Layer_ModalMask should exist");
            Assert.IsFalse(mask.gameObject.activeSelf, "Modal mask should start hidden");
        }

        [UnityTest]
        public IEnumerator LayerCanvases_HaveRequiredComponents()
        {
            yield return null;

            foreach (UILayer layer in System.Enum.GetValues(typeof(UILayer)))
            {
                var canvas = _manager.GetLayerCanvas(layer);
                Assert.IsNotNull(canvas, $"Canvas for {layer} should exist");
                Assert.IsNotNull(canvas.GetComponent<CanvasScaler>(), $"{layer} should have CanvasScaler");
                Assert.IsNotNull(canvas.GetComponent<GraphicRaycaster>(), $"{layer} should have GraphicRaycaster");
            }
        }

        [UnityTest]
        public IEnumerator ShowWindowAsync_NullKey_ReturnsNull()
        {
            yield return null;

            LogAssert.Expect(LogType.Error, "[Maqui] ShowWindowAsync called with null or empty assetKey.");

            var task = _manager.ShowWindowAsync<TestView, TestVM>(
                null, UILayer.Default, new TestVM());

            Assert.IsTrue(task.Status == Cysharp.Threading.Tasks.UniTaskStatus.Succeeded);
            Assert.IsNull(task.GetAwaiter().GetResult());
        }

        [UnityTest]
        public IEnumerator ShowWindowAsync_EmptyKey_ReturnsNull()
        {
            yield return null;

            LogAssert.Expect(LogType.Error, "[Maqui] ShowWindowAsync called with null or empty assetKey.");

            var task = _manager.ShowWindowAsync<TestView, TestVM>(
                "", UILayer.Default, new TestVM());

            Assert.IsTrue(task.Status == Cysharp.Threading.Tasks.UniTaskStatus.Succeeded);
            Assert.IsNull(task.GetAwaiter().GetResult());
        }

        [UnityTest]
        public IEnumerator ShowPrefabAsync_NullKey_ReturnsNull()
        {
            yield return null;

            LogAssert.Expect(LogType.Error, "[Maqui] ShowPrefabAsync called with null or empty assetKey.");

            var task = _manager.ShowPrefabAsync(null, UILayer.Default);

            Assert.IsTrue(task.Status == Cysharp.Threading.Tasks.UniTaskStatus.Succeeded);
            Assert.IsNull(task.GetAwaiter().GetResult());
        }

        [UnityTest]
        public IEnumerator CleanupPlugin_NoHandles_DoesNotThrow()
        {
            yield return null;
            Assert.DoesNotThrow(() => _manager.CleanupPlugin("nonexistent.plugin"));
        }

        // ── Modal Freeze / Unfreeze ──────────────────────────────────────────

        [UnityTest]
        public IEnumerator ModalWindow_ShowAndDispose_ActivatesAndDeactivatesMask()
        {
            yield return null;

            var mask = _managerGo.transform.Find("Layer_ModalMask");
            Assert.IsFalse(mask.gameObject.activeSelf, "Mask should start hidden");

            var modalCanvas = _manager.GetLayerCanvas(UILayer.Modal);
            var windowGo = new GameObject("ModalWindow");
            windowGo.transform.SetParent(modalCanvas.transform, false);

            var prefab = new GameObject("ModalPrefab");
            var mockProvider = new MockAssetProvider(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(mockProvider);

            var task = _manager.ShowPrefabAsync("test/modal", UILayer.Modal);
            var handle = task.GetAwaiter().GetResult();

            Assert.IsNotNull(handle, "Handle should not be null");
            Assert.IsTrue(mask.gameObject.activeSelf, "Mask should be visible when modal is shown");

            handle.Dispose();
            yield return null;

            Assert.IsFalse(mask.gameObject.activeSelf, "Mask should hide when last modal is disposed");

            Object.DestroyImmediate(windowGo);
            Object.DestroyImmediate(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);
        }

        [UnityTest]
        public IEnumerator ModalWindow_FreezesPropagated_ToRegisteredViews()
        {
            yield return null;

            var viewGo = new GameObject("FreezableView");
            var view = viewGo.AddComponent<FreezeTrackingView>();
            yield return null;

            _manager.RegisterFreezable(view);

            Assert.IsFalse(view.IsFrozen, "View should start unfrozen");

            var prefab = new GameObject("ModalPrefab");
            var mockProvider = new MockAssetProvider(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(mockProvider);

            var task = _manager.ShowPrefabAsync("test/modal", UILayer.Modal);
            var handle = task.GetAwaiter().GetResult();

            Assert.IsTrue(view.IsFrozen, "View should be frozen when modal is shown");

            handle.Dispose();
            yield return null;

            Assert.IsFalse(view.IsFrozen, "View should be unfrozen when modal is disposed");

            _manager.UnregisterFreezable(view);
            Object.DestroyImmediate(viewGo);
            Object.DestroyImmediate(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);
        }

        [UnityTest]
        public IEnumerator ModalWindow_MultipleModals_MaskStaysUntilLastDisposed()
        {
            yield return null;

            var mask = _managerGo.transform.Find("Layer_ModalMask");

            var prefab = new GameObject("ModalPrefab");
            var mockProvider = new MockAssetProvider(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(mockProvider);

            var handle1 = _manager.ShowPrefabAsync("test/modal1", UILayer.Modal).GetAwaiter().GetResult();
            var handle2 = _manager.ShowPrefabAsync("test/modal2", UILayer.Modal).GetAwaiter().GetResult();

            Assert.IsTrue(mask.gameObject.activeSelf, "Mask should be visible with 2 modals");

            handle1.Dispose();
            yield return null;
            Assert.IsTrue(mask.gameObject.activeSelf, "Mask should still be visible with 1 modal remaining");

            handle2.Dispose();
            yield return null;
            Assert.IsFalse(mask.gameObject.activeSelf, "Mask should hide when last modal is disposed");

            Object.DestroyImmediate(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);
        }

        [UnityTest]
        public IEnumerator ModalWindow_DisablesDefaultAndOverlayRaycasters()
        {
            yield return null;

            var defaultRaycaster = _manager.GetLayerCanvas(UILayer.Default).GetComponent<GraphicRaycaster>();
            var overlayRaycaster = _manager.GetLayerCanvas(UILayer.Overlay).GetComponent<GraphicRaycaster>();

            Assert.IsTrue(defaultRaycaster.enabled, "Default raycaster should start enabled");
            Assert.IsTrue(overlayRaycaster.enabled, "Overlay raycaster should start enabled");

            var prefab = new GameObject("ModalPrefab");
            var mockProvider = new MockAssetProvider(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(mockProvider);

            var handle = _manager.ShowPrefabAsync("test/modal", UILayer.Modal).GetAwaiter().GetResult();

            Assert.IsFalse(defaultRaycaster.enabled, "Default raycaster should be disabled when modal is shown");
            Assert.IsFalse(overlayRaycaster.enabled, "Overlay raycaster should be disabled when modal is shown");

            handle.Dispose();
            yield return null;

            Assert.IsTrue(defaultRaycaster.enabled, "Default raycaster should re-enable after modal dispose");
            Assert.IsTrue(overlayRaycaster.enabled, "Overlay raycaster should re-enable after modal dispose");

            Object.DestroyImmediate(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);
        }

        // ── WindowHandle Contract ────────────────────────────────────────────

        [UnityTest]
        public IEnumerator WindowHandle_Show_ActivatesRoot()
        {
            yield return null;

            var prefab = new GameObject("Prefab");
            var mockProvider = new MockAssetProvider(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(mockProvider);

            var handle = _manager.ShowPrefabAsync("test/window", UILayer.Default).GetAwaiter().GetResult();
            Assert.IsTrue(handle.IsVisible);

            handle.Hide();
            Assert.IsFalse(handle.IsVisible);

            handle.Show();
            Assert.IsTrue(handle.IsVisible);

            handle.Dispose();
            Object.DestroyImmediate(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);
        }

        [UnityTest]
        public IEnumerator WindowHandle_Dispose_DestroysRoot()
        {
            yield return null;

            var prefab = new GameObject("Prefab");
            var mockProvider = new MockAssetProvider(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(mockProvider);

            var handle = _manager.ShowPrefabAsync("test/window", UILayer.Default).GetAwaiter().GetResult();
            var root = handle.Root;
            Assert.IsNotNull(root);

            handle.Dispose();
            yield return null;

            Assert.IsTrue(root == null, "Root should be destroyed after Dispose");

            Object.DestroyImmediate(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);
        }

        [UnityTest]
        public IEnumerator WindowHandle_DoubleDispose_DoesNotThrow()
        {
            yield return null;

            var prefab = new GameObject("Prefab");
            var mockProvider = new MockAssetProvider(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(mockProvider);

            var handle = _manager.ShowPrefabAsync("test/window", UILayer.Default).GetAwaiter().GetResult();

            Assert.DoesNotThrow(() =>
            {
                handle.Dispose();
                handle.Dispose(); // second dispose should be a no-op
            });

            Object.DestroyImmediate(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);
        }

        [UnityTest]
        public IEnumerator WindowHandle_Layer_MatchesRequestedLayer()
        {
            yield return null;

            var prefab = new GameObject("Prefab");
            var mockProvider = new MockAssetProvider(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(mockProvider);

            var handle = _manager.ShowPrefabAsync("test/window", UILayer.Overlay).GetAwaiter().GetResult();
            Assert.AreEqual(UILayer.Overlay, handle.Layer);

            handle.Dispose();
            Object.DestroyImmediate(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);
        }

        // ── Plugin Cleanup ───────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator CleanupPlugin_DestroysAllPluginWindows()
        {
            yield return null;

            var prefab = new GameObject("Prefab");
            var mockProvider = new MockAssetProvider(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(mockProvider);

            var h1 = _manager.ShowPrefabAsync("test/w1", UILayer.Default, "my.plugin").GetAwaiter().GetResult();
            var h2 = _manager.ShowPrefabAsync("test/w2", UILayer.Default, "my.plugin").GetAwaiter().GetResult();

            var root1 = h1.Root;
            var root2 = h2.Root;

            _manager.CleanupPlugin("my.plugin");
            yield return null;

            Assert.IsTrue(root1 == null, "First plugin window root should be destroyed");
            Assert.IsTrue(root2 == null, "Second plugin window root should be destroyed");

            Object.DestroyImmediate(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);
        }

        [UnityTest]
        public IEnumerator ShowPrefabAsync_NonexistentAsset_ReturnsNull()
        {
            yield return null;

            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("Asset provider"));
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);

            LogAssert.Expect(LogType.Error, "[Maqui] Prefab not found in Resources: 'nonexistent/path'");
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Prefab not found.*nonexistent"));

            var task = _manager.ShowPrefabAsync("nonexistent/path", UILayer.Default);
            Assert.IsNull(task.GetAwaiter().GetResult());

            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);
        }

        // ── Error Event Tests ────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator WindowLoadFailed_FiresOnNullKey()
        {
            yield return null;

            WindowLoadFailedEvent? received = null;
            _manager.WindowLoadFailed += e => received = e;

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("ShowWindowAsync"));
            _manager.ShowWindowAsync<TestView, TestVM>(null, UILayer.Default, new TestVM());

            Assert.IsNotNull(received, "WindowLoadFailed should have fired");
            Assert.AreEqual(UILayer.Default, received.Value.Layer);
            Assert.IsNull(received.Value.Exception);
        }

        [UnityTest]
        public IEnumerator WindowLoadFailed_FiresOnMissingAsset()
        {
            yield return null;

            WindowLoadFailedEvent? received = null;
            _manager.WindowLoadFailed += e => received = e;

            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(new NullAssetProvider());
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("Asset provider"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("not found"));

            _manager.ShowPrefabAsync("missing/key", UILayer.Default).GetAwaiter().GetResult();

            Assert.IsNotNull(received);
            Assert.AreEqual("missing/key", received.Value.AssetKey);

            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);
        }

        [UnityTest]
        public IEnumerator WindowLoadFailed_FiresOnThrowingProvider()
        {
            yield return null;

            WindowLoadFailedEvent? received = null;
            _manager.WindowLoadFailed += e => received = e;

            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(new ThrowingAssetProvider());
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("Asset provider"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Failed to load"));

            _manager.ShowPrefabAsync("any/key", UILayer.Default).GetAwaiter().GetResult();

            Assert.IsNotNull(received);
            Assert.IsNotNull(received.Value.Exception);
            Assert.AreEqual("any/key", received.Value.AssetKey);

            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);
        }

        [UnityTest]
        public IEnumerator ShowWindowAsync_MissingViewComponent_ReturnsNullAndFiresEvent()
        {
            yield return null;

            WindowLoadFailedEvent? received = null;
            _manager.WindowLoadFailed += e => received = e;

            // Prefab without TestView component
            var prefab = new GameObject("NoViewPrefab");
            prefab.AddComponent<CanvasGroup>();
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(new MockAssetProvider(prefab));
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("Asset provider"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("missing.*TestView"));

            var result = _manager.ShowWindowAsync<TestView, TestVM>("test/noview", UILayer.Default, new TestVM())
                .GetAwaiter().GetResult();

            Assert.IsNull(result);
            Assert.IsNotNull(received);
            Assert.IsTrue(received.Value.Reason.Contains("missing"));

            Object.DestroyImmediate(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);
        }

        // ── ShowWindowAsync typed view lifecycle ─────────────────────────────

        [UnityTest]
        public IEnumerator ShowWindowAsync_WithView_InitializesAndReturnsHandle()
        {
            yield return null;

            var prefab = new GameObject("ViewPrefab");
            prefab.AddComponent<CanvasGroup>();
            prefab.AddComponent<RectTransform>();
            var view = prefab.AddComponent<TestView>();
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(new MockAssetProvider(prefab));
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("Asset provider"));

            var vm = new TestVM();
            var handle = _manager.ShowWindowAsync<TestView, TestVM>("test/view", UILayer.Default, vm)
                .GetAwaiter().GetResult();

            Assert.IsNotNull(handle);
            Assert.AreEqual(UILayer.Default, handle.Layer);
            Assert.IsTrue(handle.IsVisible);

            handle.Dispose();
            yield return null;
            Object.DestroyImmediate(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);
        }

        [UnityTest]
        public IEnumerator ShowWindowAsync_ConfigureOverload_ConfiguresVM()
        {
            yield return null;

            var prefab = new GameObject("ViewPrefab");
            prefab.AddComponent<CanvasGroup>();
            prefab.AddComponent<RectTransform>();
            prefab.AddComponent<ConfigTestView>();
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(new MockAssetProvider(prefab));
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("Asset provider"));

            bool configured = false;
            var handle = _manager.ShowWindowAsync<ConfigTestView, ConfigTestVM>(
                "test/config", UILayer.Default,
                vm => { vm.WasConfigured = true; configured = true; })
                .GetAwaiter().GetResult();

            Assert.IsNotNull(handle);
            Assert.IsTrue(configured, "Configure callback should have been invoked");

            handle.Dispose();
            yield return null;
            Object.DestroyImmediate(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);
        }

        // ── ShowWindowAsync on Modal layer triggers freeze ───────────────────

        [UnityTest]
        public IEnumerator ShowWindowAsync_ModalLayer_ActivatesMaskAndFreezes()
        {
            yield return null;

            var mask = _managerGo.transform.Find("Layer_ModalMask");
            var viewGo = new GameObject("FreezableView2");
            var fv = viewGo.AddComponent<FreezeTrackingView>();
            yield return null;
            _manager.RegisterFreezable(fv);

            var prefab = new GameObject("ModalViewPrefab");
            prefab.AddComponent<CanvasGroup>();
            prefab.AddComponent<RectTransform>();
            prefab.AddComponent<TestView>();
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(new MockAssetProvider(prefab));
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("Asset provider"));

            var handle = _manager.ShowWindowAsync<TestView, TestVM>("test/modal", UILayer.Modal, new TestVM())
                .GetAwaiter().GetResult();

            Assert.IsTrue(mask.gameObject.activeSelf, "Modal mask should activate");
            Assert.IsTrue(fv.IsFrozen, "View should be frozen");

            handle.Dispose();
            yield return null;

            Assert.IsFalse(mask.gameObject.activeSelf, "Modal mask should deactivate");
            Assert.IsFalse(fv.IsFrozen, "View should be unfrozen");

            _manager.UnregisterFreezable(fv);
            Object.DestroyImmediate(viewGo);
            Object.DestroyImmediate(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);
        }

        // ── ShowPrefabAsync with plugin tracking ─────────────────────────────

        [UnityTest]
        public IEnumerator ShowPrefabAsync_PluginId_TracksAndCleansUp()
        {
            yield return null;

            var prefab = new GameObject("PluginPrefab");
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(new MockAssetProvider(prefab));
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("Asset provider"));

            var h1 = _manager.ShowPrefabAsync("p/w1", UILayer.Default, "test.plugin").GetAwaiter().GetResult();
            var h2 = _manager.ShowPrefabAsync("p/w2", UILayer.Overlay, "test.plugin").GetAwaiter().GetResult();
            var h3 = _manager.ShowPrefabAsync("p/w3", UILayer.Default, "other.plugin").GetAwaiter().GetResult();

            var root1 = h1.Root;
            var root2 = h2.Root;
            var root3 = h3.Root;

            _manager.CleanupPlugin("test.plugin");
            yield return null;

            Assert.IsTrue(root1 == null, "Plugin window 1 should be destroyed");
            Assert.IsTrue(root2 == null, "Plugin window 2 should be destroyed");
            Assert.IsFalse(root3 == null, "Other plugin window should survive");

            h3.Dispose();
            yield return null;
            Object.DestroyImmediate(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);
        }

        // ── Pool Tests ───────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator ShowWindowAsync_Pooled_ReusesInstance()
        {
            yield return null;

            var prefab = new GameObject("PoolPrefab");
            prefab.AddComponent<CanvasGroup>();
            prefab.AddComponent<RectTransform>();
            prefab.AddComponent<PoolTrackingView>();
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(new MockAssetProvider(prefab));
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("Asset provider"));

            var opts = new WindowOptions { Pooled = true, MaxPoolSize = 2 };

            // First show — fresh instantiation
            var vm1 = new PoolTrackingVM();
            var handle1 = _manager.ShowWindowAsync<PoolTrackingView, PoolTrackingVM>(
                "test/pool", UILayer.Default, vm1, opts).GetAwaiter().GetResult();
            Assert.IsNotNull(handle1);
            var root1 = handle1.Root;
            var instanceId = root1.GetInstanceID();

            // Dispose — should return to pool, not destroy
            handle1.Dispose();
            yield return null;
            Assert.IsFalse(root1 == null, "Root should NOT be destroyed (pooled)");
            Assert.IsFalse(root1.activeSelf, "Root should be inactive in pool");

            // Second show — should reuse the same instance
            var vm2 = new PoolTrackingVM();
            var handle2 = _manager.ShowWindowAsync<PoolTrackingView, PoolTrackingVM>(
                "test/pool", UILayer.Default, vm2, opts).GetAwaiter().GetResult();
            Assert.IsNotNull(handle2);
            Assert.AreEqual(instanceId, handle2.Root.GetInstanceID(), "Should reuse pooled instance");
            Assert.IsTrue(handle2.Root.activeSelf, "Reused root should be active");

            handle2.Dispose();
            yield return null;
            Object.DestroyImmediate(prefab);
            // Drain remaining pooled objects
            Object.DestroyImmediate(root1);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);
        }

        [UnityTest]
        public IEnumerator ShowWindowAsync_Pooled_DisposeClearsOldVM()
        {
            yield return null;

            var prefab = new GameObject("PoolPrefab");
            prefab.AddComponent<CanvasGroup>();
            prefab.AddComponent<RectTransform>();
            prefab.AddComponent<PoolTrackingView>();
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(new MockAssetProvider(prefab));
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("Asset provider"));

            var opts = new WindowOptions { Pooled = true };
            var vm1 = new PoolTrackingVM();
            var handle = _manager.ShowWindowAsync<PoolTrackingView, PoolTrackingVM>(
                "test/pool", UILayer.Default, vm1, opts).GetAwaiter().GetResult();

            handle.Dispose();
            yield return null;

            Assert.IsTrue(vm1.WasDisposed, "Old ViewModel should be disposed on pool return");

            var root = handle.Root;
            if (root != null) Object.DestroyImmediate(root);
            Object.DestroyImmediate(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);
        }

        [UnityTest]
        public IEnumerator ShowWindowAsync_Pooled_RebindsNewVM()
        {
            yield return null;

            var prefab = new GameObject("PoolPrefab");
            prefab.AddComponent<CanvasGroup>();
            prefab.AddComponent<RectTransform>();
            prefab.AddComponent<PoolTrackingView>();
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(new MockAssetProvider(prefab));
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("Asset provider"));

            var opts = new WindowOptions { Pooled = true };

            var vm1 = new PoolTrackingVM();
            var h1 = _manager.ShowWindowAsync<PoolTrackingView, PoolTrackingVM>(
                "test/pool", UILayer.Default, vm1, opts).GetAwaiter().GetResult();
            var view = h1.Root.GetComponent<PoolTrackingView>();
            Assert.AreEqual(1, view.BindCount, "OnBind should be called once on first show");

            h1.Dispose();
            yield return null;
            Assert.IsTrue(view.ResetCalled, "OnReset should be called on pool return");

            var vm2 = new PoolTrackingVM();
            var h2 = _manager.ShowWindowAsync<PoolTrackingView, PoolTrackingVM>(
                "test/pool", UILayer.Default, vm2, opts).GetAwaiter().GetResult();
            Assert.AreEqual(2, view.BindCount, "OnBind should be called again on reuse");

            h2.Dispose();
            yield return null;
            var root = h2.Root;
            if (root != null) Object.DestroyImmediate(root);
            Object.DestroyImmediate(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);
        }

        [UnityTest]
        public IEnumerator ShowWindowAsync_Pooled_MaxSize_DestroysExcess()
        {
            yield return null;

            var prefab = new GameObject("PoolPrefab");
            prefab.AddComponent<CanvasGroup>();
            prefab.AddComponent<RectTransform>();
            prefab.AddComponent<PoolTrackingView>();
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(new MockAssetProvider(prefab));
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("Asset provider"));

            var opts = new WindowOptions { Pooled = true, MaxPoolSize = 1 };

            // Show and pool two windows with max=1
            var h1 = _manager.ShowWindowAsync<PoolTrackingView, PoolTrackingVM>(
                "test/pool", UILayer.Default, new PoolTrackingVM(), opts).GetAwaiter().GetResult();
            var root1 = h1.Root;

            var h2 = _manager.ShowWindowAsync<PoolTrackingView, PoolTrackingVM>(
                "test/pool", UILayer.Default, new PoolTrackingVM(), opts).GetAwaiter().GetResult();
            var root2 = h2.Root;

            // Dispose first — goes to pool (pool has 0, max 1)
            h1.Dispose();
            yield return null;
            Assert.IsFalse(root1 == null, "First should be pooled");

            // Dispose second — pool full (max 1), should be destroyed
            h2.Dispose();
            yield return null;
            Assert.IsTrue(root2 == null, "Second should be destroyed (pool full)");

            // Clean up pooled instance
            if (root1 != null) Object.DestroyImmediate(root1);
            Object.DestroyImmediate(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);
        }

        [UnityTest]
        public IEnumerator ShowWindowAsync_NonPooled_StillDestroysOnDispose()
        {
            yield return null;

            var prefab = new GameObject("NonPoolPrefab");
            prefab.AddComponent<CanvasGroup>();
            prefab.AddComponent<RectTransform>();
            prefab.AddComponent<TestView>();
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(new MockAssetProvider(prefab));
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("Asset provider"));

            var handle = _manager.ShowWindowAsync<TestView, TestVM>(
                "test/nopool", UILayer.Default, new TestVM()).GetAwaiter().GetResult();
            var root = handle.Root;

            handle.Dispose();
            yield return null;
            Assert.IsTrue(root == null, "Non-pooled window should be destroyed");

            Object.DestroyImmediate(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);
        }

        [UnityTest]
        public IEnumerator ShowWindowAsync_Pooled_NotFreezeWhenDormant()
        {
            yield return null;

            var prefab = new GameObject("PoolPrefab");
            prefab.AddComponent<CanvasGroup>();
            prefab.AddComponent<RectTransform>();
            prefab.AddComponent<FreezeTrackingView>();
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(new MockAssetProvider(prefab));
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("Asset provider"));

            var opts = new WindowOptions { Pooled = true };
            var h = _manager.ShowWindowAsync<FreezeTrackingView, TestVM>(
                "test/freezepool", UILayer.Default, new TestVM(), opts).GetAwaiter().GetResult();
            var view = h.Root.GetComponent<FreezeTrackingView>();

            // Pool the view
            h.Dispose();
            yield return null;

            // Open a modal — pooled dormant view should NOT be frozen
            var modalPrefab = new GameObject("ModalPrefab");
            var modalMockProvider = new MockAssetProvider(modalPrefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(modalMockProvider);
            var modalHandle = _manager.ShowPrefabAsync("test/modal", UILayer.Modal).GetAwaiter().GetResult();

            Assert.IsFalse(view.IsFrozen, "Dormant pooled view should NOT receive freeze");

            modalHandle.Dispose();
            yield return null;

            var root = h.Root;
            if (root != null) Object.DestroyImmediate(root);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(modalPrefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);
        }

        // ── Navigation Stack Tests ────────────────────────────────────────────

        [UnityTest]
        public IEnumerator PopWindow_EmptyStack_ReturnsFalse()
        {
            yield return null;

            Assert.IsFalse(_manager.PopWindow(UILayer.Default), "PopWindow on empty stack should return false");
            Assert.IsFalse(_manager.PopWindow(UILayer.Modal), "PopWindow on empty Modal stack should return false");
        }

        [UnityTest]
        public IEnumerator PopWindow_DisposesTopmost()
        {
            yield return null;

            var prefab = new GameObject("Prefab");
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(new MockAssetProvider(prefab));
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("Asset provider"));

            var h1 = _manager.ShowPrefabAsync("test/w1", UILayer.Default).GetAwaiter().GetResult();
            var h2 = _manager.ShowPrefabAsync("test/w2", UILayer.Default).GetAwaiter().GetResult();

            var root2 = h2.Root;
            var root1 = h1.Root;

            Assert.AreEqual(2, _manager.GetStackDepth(UILayer.Default));

            var popped = _manager.PopWindow(UILayer.Default);
            yield return null;

            Assert.IsTrue(popped, "PopWindow should return true");
            Assert.IsTrue(root2 == null, "Top window should be destroyed");
            Assert.IsFalse(root1 == null, "Bottom window should survive");
            Assert.AreEqual(1, _manager.GetStackDepth(UILayer.Default));

            h1.Dispose();
            yield return null;
            Object.DestroyImmediate(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);
        }

        [UnityTest]
        public IEnumerator GetStackDepth_TracksShowAndDispose()
        {
            yield return null;

            var prefab = new GameObject("Prefab");
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(new MockAssetProvider(prefab));
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("Asset provider"));

            Assert.AreEqual(0, _manager.GetStackDepth(UILayer.Default));

            var h1 = _manager.ShowPrefabAsync("test/w1", UILayer.Default).GetAwaiter().GetResult();
            Assert.AreEqual(1, _manager.GetStackDepth(UILayer.Default));

            var h2 = _manager.ShowPrefabAsync("test/w2", UILayer.Default).GetAwaiter().GetResult();
            Assert.AreEqual(2, _manager.GetStackDepth(UILayer.Default));

            h2.Dispose();
            yield return null;
            Assert.AreEqual(1, _manager.GetStackDepth(UILayer.Default));

            h1.Dispose();
            yield return null;
            Assert.AreEqual(0, _manager.GetStackDepth(UILayer.Default));

            Object.DestroyImmediate(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);
        }

        [UnityTest]
        public IEnumerator GetStackDepth_PerLayerIndependent()
        {
            yield return null;

            var prefab = new GameObject("Prefab");
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(new MockAssetProvider(prefab));
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("Asset provider"));

            var hDefault = _manager.ShowPrefabAsync("test/w1", UILayer.Default).GetAwaiter().GetResult();
            var hOverlay = _manager.ShowPrefabAsync("test/w2", UILayer.Overlay).GetAwaiter().GetResult();

            Assert.AreEqual(1, _manager.GetStackDepth(UILayer.Default));
            Assert.AreEqual(1, _manager.GetStackDepth(UILayer.Overlay));
            Assert.AreEqual(0, _manager.GetStackDepth(UILayer.Modal));

            hDefault.Dispose();
            hOverlay.Dispose();
            yield return null;
            Object.DestroyImmediate(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);
        }

        [UnityTest]
        public IEnumerator BackRequested_OnBackRequested_ConsumesEvent()
        {
            yield return null;

            var prefab = new GameObject("BackPrefab");
            prefab.AddComponent<CanvasGroup>();
            prefab.AddComponent<RectTransform>();
            prefab.AddComponent<BackConsumingView>();
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(new MockAssetProvider(prefab));
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("Asset provider"));

            var vm = new TestVM();
            var handle = _manager.ShowWindowAsync<BackConsumingView, TestVM>(
                "test/back", UILayer.Default, vm).GetAwaiter().GetResult();

            Assert.AreEqual(1, _manager.GetStackDepth(UILayer.Default));

            // Simulate what Update() does when Escape is pressed:
            // The view's OnBackRequested returns true, so PopWindow should NOT be called
            var view = handle.Root.GetComponent<BackConsumingView>();
            Assert.IsTrue(view.OnBackRequestedResult, "BackConsumingView should return true");

            // Verify the view is still alive (stack not popped because event was consumed)
            Assert.AreEqual(1, _manager.GetStackDepth(UILayer.Default));
            Assert.IsTrue(handle.IsVisible);

            handle.Dispose();
            yield return null;
            Object.DestroyImmediate(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);
        }

        [UnityTest]
        public IEnumerator SuppressBackNavigation_DefaultsFalse()
        {
            yield return null;
            Assert.IsFalse(_manager.SuppressBackNavigation);
        }

        [UnityTest]
        public IEnumerator NavigationStack_OutOfOrderDispose_HandledGracefully()
        {
            yield return null;

            var prefab = new GameObject("Prefab");
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(new MockAssetProvider(prefab));
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("Asset provider"));

            var h1 = _manager.ShowPrefabAsync("test/w1", UILayer.Default).GetAwaiter().GetResult();
            var h2 = _manager.ShowPrefabAsync("test/w2", UILayer.Default).GetAwaiter().GetResult();
            var h3 = _manager.ShowPrefabAsync("test/w3", UILayer.Default).GetAwaiter().GetResult();

            Assert.AreEqual(3, _manager.GetStackDepth(UILayer.Default));

            // Dispose middle window (out of order)
            h2.Dispose();
            yield return null;
            Assert.AreEqual(2, _manager.GetStackDepth(UILayer.Default));

            // Pop top — should get h3
            var root3 = h3.Root;
            _manager.PopWindow(UILayer.Default);
            yield return null;
            Assert.IsTrue(root3 == null, "h3 should be destroyed by pop");
            Assert.AreEqual(1, _manager.GetStackDepth(UILayer.Default));

            h1.Dispose();
            yield return null;
            Object.DestroyImmediate(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);
        }

        [UnityTest]
        public IEnumerator ShowWindowAsync_TypedView_PushesToStack()
        {
            yield return null;

            var prefab = new GameObject("ViewPrefab");
            prefab.AddComponent<CanvasGroup>();
            prefab.AddComponent<RectTransform>();
            prefab.AddComponent<TestView>();
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(new MockAssetProvider(prefab));
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("Asset provider"));

            Assert.AreEqual(0, _manager.GetStackDepth(UILayer.Default));

            var handle = _manager.ShowWindowAsync<TestView, TestVM>(
                "test/view", UILayer.Default, new TestVM()).GetAwaiter().GetResult();

            Assert.AreEqual(1, _manager.GetStackDepth(UILayer.Default));

            handle.Dispose();
            yield return null;
            Assert.AreEqual(0, _manager.GetStackDepth(UILayer.Default));

            Object.DestroyImmediate(prefab);
            Maqui.Core.Bridge.MaquiAssetProviderBridge.SetProvider(null);
        }

        // ── Test doubles ──────────────────────────────────────────────────────

        private class BackConsumingView : ReactiveBaseView<TestVM>
        {
            public bool OnBackRequestedResult = true;

            protected override void OnBind() { }
            protected override bool OnBackRequested() => OnBackRequestedResult;
        }

        private class PoolTrackingVM : Maqui.Core.Logic.ViewModel
        {
            public bool WasDisposed;
            public override void Dispose() { WasDisposed = true; base.Dispose(); }
        }

        private class PoolTrackingView : ReactiveBaseView<PoolTrackingVM>
        {
            public int BindCount;
            public bool ResetCalled;

            protected override void OnBind() => BindCount++;
            protected override void OnReset() => ResetCalled = true;
        }

        private class TestVM : Maqui.Core.Logic.ViewModel { }

        private class TestView : ReactiveBaseView<TestVM>
        {
            protected override void OnBind() { }
        }

        private class ConfigTestVM : Maqui.Core.Logic.ViewModel
        {
            public bool WasConfigured;
        }

        private class ConfigTestView : ReactiveBaseView<ConfigTestVM>
        {
            protected override void OnBind() { }
        }

        private class FreezeTrackingView : ReactiveBaseView<TestVM>, IFreezableView
        {
            public bool IsFrozen { get; private set; }

            protected override void OnBind() { }
            protected override void OnFreeze() => IsFrozen = true;
            protected override void OnUnfreeze() => IsFrozen = false;
        }

        private class MockAssetProvider : Maqui.Core.Bridge.IMaquiAssetProvider
        {
            private readonly GameObject _prefab;

            public MockAssetProvider(GameObject prefab) => _prefab = prefab;

            public Cysharp.Threading.Tasks.UniTask<GameObject> LoadPrefabAsync(
                string key, System.Threading.CancellationToken ct = default)
            {
                return Cysharp.Threading.Tasks.UniTask.FromResult(_prefab);
            }

            public void ReleasePrefab(string key) { }
        }

        private class NullAssetProvider : Maqui.Core.Bridge.IMaquiAssetProvider
        {
            public Cysharp.Threading.Tasks.UniTask<GameObject> LoadPrefabAsync(
                string key, System.Threading.CancellationToken ct = default)
            {
                return Cysharp.Threading.Tasks.UniTask.FromResult<GameObject>(null);
            }

            public void ReleasePrefab(string key) { }
        }

        private class ThrowingAssetProvider : Maqui.Core.Bridge.IMaquiAssetProvider
        {
            public Cysharp.Threading.Tasks.UniTask<GameObject> LoadPrefabAsync(
                string key, System.Threading.CancellationToken ct = default)
            {
                throw new System.IO.FileNotFoundException($"Asset bundle not found: {key}");
            }

            public void ReleasePrefab(string key) { }
        }
    }
}
