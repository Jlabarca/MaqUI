using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Maqui.Core.Bridge
{
    /// <summary>
    /// Fluent builder for composing sequential and parallel animations.
    /// Use Then() for sequential steps and With() for parallel steps.
    /// </summary>
    public class AnimationSequence
    {
        private readonly List<List<Func<CancellationToken, UniTask>>> _groups = new();

        private AnimationSequence() { }

        /// <summary>
        /// Start building a new animation sequence.
        /// </summary>
        public static AnimationSequence Create() => new();

        /// <summary>
        /// Add a step that runs sequentially (after all previous steps complete).
        /// </summary>
        public AnimationSequence Then(Func<CancellationToken, UniTask> step)
        {
            if (step == null) throw new ArgumentNullException(nameof(step));
            _groups.Add(new List<Func<CancellationToken, UniTask>> { step });
            return this;
        }

        /// <summary>
        /// Add a step that runs in parallel with the previous step.
        /// If there is no previous step, behaves like Then().
        /// </summary>
        public AnimationSequence With(Func<CancellationToken, UniTask> step)
        {
            if (step == null) throw new ArgumentNullException(nameof(step));

            if (_groups.Count == 0)
            {
                _groups.Add(new List<Func<CancellationToken, UniTask>> { step });
            }
            else
            {
                _groups[_groups.Count - 1].Add(step);
            }

            return this;
        }

        /// <summary>
        /// Add a delay step that runs sequentially.
        /// </summary>
        public AnimationSequence Delay(float seconds)
        {
            return Then(async ct =>
            {
                await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: ct);
            });
        }

        /// <summary>
        /// Execute the full sequence. Each group runs sequentially;
        /// steps within a group run in parallel via UniTask.WhenAll.
        /// </summary>
        public async UniTask PlayAsync(CancellationToken ct)
        {
            for (int i = 0; i < _groups.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var group = _groups[i];
                if (group.Count == 1)
                {
                    await group[0](ct);
                }
                else
                {
                    var tasks = new UniTask[group.Count];
                    for (int j = 0; j < group.Count; j++)
                    {
                        tasks[j] = group[j](ct);
                    }
                    await UniTask.WhenAll(tasks);
                }
            }
        }
    }
}
