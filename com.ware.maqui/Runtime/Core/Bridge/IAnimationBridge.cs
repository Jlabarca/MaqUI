using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Maqui.Core.Bridge
{
    /// <summary>
    /// Contract for UniTask-based UI animations.
    /// </summary>
    public interface IAnimationBridge
    {
        UniTask FadeAsync(CanvasGroup group, float targetAlpha, float duration, CancellationToken ct = default);
        UniTask ScaleAsync(RectTransform transform, Vector3 targetScale, float duration, AnimationCurve curve = null, CancellationToken ct = default);
        UniTask SceneTransitionAsync(string sceneName, float duration, Color color, CancellationToken ct = default);
    }
}
