// SPDX-License-Identifier: MIT
// MaqUI v2 — ResourcesImageLoader. Default IImageLoader using Resources.Load.
//
// References UnityEngine.Resources + UnityEngine.Texture2D — Unity-only.
// The standalone xUnit csproj excludes this file via <Compile Remove>.

using UnityEngine;

namespace Maqui.V2
{
    /// <summary>
    /// Default <see cref="Maqui.V2.Components.IImageLoader"/> backed by
    /// <see cref="Resources.Load{T}(string)"/>. Place textures under
    /// <c>Assets/Resources/</c> in the host project; reference them by
    /// the path relative to <c>Resources/</c> (no extension).
    ///
    /// <para>For YooAsset / Addressables, write a parallel impl and pass
    /// it to the <see cref="UIToolkitBackend"/> ctor instead.</para>
    /// </summary>
    public sealed class ResourcesImageLoader : Maqui.V2.Components.IImageLoader
    {
        public Texture2D Resolve(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            return Resources.Load<Texture2D>(key);
        }
    }
}
