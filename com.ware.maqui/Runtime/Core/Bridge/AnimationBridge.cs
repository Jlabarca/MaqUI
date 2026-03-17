using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Maqui.Core.Bridge
{
    /// <summary>
    /// UniTask-based animation bridge for Maqui.
    /// Registered into MaquiServices as IAnimationBridge by CoreBootstrap.
    /// </summary>
    public class AnimationBridge : MonoBehaviour, IAnimationBridge
    {
        /// <summary>
        /// Fades a CanvasGroup to a target alpha.
        /// </summary>
        public async UniTask FadeAsync(CanvasGroup group, float targetAlpha, float duration, CancellationToken ct = default)
        {
            if (group == null) return;

            float startAlpha = group.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (ct.IsCancellationRequested) return;

                elapsed += Time.deltaTime;
                group.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            group.alpha = targetAlpha;
        }

        /// <summary>
        /// Scales a Transform to a target scale.
        /// </summary>
        public async UniTask ScaleAsync(RectTransform transform, Vector3 targetScale, float duration, AnimationCurve curve = null, CancellationToken ct = default)
        {
            if (transform == null) return;

            Vector3 startScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (ct.IsCancellationRequested) return;

                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float curveValue = curve?.Evaluate(t) ?? t;

                transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, curveValue);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            transform.localScale = targetScale;
        }

        /// <summary>
        /// Global Scene Fade transition.
        /// </summary>
        public async UniTask SceneTransitionAsync(string sceneName, float duration, Color color, CancellationToken ct = default)
        {
            Debug.Log($"[Maqui] Transitioning to scene: {sceneName}");
            // In a real scenario, we'd spawn a Global Overlay here.
            await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: ct);
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }
}
