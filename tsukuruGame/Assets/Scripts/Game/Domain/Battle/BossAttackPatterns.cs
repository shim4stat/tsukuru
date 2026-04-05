using System;
using System.Collections.Generic;
using System.Numerics;
using Game.Contracts.MasterData.Models;

namespace Game.Domain.Battle
{
    /// <summary>
    /// フェーズ別の明示設定が無い場合に使う既定弾幕を構築する。
    /// </summary>
    internal static class BossPhasePatternDefaults
    {
        private static readonly Vector3 DefaultFireDirection = new Vector3(0f, -1f, 0f);
        private static readonly Vector3 DefaultSpawnOffset = Vector3.Zero;

        private const float DefaultBulletSpeed = 3.0f;
        private const float DefaultBulletLifetimeSeconds = 2.0f;
        private const int DefaultBulletDamage = 1;
        private const int DefaultBulletAbsorbableEnergyAmount = 1;
        private const int DefaultNWayShotCount = 3;
        private const float DefaultNWaySpreadDegrees = 30.0f;
        private const int DefaultBurstShotCount = 3;
        private const float DefaultBurstShotIntervalSeconds = 0.15f;

        public static IReadOnlyList<BossPhasePatternContract> BuildFallbackPhasePatterns(BossParamsContract bossParams)
        {
            if (bossParams == null)
                throw new ArgumentNullException(nameof(bossParams));
            if (bossParams.ActionIntervalSeconds <= 0f)
            {
                throw new InvalidOperationException(
                    $"Boss action interval must be positive when fallback patterns are used. actionIntervalSeconds={bossParams.ActionIntervalSeconds}");
            }

            List<BossPhasePatternContract> phasePatterns = new List<BossPhasePatternContract>(bossParams.GaugeMaxHps.Count);
            for (int i = 0; i < bossParams.GaugeMaxHps.Count; i++)
            {
                // 序盤は単発、中盤は拡散、最終ゲージは連射で圧を上げる単純な既定値。
                if (i == 0)
                {
                    phasePatterns.Add(
                        CreateSingleShotPhasePattern(
                            bossParams.ActionIntervalSeconds,
                            DefaultFireDirection));
                    continue;
                }

                if (i == bossParams.GaugeMaxHps.Count - 1)
                {
                    phasePatterns.Add(
                        CreateBurstShotPhasePattern(
                            bossParams.ActionIntervalSeconds,
                            DefaultBurstShotCount,
                            DefaultBurstShotIntervalSeconds,
                            DefaultFireDirection));
                    continue;
                }

                phasePatterns.Add(
                    CreateNWayShotPhasePattern(
                        bossParams.ActionIntervalSeconds,
                        DefaultNWayShotCount,
                        DefaultNWaySpreadDegrees,
                        DefaultFireDirection));
            }

            return phasePatterns;
        }

        private static BossPhasePatternContract CreateSingleShotPhasePattern(float fireIntervalSeconds, Vector3 fireDirection)
        {
            return CreateBasePhasePattern(BossAttackPatternType.SingleShot, fireIntervalSeconds, fireDirection);
        }

        private static BossPhasePatternContract CreateNWayShotPhasePattern(
            float fireIntervalSeconds,
            int shotCount,
            float spreadDegrees,
            Vector3 fireDirection)
        {
            BossPhasePatternContract phasePattern = CreateBasePhasePattern(BossAttackPatternType.NWayShot, fireIntervalSeconds, fireDirection);
            phasePattern.ShotCount = shotCount;
            phasePattern.SpreadDegrees = spreadDegrees;
            return phasePattern;
        }

        private static BossPhasePatternContract CreateBurstShotPhasePattern(
            float fireIntervalSeconds,
            int burstShotCount,
            float burstShotIntervalSeconds,
            Vector3 fireDirection)
        {
            BossPhasePatternContract phasePattern = CreateBasePhasePattern(BossAttackPatternType.BurstShot, fireIntervalSeconds, fireDirection);
            phasePattern.BurstShotCount = burstShotCount;
            phasePattern.BurstShotIntervalSeconds = burstShotIntervalSeconds;
            return phasePattern;
        }

        private static BossPhasePatternContract CreateBasePhasePattern(
            BossAttackPatternType patternType,
            float fireIntervalSeconds,
            Vector3 fireDirection)
        {
            Vector3 normalizedDirection = fireDirection.LengthSquared() > 0f
                ? Vector3.Normalize(fireDirection)
                : DefaultFireDirection;

            return new BossPhasePatternContract
            {
                PatternType = patternType,
                FireIntervalSeconds = fireIntervalSeconds,
                ShotCount = 1,
                SpreadDegrees = 0f,
                BurstShotCount = DefaultBurstShotCount,
                BurstShotIntervalSeconds = DefaultBurstShotIntervalSeconds,
                BulletSpeed = DefaultBulletSpeed,
                BulletLifetimeSeconds = DefaultBulletLifetimeSeconds,
                BulletDamage = DefaultBulletDamage,
                AbsorbableEnergyAmount = DefaultBulletAbsorbableEnergyAmount,
                BulletBehaviorType = EnemyBulletBehaviorTypeContract.Straight,
                SpawnOffset = DefaultSpawnOffset,
                FireDirection = normalizedDirection,
            };
        }
    }

    /// <summary>
    /// マスターデータ上の攻撃定義をランタイムの弾幕パターンへ変換するファクトリ。
    /// </summary>
    internal static class BossAttackPatternFactory
    {
        public static IBossAttackPattern BuildPattern(BossAttackDefinitionContract attackDefinition)
        {
            if (attackDefinition == null)
                throw new ArgumentNullException(nameof(attackDefinition));

            BossPhasePatternContract phasePattern = new BossPhasePatternContract
            {
                PatternType = attackDefinition.PatternType,
                FireIntervalSeconds = attackDefinition.FireIntervalSeconds,
                ShotCount = attackDefinition.ShotCount,
                SpreadDegrees = attackDefinition.SpreadDegrees,
                BurstShotCount = attackDefinition.BurstShotCount,
                BurstShotIntervalSeconds = attackDefinition.BurstShotIntervalSeconds,
                BulletSpeed = attackDefinition.BulletSpeed,
                BulletLifetimeSeconds = attackDefinition.BulletLifetimeSeconds,
                BulletDamage = attackDefinition.BulletDamage,
                AbsorbableEnergyAmount = attackDefinition.AbsorbableEnergyAmount,
                BulletBehaviorType = attackDefinition.BulletBehaviorType,
                SpawnOffset = attackDefinition.SpawnOffset,
                FireDirection = attackDefinition.FireDirection,
            };

            return Create(phasePattern);
        }

        public static IBossAttackPattern[] BuildPatternsByGauge(BossParamsContract bossParams)
        {
            if (bossParams == null)
                throw new ArgumentNullException(nameof(bossParams));

            IReadOnlyList<BossPhasePatternContract> phasePatterns = bossParams.PhasePatterns;
            if (phasePatterns == null || phasePatterns.Count == 0)
                phasePatterns = BossPhasePatternDefaults.BuildFallbackPhasePatterns(bossParams);

            if (phasePatterns.Count != bossParams.GaugeMaxHps.Count)
            {
                throw new InvalidOperationException(
                    $"Boss phase pattern count must match gauge count. gaugeCount={bossParams.GaugeMaxHps.Count}, phasePatternCount={phasePatterns.Count}");
            }

            IBossAttackPattern[] patterns = new IBossAttackPattern[phasePatterns.Count];
            for (int i = 0; i < phasePatterns.Count; i++)
            {
                BossPhasePatternContract phasePattern = phasePatterns[i];
                if (phasePattern == null)
                    throw new InvalidOperationException($"Boss phase pattern is null at index {i}.");

                patterns[i] = Create(phasePattern);
            }

            return patterns;
        }

        private static IBossAttackPattern Create(BossPhasePatternContract phasePattern)
        {
            // 契約モデルを具体的なパターン実装へ解決する。
            BossBulletPatternConfig bulletConfig = CreateBulletConfig(phasePattern);
            switch (phasePattern.PatternType)
            {
                case BossAttackPatternType.SingleShot:
                    ValidateSingleShot(phasePattern);
                    return new SingleShotPattern(phasePattern.FireIntervalSeconds, bulletConfig);
                case BossAttackPatternType.NWayShot:
                    ValidateNWayShot(phasePattern);
                    return new NWayShotPattern(
                        phasePattern.FireIntervalSeconds,
                        phasePattern.ShotCount,
                        phasePattern.SpreadDegrees,
                        bulletConfig);
                case BossAttackPatternType.BurstShot:
                    ValidateBurstShot(phasePattern);
                    return new BurstShotPattern(
                        phasePattern.FireIntervalSeconds,
                        phasePattern.BurstShotCount,
                        phasePattern.BurstShotIntervalSeconds,
                        bulletConfig);
                default:
                    throw new InvalidOperationException($"Unsupported boss attack pattern type: {phasePattern.PatternType}");
            }
        }

        private static BossBulletPatternConfig CreateBulletConfig(BossPhasePatternContract phasePattern)
        {
            return new BossBulletPatternConfig(
                phasePattern.SpawnOffset,
                phasePattern.FireDirection,
                phasePattern.BulletSpeed,
                phasePattern.BulletDamage,
                phasePattern.BulletLifetimeSeconds,
                phasePattern.AbsorbableEnergyAmount,
                MapBehaviorType(phasePattern.BulletBehaviorType));
        }

        private static void ValidateSingleShot(BossPhasePatternContract phasePattern)
        {
            if (phasePattern.FireIntervalSeconds <= 0f)
            {
                throw new InvalidOperationException(
                    $"SingleShot fire interval must be positive. fireIntervalSeconds={phasePattern.FireIntervalSeconds}");
            }
        }

        private static void ValidateNWayShot(BossPhasePatternContract phasePattern)
        {
            if (phasePattern.FireIntervalSeconds <= 0f)
            {
                throw new InvalidOperationException(
                    $"NWayShot fire interval must be positive. fireIntervalSeconds={phasePattern.FireIntervalSeconds}");
            }

            if (phasePattern.ShotCount <= 0)
            {
                throw new InvalidOperationException(
                    $"NWayShot shot count must be positive. shotCount={phasePattern.ShotCount}");
            }
        }

        private static void ValidateBurstShot(BossPhasePatternContract phasePattern)
        {
            if (phasePattern.FireIntervalSeconds <= 0f)
            {
                throw new InvalidOperationException(
                    $"BurstShot fire interval must be positive. fireIntervalSeconds={phasePattern.FireIntervalSeconds}");
            }

            if (phasePattern.BurstShotCount <= 0)
            {
                throw new InvalidOperationException(
                    $"BurstShot burst shot count must be positive. burstShotCount={phasePattern.BurstShotCount}");
            }

            if (phasePattern.BurstShotIntervalSeconds <= 0f)
            {
                throw new InvalidOperationException(
                    $"BurstShot interval must be positive. burstShotIntervalSeconds={phasePattern.BurstShotIntervalSeconds}");
            }
        }

        private static EnemyBulletBehaviorType MapBehaviorType(EnemyBulletBehaviorTypeContract behaviorType)
        {
            // マスターデータの列挙値をドメイン側の列挙値へ写像する。
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
    }
}
