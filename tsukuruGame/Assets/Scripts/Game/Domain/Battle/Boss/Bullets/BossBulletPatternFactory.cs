using System;
using System.Numerics;
using Game.Contracts.MasterData.Models;

namespace Game.Domain.Battle
{
    /// <summary>
    /// マスターデータ上の弾幕定義をランタイムの弾幕パターンへ変換するファクトリ。
    /// </summary>
    internal static class BossBulletPatternFactory
    {
        public static IBossAttackPattern BuildPattern(BossBulletPatternDefinitionContract definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            ValidatePatternDefinition(definition);

            BossBulletPatternConfig bulletConfig = CreateBulletConfig(definition);
            switch (definition.PatternType)
            {
                case BossAttackPatternType.SingleShot:
                    return new SingleShotPattern(FramesToSeconds(definition.FireIntervalFrames), bulletConfig);
                case BossAttackPatternType.NWayShot:
                    return new NWayShotPattern(
                        FramesToSeconds(definition.FireIntervalFrames),
                        definition.ShotCount,
                        definition.SpreadDegrees,
                        bulletConfig);
                case BossAttackPatternType.BurstShot:
                    return new BurstShotPattern(
                        FramesToSeconds(definition.FireIntervalFrames),
                        definition.BurstShotCount,
                        FramesToSeconds(definition.BurstShotIntervalFrames),
                        bulletConfig);
                default:
                    throw new InvalidOperationException($"Unsupported boss attack pattern type: {definition.PatternType}");
            }
        }

        public static void ValidatePatternDefinition(BossBulletPatternDefinitionContract definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (definition.InitialDelayFrames < -1)
            {
                throw new InvalidOperationException(
                    $"Bullet pattern initial delay must be -1 or non-negative. initialDelayFrames={definition.InitialDelayFrames}");
            }
            if (definition.FireIntervalFrames <= 0)
            {
                throw new InvalidOperationException(
                    $"Bullet pattern fire interval must be positive. fireIntervalFrames={definition.FireIntervalFrames}");
            }
            if (definition.BurstShotIntervalFrames <= 0)
            {
                throw new InvalidOperationException(
                    $"Bullet pattern burst interval must be positive. burstShotIntervalFrames={definition.BurstShotIntervalFrames}");
            }
            if (definition.BulletSpeed <= 0f)
                throw new InvalidOperationException($"Bullet speed must be positive. bulletSpeed={definition.BulletSpeed}");
            if (definition.BulletLifetimeSeconds <= 0f)
            {
                throw new InvalidOperationException(
                    $"Bullet lifetime must be positive. bulletLifetimeSeconds={definition.BulletLifetimeSeconds}");
            }
            if (definition.FireDirection.LengthSquared() <= 0f)
                throw new InvalidOperationException("Bullet pattern fire direction must be non-zero.");

            switch (definition.PatternType)
            {
                case BossAttackPatternType.SingleShot:
                    break;
                case BossAttackPatternType.NWayShot:
                    if (definition.ShotCount <= 0)
                    {
                        throw new InvalidOperationException(
                            $"NWayShot shot count must be positive. shotCount={definition.ShotCount}");
                    }

                    break;
                case BossAttackPatternType.BurstShot:
                    if (definition.BurstShotCount <= 0)
                    {
                        throw new InvalidOperationException(
                            $"BurstShot burst shot count must be positive. burstShotCount={definition.BurstShotCount}");
                    }

                    break;
                default:
                    throw new InvalidOperationException($"Unsupported boss attack pattern type: {definition.PatternType}");
            }
        }

        private static BossBulletPatternConfig CreateBulletConfig(BossBulletPatternDefinitionContract definition)
        {
            return new BossBulletPatternConfig(
                definition.SpawnOffset,
                definition.FireDirection,
                definition.BulletSpeed,
                definition.BulletDamage,
                definition.BulletLifetimeSeconds,
                definition.AbsorbableEnergyAmount,
                MapBehaviorType(definition.BulletBehaviorType));
        }

        private static EnemyBulletBehaviorType MapBehaviorType(EnemyBulletBehaviorTypeContract behaviorType)
        {
            switch (behaviorType)
            {
                case EnemyBulletBehaviorTypeContract.Straight:
                    return EnemyBulletBehaviorType.Straight;
                case EnemyBulletBehaviorTypeContract.Wave:
                    return EnemyBulletBehaviorType.Wave;
                case EnemyBulletBehaviorTypeContract.Homing:
                    return EnemyBulletBehaviorType.Homing;
                default:
                    throw new InvalidOperationException($"Unsupported enemy bullet behavior type: {behaviorType}");
            }
        }

        private static float FramesToSeconds(int frames)
        {
            return Math.Max(1, frames) / (float)BossActionTimelineConstants.FramesPerSecond;
        }
    }
}
