using UnityEngine;

namespace PaiSho
{
    /// <summary>Springy easing for UI — bouncy Nintendo-style motion.</summary>
    public static class UiFeel
    {
        private const float MaxDeltaTime = 0.05f;
        private const float MaxAbsVelocity = 40f;

        public static float Spring(
            ref float current,
            float target,
            ref float velocity,
            float deltaTime,
            float frequency = 5.5f,
            float damping = 0.72f)
        {
            if (deltaTime <= 0f)
                return current;

            // Long AI turns / hitch frames used to explode Euler springs into NaN and freeze UI.
            deltaTime = Mathf.Min(deltaTime, MaxDeltaTime);

            if (float.IsNaN(current) || float.IsInfinity(current))
                current = target;
            if (float.IsNaN(velocity) || float.IsInfinity(velocity))
                velocity = 0f;

            float omega = frequency * 2f * Mathf.PI;
            float k = omega * omega;
            float c = 2f * damping * omega;
            float displacement = current - target;
            float acceleration = -k * displacement - c * velocity;
            velocity += acceleration * deltaTime;
            velocity = Mathf.Clamp(velocity, -MaxAbsVelocity, MaxAbsVelocity);
            current += velocity * deltaTime;
            return current;
        }

        public static float OutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            t = Mathf.Clamp01(t);
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        public static float OutElastic(float t, float amplitude = 0.08f)
        {
            t = Mathf.Clamp01(t);
            if (t <= 0f)
                return 0f;
            if (t >= 1f)
                return 1f;
            return 1f + amplitude * Mathf.Sin(t * Mathf.PI * 3.5f) * (1f - t);
        }

        public static float Smooth(float current, float target, float deltaTime, float speed = 12f)
        {
            if (deltaTime <= 0f)
                return current;
            return Mathf.Lerp(current, target, 1f - Mathf.Exp(-speed * deltaTime));
        }
    }
}
