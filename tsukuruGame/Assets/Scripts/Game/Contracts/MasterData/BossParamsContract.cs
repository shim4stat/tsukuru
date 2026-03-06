using System;
using System.Collections.Generic;
using System.Numerics;

namespace Game.Contracts.MasterData.Models
{
    public enum BossAttackPatternType
    {
        SingleShot = 0,
        NWayShot = 1,
        BurstShot = 2,
    }

    public enum EnemyBulletBehaviorTypeContract
    {
        Straight = 0,
        Wave = 1,
        Homing = 2,
    }

    public sealed class BossPhasePatternContract
    {
        public BossAttackPatternType PatternType { get; set; } = BossAttackPatternType.SingleShot;

        public float FireIntervalSeconds { get; set; } = 1.0f;

        public int ShotCount { get; set; } = 1;

        public float SpreadDegrees { get; set; }

        public int BurstShotCount { get; set; } = 3;

        public float BurstShotIntervalSeconds { get; set; } = 0.15f;

        public float BulletSpeed { get; set; } = 3.0f;

        public float BulletLifetimeSeconds { get; set; } = 2.0f;

        public int BulletDamage { get; set; } = 1;

        public int AbsorbableEnergyAmount { get; set; } = 1;

        public EnemyBulletBehaviorTypeContract BulletBehaviorType { get; set; } = EnemyBulletBehaviorTypeContract.Straight;

        public Vector3 SpawnOffset { get; set; } = Vector3.Zero;

        public Vector3 FireDirection { get; set; } = new Vector3(0f, -1f, 0f);
    }

    /// <summary>
    /// Static boss parameters needed by battle logic.
    /// </summary>
    public sealed class BossParamsContract
    {
        public string Id { get; set; } = string.Empty;

        public IReadOnlyList<int> GaugeMaxHps { get; set; } = Array.Empty<int>();

        public int BaseDropEnergyAmount { get; set; }

        public float MinDropIntervalSeconds { get; set; }

        public float ActionIntervalSeconds { get; set; }

        public IReadOnlyList<BossPhasePatternContract> PhasePatterns { get; set; } = Array.Empty<BossPhasePatternContract>();
    }
}
