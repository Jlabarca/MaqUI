using System.Collections;
using System.Threading;
using NUnit.Framework;
using Maqui.Core.Bridge;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;

namespace Maqui.Tests
{
    public class AssetProviderTests
    {
        private IMaquiAssetProvider _originalProvider;

        [SetUp]
        public void SetUp()
        {
            _originalProvider = MaquiAssetProviderBridge.Current;
        }

        [TearDown]
        public void TearDown()
        {
            // Restore default provider
            MaquiAssetProviderBridge.SetProvider(null); // resets to ResourcesAssetProvider
        }

        [Test]
        public void Current_DefaultsToResourcesAssetProvider()
        {
            MaquiAssetProviderBridge.SetProvider(null);
            Assert.IsInstanceOf<ResourcesAssetProvider>(MaquiAssetProviderBridge.Current);
        }

        [Test]
        public void SetProvider_CustomProvider_ReplacesCurrent()
        {
            var custom = new MockAssetProvider();
            MaquiAssetProviderBridge.SetProvider(custom);

            Assert.AreSame(custom, MaquiAssetProviderBridge.Current);
        }

        [Test]
        public void SetProvider_Null_FallsBackToResourcesProvider()
        {
            var custom = new MockAssetProvider();
            MaquiAssetProviderBridge.SetProvider(custom);
            Assert.AreSame(custom, MaquiAssetProviderBridge.Current);

            MaquiAssetProviderBridge.SetProvider(null);
            Assert.IsInstanceOf<ResourcesAssetProvider>(MaquiAssetProviderBridge.Current);
        }

        [UnityTest]
        public IEnumerator ResourcesAssetProvider_NonexistentKey_LogsError()
        {
            var provider = new ResourcesAssetProvider();

            LogAssert.Expect(LogType.Error, "[Maqui] Prefab not found in Resources: 'NonExistent/Prefab'");

            var task = provider.LoadPrefabAsync("NonExistent/Prefab");
            // ResourcesAssetProvider is synchronous so this completes immediately
            Assert.IsTrue(task.Status == UniTaskStatus.Succeeded);
            Assert.IsNull(task.GetAwaiter().GetResult());
            yield return null;
        }

        [Test]
        public void ResourcesAssetProvider_ReleasePrefab_DoesNotThrow()
        {
            var provider = new ResourcesAssetProvider();
            Assert.DoesNotThrow(() => provider.ReleasePrefab("any/key"));
        }

        [UnityTest]
        public IEnumerator CustomProvider_LoadPrefabAsync_IsInvoked()
        {
            var mock = new MockAssetProvider();
            var expected = new GameObject("MockPrefab");
            mock.PrefabToReturn = expected;

            MaquiAssetProviderBridge.SetProvider(mock);

            var task = MaquiAssetProviderBridge.Current.LoadPrefabAsync("test/key");
            Assert.IsTrue(task.Status == UniTaskStatus.Succeeded);
            Assert.AreSame(expected, task.GetAwaiter().GetResult());
            Assert.AreEqual("test/key", mock.LastLoadedKey);

            Object.DestroyImmediate(expected);
            yield return null;
        }

        [Test]
        public void CustomProvider_ReleasePrefab_IsInvoked()
        {
            var mock = new MockAssetProvider();
            MaquiAssetProviderBridge.SetProvider(mock);

            MaquiAssetProviderBridge.Current.ReleasePrefab("some/key");
            Assert.AreEqual("some/key", mock.LastReleasedKey);
        }

        // ── Test doubles ──────────────────────────────────────────────────────

        private class MockAssetProvider : IMaquiAssetProvider
        {
            public GameObject PrefabToReturn;
            public string LastLoadedKey { get; private set; }
            public string LastReleasedKey { get; private set; }

            public UniTask<GameObject> LoadPrefabAsync(string key, CancellationToken ct = default)
            {
                LastLoadedKey = key;
                return UniTask.FromResult(PrefabToReturn);
            }

            public void ReleasePrefab(string key)
            {
                LastReleasedKey = key;
            }
        }
    }
}
