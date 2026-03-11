using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Maqui.Core.Bridge
{
    /// <summary>
    /// Asset loading contract used by MaquiWindowManager.
    /// Default implementation uses Resources.Load (synchronous, no external dependency).
    /// Replace with a YooAsset or Addressables implementation in your project via
    /// MaquiAssetProviderBridge.SetProvider() during boot.
    /// </summary>
    public interface IMaquiAssetProvider
    {
        UniTask<GameObject> LoadPrefabAsync(string key, CancellationToken ct = default);
        void ReleasePrefab(string key);
    }

    /// <summary>
    /// Default provider — synchronous Resources.Load wrapped in UniTask.
    /// Suitable for the CharqUI sandbox and simple projects.
    /// </summary>
    public class ResourcesAssetProvider : IMaquiAssetProvider
    {
        public UniTask<GameObject> LoadPrefabAsync(string key, CancellationToken ct = default)
        {
            var prefab = Resources.Load<GameObject>(key);
            if (prefab == null)
                Debug.LogError($"[Maqui] Prefab not found in Resources: '{key}'");
            return UniTask.FromResult(prefab);
        }

        public void ReleasePrefab(string key) { }
    }

    /// <summary>
    /// Static bridge for registering a custom IMaquiAssetProvider at runtime.
    /// Call SetProvider() early in your boot sequence (before any window is opened).
    /// </summary>
    public static class MaquiAssetProviderBridge
    {
        private static IMaquiAssetProvider _provider = new ResourcesAssetProvider();

        public static IMaquiAssetProvider Current => _provider;

        public static void SetProvider(IMaquiAssetProvider provider)
        {
            _provider = provider ?? new ResourcesAssetProvider();
            Debug.Log($"[Maqui] Asset provider set to: {_provider.GetType().Name}");
        }
    }
}
