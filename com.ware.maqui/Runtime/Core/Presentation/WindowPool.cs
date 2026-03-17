using System.Collections.Generic;
using UnityEngine;

namespace Maqui.Core.Presentation
{
    /// <summary>
    /// Per-asset-key object pool for window GameObjects.
    /// Internal to MaquiWindowManager — not part of the public API.
    /// Pooled objects stay parented to their layer canvas but are deactivated.
    /// </summary>
    internal sealed class WindowPool
    {
        private const int DefaultMaxSize = 4;

        private readonly Dictionary<string, Stack<GameObject>> _pools = new();
        private readonly Dictionary<string, int> _maxSizes = new();

        /// <summary>
        /// Try to retrieve a pooled GameObject for the given asset key.
        /// Returns false if the pool is empty for that key.
        /// The returned GO is still inactive — caller must SetActive(true).
        /// </summary>
        public bool TryGet(string assetKey, out GameObject go)
        {
            if (_pools.TryGetValue(assetKey, out var stack) && stack.Count > 0)
            {
                go = stack.Pop();
                // Guard against externally destroyed objects
                if (go == null)
                {
                    go = null;
                    return false;
                }
                return true;
            }

            go = null;
            return false;
        }

        /// <summary>
        /// Return a GameObject to the pool. Deactivates it.
        /// Returns false if the pool is at capacity (caller should Destroy).
        /// </summary>
        public bool Return(string assetKey, GameObject go)
        {
            if (go == null) return false;

            int max = GetMaxSize(assetKey);

            if (!_pools.TryGetValue(assetKey, out var stack))
            {
                stack = new Stack<GameObject>();
                _pools[assetKey] = stack;
            }

            if (stack.Count >= max)
                return false;

            go.SetActive(false);
            stack.Push(go);
            return true;
        }

        /// <summary>
        /// Set the max pool size for an asset key. First call per key wins;
        /// subsequent calls use the larger value.
        /// </summary>
        public void EnsureMaxSize(string assetKey, int requestedMax)
        {
            int effective = requestedMax > 0 ? requestedMax : DefaultMaxSize;

            if (_maxSizes.TryGetValue(assetKey, out int current))
            {
                if (effective > current)
                    _maxSizes[assetKey] = effective;
            }
            else
            {
                _maxSizes[assetKey] = effective;
            }
        }

        /// <summary>
        /// Destroys all pooled GameObjects. Called from MaquiWindowManager.OnDestroy().
        /// </summary>
        public void DrainAll()
        {
            foreach (var kvp in _pools)
            {
                foreach (var go in kvp.Value)
                {
                    if (go != null)
                        Object.Destroy(go);
                }
            }
            _pools.Clear();
            _maxSizes.Clear();
        }

        private int GetMaxSize(string assetKey)
        {
            return _maxSizes.TryGetValue(assetKey, out int max) ? max : DefaultMaxSize;
        }
    }
}
