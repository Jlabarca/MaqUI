// SPDX-License-Identifier: MIT
// MaqUI v2 — AnimationFloat. Spring-damper integrator for UI motion.

namespace Maqui
{
    /// <summary>
    /// Spring-damper integrator advancing <see cref="Current"/> toward
    /// <see cref="Target"/> via <see cref="Velocity"/>, <see cref="Stiffness"/>
    /// (spring k), and <see cref="Damping"/> (damper c).
    ///
    /// <para>Single-step explicit Euler. Defaults give ~150ms settle time for
    /// unit-distance moves; the default ratio is overdamped to avoid overshoot
    /// on UI motion. Callers should clamp <c>dt</c> ≤ 1/30s when calling
    /// <see cref="Tick"/> — large timesteps can blow up the explicit
    /// integrator.</para>
    ///
    /// <para>Usage: as a stored field on a view-model, or via
    /// <see cref="AnimationStore"/> for scope-keyed values living on
    /// <see cref="Gui"/> across reconciles.</para>
    /// </summary>
    public struct AnimationFloat
    {
        /// <summary>Current animated value. Read this in render code.</summary>
        public float Current;

        /// <summary>Target value. Write this to start a transition.</summary>
        public float Target;

        /// <summary>Current velocity (units / second).</summary>
        public float Velocity;

        /// <summary>Spring constant <c>k</c>. Higher = faster pull toward target.</summary>
        public float Stiffness;

        /// <summary>Damper constant <c>c</c>. Higher = more friction; suppresses overshoot.</summary>
        public float Damping;

        /// <summary>Default stiffness (200) — ~150ms settle for unit moves.</summary>
        public const float DefaultStiffness = 200f;

        /// <summary>Default damping (20) — overdamped to suppress overshoot.</summary>
        public const float DefaultDamping = 20f;

        /// <summary>Max recommended timestep — explicit Euler blows up beyond this.</summary>
        public const float MaxStableDt = 1f / 30f;

        public AnimationFloat(float current, float target = 0f, float stiffness = DefaultStiffness, float damping = DefaultDamping)
        {
            Current = current;
            Target = target;
            Velocity = 0f;
            Stiffness = stiffness;
            Damping = damping;
        }

        /// <summary>
        /// Advance the integrator by <paramref name="dt"/> seconds.
        /// Caller clamps <paramref name="dt"/> ≤ <see cref="MaxStableDt"/>.
        /// </summary>
        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            if (dt > MaxStableDt) dt = MaxStableDt;
            float force = (Target - Current) * Stiffness - Velocity * Damping;
            Velocity += force * dt;
            Current += Velocity * dt;
        }

        /// <summary>
        /// Snap <see cref="Current"/> to <see cref="Target"/> and zero
        /// <see cref="Velocity"/>. Useful for skipping intro animations.
        /// </summary>
        public void SnapToTarget()
        {
            Current = Target;
            Velocity = 0f;
        }

        /// <summary>
        /// Has the animation effectively settled (distance + velocity below
        /// <paramref name="epsilon"/>)? Cheap test for stopping per-frame ticks.
        /// </summary>
        public readonly bool IsSettled(float epsilon = 0.001f)
        {
            float dist = Target - Current;
            if (dist < 0f) dist = -dist;
            float vel = Velocity;
            if (vel < 0f) vel = -vel;
            return dist < epsilon && vel < epsilon;
        }
    }
}
