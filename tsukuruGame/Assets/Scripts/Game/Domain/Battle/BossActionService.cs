using System;
using System.Collections.Generic;
using System.Numerics;
using Game.Contracts.MasterData.Models;

namespace Game.Domain.Battle
{
    /// <summary>
    /// Produces boss action spawn requests on a timer while in Combat.
    /// </summary>
    public sealed class BossActionService
    {
        private static readonly IReadOnlyList<EnemyBulletSpawnRequest> EmptyRequests = Array.Empty<EnemyBulletSpawnRequest>();
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

        private Boss _boss;
        private IBossAttackPattern[] _patternsByGauge = Array.Empty<IBossAttackPattern>();
        private int _activeGaugeIndex = -1;
        private bool _isInitialized;

        public void Initialize(Boss boss, BossParamsContract bossParams)
        {
            if (boss == null)
                throw new ArgumentNullException(nameof(boss));
            if (bossParams == null)
                throw new ArgumentNullException(nameof(bossParams));
            if (bossParams.GaugeMaxHps == null || bossParams.GaugeMaxHps.Count == 0)
                throw new InvalidOperationException("Boss gaugeMaxHps is null or empty.");
            if ((bossParams.PhasePatterns == null || bossParams.PhasePatterns.Count == 0) && bossParams.ActionIntervalSeconds <= 0f)
            {
                throw new InvalidOperationException(
                    $"Boss action interval must be positive when fallback patterns are used. actionIntervalSeconds={bossParams.ActionIntervalSeconds}");
            }

            _boss = boss;
            _patternsByGauge = BuildPatternsByGauge(bossParams);
            _activeGaugeIndex = -1;
            _isInitialized = true;
        }

        public IReadOnlyList<EnemyBulletSpawnRequest> Update(BattleContext context, float deltaTime)
        {
            EnsureInitialized();

            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (context.Boss == null)
                throw new InvalidOperationException("BattleContext.Boss is not initialized.");
            if (!ReferenceEquals(context.Boss, _boss))
                throw new InvalidOperationException("BattleContext.Boss does not match initialized boss.");

            if (context.Phase != BattlePhase.Combat)
                return EmptyRequests;
            if (context.Boss.IsAllGaugesEmpty())
                return EmptyRequests;
            if (deltaTime <= 0f)
                return EmptyRequests;

            int gaugeIndex = context.Boss.GetCurrentGaugeIndex();
            IBossAttackPattern pattern = GetPatternForGauge(gaugeIndex);
            if (_activeGaugeIndex != gaugeIndex)
            {
                pattern.Reset();
                _activeGaugeIndex = gaugeIndex;
            }

            return pattern.Update(context, deltaTime);
        }

        public void Reset()
        {
            for (int i = 0; i < _patternsByGauge.Length; i++)
            {
                _patternsByGauge[i]?.Reset();
            }

            _activeGaugeIndex = -1;
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("BossActionService is not initialized.");
        }

        private IBossAttackPattern GetPatternForGauge(int gaugeIndex)
        {
            if (gaugeIndex < 0 || gaugeIndex >= _patternsByGauge.Length)
            {
                throw new InvalidOperationException(
                    $"Boss gauge index is out of range. gaugeIndex={gaugeIndex}, patternCount={_patternsByGauge.Length}");
            }

            IBossAttackPattern pattern = _patternsByGauge[gaugeIndex];
            if (pattern == null)
                throw new InvalidOperationException($"Boss attack pattern is not configured for gaugeIndex={gaugeIndex}.");

            return pattern;
        }

        private static IBossAttackPattern[] BuildPatternsByGauge(BossParamsContract bossParams)
        {
            IReadOnlyList<BossPhasePatternContract> phasePatterns = bossParams.PhasePatterns;
            if (phasePatterns == null || phasePatterns.Count == 0)
            {
                phasePatterns = BuildFallbackPhasePatterns(bossParams);
            }

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

                patterns[i] = CreatePattern(phasePattern);
            }

            return patterns;
        }

        private static IReadOnlyList<BossPhasePatternContract> BuildFallbackPhasePatterns(BossParamsContract bossParams)
        {
            List<BossPhasePatternContract> phasePatterns = new List<BossPhasePatternContract>(bossParams.GaugeMaxHps.Count);
            for (int i = 0; i < bossParams.GaugeMaxHps.Count; i++)
            {
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

        private static IBossAttackPattern CreatePattern(BossPhasePatternContract phasePattern)
        {
            BossBulletPatternConfig bulletConfig = new BossBulletPatternConfig(
                phasePattern.SpawnOffset,
                phasePattern.FireDirection,
                phasePattern.BulletSpeed,
                phasePattern.BulletDamage,
                phasePattern.BulletLifetimeSeconds,
                phasePattern.AbsorbableEnergyAmount,
                MapBehaviorType(phasePattern.BulletBehaviorType));

            switch (phasePattern.PatternType)
            {
                case BossAttackPatternType.SingleShot:
                    return new SingleShotPattern(phasePattern.FireIntervalSeconds, bulletConfig);
                case BossAttackPatternType.NWayShot:
                    return new NWayShotPattern(
                        phasePattern.FireIntervalSeconds,
                        phasePattern.ShotCount,
                        phasePattern.SpreadDegrees,
                        bulletConfig);
                case BossAttackPatternType.BurstShot:
                    return new BurstShotPattern(
                        phasePattern.FireIntervalSeconds,
                        phasePattern.BurstShotCount,
                        phasePattern.BurstShotIntervalSeconds,
                        bulletConfig);
                default:
                    throw new InvalidOperationException($"Unsupported boss attack pattern type: {phasePattern.PatternType}");
            }
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
    }
}
