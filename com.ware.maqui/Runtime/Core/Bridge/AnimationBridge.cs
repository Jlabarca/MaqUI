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
        /// Fades a CanvasGroup to a target alpha (linear).
        /// </summary>
        public UniTask FadeAsync(CanvasGroup group, float targetAlpha, float duration, CancellationToken ct = default)
        {
            return FadeAsync(group, targetAlpha, duration, Ease.Linear, ct);
        }

        /// <summary>
        /// Fades a CanvasGroup to a target alpha with easing.
        /// </summary>
        public async UniTask FadeAsync(CanvasGroup group, float targetAlpha, float duration, Ease ease, CancellationToken ct = default)
        {
            if (group == null) return;

            float startAlpha = group.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (ct.IsCancellationRequested) return;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                group.alpha = Mathf.LerpUnclamped(startAlpha, targetAlpha, EaseFunctions.Evaluate(ease, t));
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            group.alpha = targetAlpha;
        }

        /// <summary>
        /// Scales a Transform to a target scale (linear, with optional AnimationCurve).
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
        /// Scales a Transform to a target scale with easing.
        /// </summary>
        public async UniTask ScaleAsync(RectTransform transform, Vector3 targetScale, float duration, Ease ease, CancellationToken ct = default)
        {
            if (transform == null) return;

            Vector3 startScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (ct.IsCancellationRequested) return;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, EaseFunctions.Evaluate(ease, t));
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
