using System;
using System.Numerics;

namespace Game.Domain.Battle
{
    internal readonly struct BossActionBulletConfig
    {
        public BossActionBulletConfig(
            Vector3 spawnOffset,
            float speed,
            int damage,
            float lifetimeSeconds,
            int absorbableEnergyAmount,
            EnemyBulletBehaviorType behaviorType = EnemyBulletBehaviorType.Straight)
        {
            if (speed <= 0f)
                throw new ArgumentOutOfRangeException(nameof(speed), "speed must be positive.");
            if (damage < 0)
                throw new ArgumentOutOfRangeException(nameof(damage), "damage must be non-negative.");
            if (lifetimeSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(lifetimeSeconds), "lifetimeSeconds must be positive.");
            if (absorbableEnergyAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(absorbableEnergyAmount), "absorbableEnergyAmount must be non-negative.");

            SpawnOffset = spawnOffset;
            Speed = speed;
            Damage = damage;
            LifetimeSeconds = lifetimeSeconds;
            AbsorbableEnergyAmount = absorbableEnergyAmount;
            BehaviorType = behaviorType;
        }

        public Vector3 SpawnOffset { get; }

        public float Speed { get; }

        public int Damage { get; }

        public float LifetimeSeconds { get; }

        public int AbsorbableEnergyAmount { get; }

        public EnemyBulletBehaviorType BehaviorType { get; }

        public EnemyBulletSpawnRequest CreateSpawnRequest(Vector3 bossPosition, Vector3 direction)
        {
            if (direction.LengthSquared() <= 0f)
                throw new ArgumentOutOfRangeException(nameof(direction), "direction must be non-zero.");

            Vector3 normalizedDirection = Vector3.Normalize(direction);
            return new EnemyBulletSpawnRequest(
                bossPosition + SpawnOffset,
                normalizedDirection * Speed,
                Damage,
                LifetimeSeconds,
                AbsorbableEnergyAmount,
                BehaviorType);
        }
    }

    internal static class BossActionMath
    {
        public const float Tau = (float)(Math.PI * 2.0);

        public static float Clamp01(float value)
        {
            if (value <= 0f)
                return 0f;
            if (value >= 1f)
                return 1f;

            return value;
        }

        public static Vector3 DirectionFromDegrees(float degrees)
        {
            float radians = degrees * (float)Math.PI / 180f;
            return new Vector3((float)Math.Cos(radians), (float)Math.Sin(radians), 0f);
        }

        public static Vector3 Lerp(Vector3 from, Vector3 to, float t)
        {
            return from + ((to - from) * Clamp01(t));
        }

        public static float SmoothStep01(float t)
        {
            float clamped = Clamp01(t);
            return clamped * clamped * (3f - (2f * clamped));
        }

        public static Vector3 RotateAroundZ(Vector3 vector, float degrees)
        {
            float radians = degrees * (float)Math.PI / 180f;
            float cos = (float)Math.Cos(radians);
            float sin = (float)Math.Sin(radians);

            return new Vector3(
                (vector.X * cos) - (vector.Y * sin),
                (vector.X * sin) + (vector.Y * cos),
                vector.Z);
        }
    }
}
