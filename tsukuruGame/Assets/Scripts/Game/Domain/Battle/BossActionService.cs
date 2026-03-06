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
        private static readonly Vector3 DefaultSpawnPosition = new Vector3(0f, 4f, 0f);
        private static readonly Vector3 DefaultFireDirection = new Vector3(0f, -1f, 0f);

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
            if (bossParams.ActionIntervalSeconds <= 0f)
                throw new InvalidOperationException(
                    $"Boss action interval must be positive. actionIntervalSeconds={bossParams.ActionIntervalSeconds}");
            if (bossParams.GaugeMaxHps == null || bossParams.GaugeMaxHps.Count == 0)
                throw new InvalidOperationException("Boss gaugeMaxHps is null or empty.");

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
            int gaugeCount = bossParams.GaugeMaxHps.Count;
            IBossAttackPattern[] patterns = new IBossAttackPattern[gaugeCount];
            EnemyBulletSpawnRequest straightShot = CreateStraightShotRequest(DefaultFireDirection);

            for (int i = 0; i < gaugeCount; i++)
            {
                if (i == 0)
                {
                    patterns[i] = new SingleShotPattern(bossParams.ActionIntervalSeconds, straightShot);
                    continue;
                }

                if (i == gaugeCount - 1)
                {
                    patterns[i] = new BurstShotPattern(
                        bossParams.ActionIntervalSeconds,
                        DefaultBurstShotCount,
                        DefaultBurstShotIntervalSeconds,
                        straightShot);
                    continue;
                }

                patterns[i] = new NWayShotPattern(
                    bossParams.ActionIntervalSeconds,
                    DefaultNWayShotCount,
                    DefaultNWaySpreadDegrees,
                    DefaultSpawnPosition,
                    DefaultFireDirection,
                    DefaultBulletSpeed,
                    DefaultBulletDamage,
                    DefaultBulletLifetimeSeconds,
                    DefaultBulletAbsorbableEnergyAmount,
                    EnemyBulletBehaviorType.Straight);
            }

            return patterns;
        }

        private static EnemyBulletSpawnRequest CreateStraightShotRequest(Vector3 direction)
        {
            Vector3 normalizedDirection = direction.LengthSquared() > 0f
                ? Vector3.Normalize(direction)
                : DefaultFireDirection;

            return new EnemyBulletSpawnRequest(
                DefaultSpawnPosition,
                normalizedDirection * DefaultBulletSpeed,
                DefaultBulletDamage,
                DefaultBulletLifetimeSeconds,
                DefaultBulletAbsorbableEnergyAmount,
                EnemyBulletBehaviorType.Straight);
        }
    }
}
