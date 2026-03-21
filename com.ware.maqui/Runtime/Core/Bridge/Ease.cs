using System;
using UnityEngine;

namespace Maqui.Core.Bridge
{
    public enum Ease
    {
        Linear,
        InQuad, OutQuad, InOutQuad,
        InCubic, OutCubic, InOutCubic,
        InBack, OutBack, InOutBack,
        InElastic, OutElastic,
    }

    public static class EaseFunctions
    {
        private const float BackOvershoot = 1.70158f;
        private const float ElasticPeriod = 0.3f;

        public static float Evaluate(Ease ease, float t)
        {
            t = Mathf.Clamp01(t);

            switch (ease)
            {
                case Ease.Linear:
                    return t;

                case Ease.InQuad:
                    return t * t;
                case Ease.OutQuad:
                    return t * (2f - t);
                case Ease.InOutQuad:
                    return t < 0.5f
                        ? 2f * t * t
                        : -1f + (4f - 2f * t) * t;

                case Ease.InCubic:
                    return t * t * t;
                case Ease.OutCubic:
                {
                    float f = t - 1f;
                    return f * f * f + 1f;
                }
                case Ease.InOutCubic:
                    return t < 0.5f
                        ? 4f * t * t * t
                        : (t - 1f) * (2f * t - 2f) * (2f * t - 2f) + 1f;

                case Ease.InBack:
                    return t * t * ((BackOvershoot + 1f) * t - BackOvershoot);
                case Ease.OutBack:
                {
                    float f = t - 1f;
                    return f * f * ((BackOvershoot + 1f) * f + BackOvershoot) + 1f;
                }
                case Ease.InOutBack:
                {
                    float s = BackOvershoot * 1.525f;
                    if (t < 0.5f)
                    {
                        float f = 2f * t;
                        return 0.5f * (f * f * ((s + 1f) * f - s));
                    }
                    else
                    {
                        float f = 2f * t - 2f;
                        return 0.5f * (f * f * ((s + 1f) * f + s) + 2f);
                    }
                }

                case Ease.InElastic:
                {
                    if (t <= 0f) return 0f;
                    if (t >= 1f) return 1f;
                    return -(float)(Math.Pow(2, 10 * (t - 1)) *
                        Math.Sin((t - 1f - ElasticPeriod / 4f) * (2f * Math.PI) / ElasticPeriod));
                }
                case Ease.OutElastic:
                {
                    if (t <= 0f) return 0f;
                    if (t >= 1f) return 1f;
                    return (float)(Math.Pow(2, -10 * t) *
                        Math.Sin((t - ElasticPeriod / 4f) * (2f * Math.PI) / ElasticPeriod) + 1f);
                }

                default:
                    return t;
            }
        }
    }
}
