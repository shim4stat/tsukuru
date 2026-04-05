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

    public enum BossStateType
    {
        Intro = 0,
        Phase = 1,
        Dead = 2,
    }

    public enum BossTransitionConditionType
    {
        ExternalSignal = 0,
        ElapsedTime = 1,
        CurrentHpRateAtOrBelow = 2,
        CurrentAttackCompleted = 3,
        CurrentGaugeIndexAtOrAbove = 4,
    }

    public static class BossStateSignalIds
    {
        public const string IntroFinished = "intro_finished";
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

    public sealed class BossAttackDefinitionContract
    {
        public string Id { get; set; } = string.Empty;

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

        public float ActiveDurationSeconds { get; set; } = 1.0f;
    }

    public sealed class BossAttackPlanContract
    {
        public IReadOnlyList<string> OpeningSequenceAttackIds { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> RandomAttackIds { get; set; } = Array.Empty<string>();

        public int HistoryWindow { get; set; } = 2;
    }

    public sealed class BossStateTransitionContract
    {
        public string NextStateId { get; set; } = string.Empty;

        public BossTransitionConditionType ConditionType { get; set; } = BossTransitionConditionType.ExternalSignal;

        public float Threshold { get; set; }

        public string SignalId { get; set; } = string.Empty;
    }

    public sealed class BossStateDefinitionContract
    {
        public string Id { get; set; } = string.Empty;

        public BossStateType StateType { get; set; } = BossStateType.Phase;

        public BossAttackPlanContract AttackPlan { get; set; } = new BossAttackPlanContract();

        public IReadOnlyList<BossStateTransitionContract> Transitions { get; set; } = Array.Empty<BossStateTransitionContract>();
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

        public string InitialStateId { get; set; } = string.Empty;

        public IReadOnlyList<BossStateDefinitionContract> States { get; set; } = Array.Empty<BossStateDefinitionContract>();

        public IReadOnlyList<BossAttackDefinitionContract> Attacks { get; set; } = Array.Empty<BossAttackDefinitionContract>();

        public IReadOnlyList<BossPhasePatternContract> PhasePatterns { get; set; } = Array.Empty<BossPhasePatternContract>();
    }
}
