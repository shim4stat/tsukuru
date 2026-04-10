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
        CurrentActionCompleted = 3,
        CurrentGaugeIndexAtOrAbove = 4,
    }

    public enum BossActionEndConditionType
    {
        Manual = 0,
        DurationElapsed = 1,
    }

    public enum BossActionCancelPolicy
    {
        AlwaysCancelable = 0,
        Windowed = 1,
    }

    public enum BossActionCommandType
    {
        PlayAnimation = 0,
        SpawnBulletPattern = 1,
        SpawnEnemy = 2,
        PlayEffect = 3,
        PlaySound = 4,
        EmitSignal = 5,
    }

    public enum BossActionWindowType
    {
        MoveWindow = 0,
        HitboxWindow = 1,
        HurtboxWindow = 2,
        InvincibleWindow = 3,
        CancelWindow = 4,
    }

    public static class BossStateSignalIds
    {
        public const string IntroFinished = "intro_finished";
    }

    public static class BossActionTimelineConstants
    {
        public const int FramesPerSecond = 60;
    }

    public sealed class BossBulletPatternDefinitionContract
    {
        public BossAttackPatternType PatternType { get; set; } = BossAttackPatternType.SingleShot;

        public int InitialDelayFrames { get; set; } = -1;

        public int FireIntervalFrames { get; set; } = BossActionTimelineConstants.FramesPerSecond;

        public int ShotCount { get; set; } = 1;

        public float SpreadDegrees { get; set; }

        public int BurstShotCount { get; set; } = 3;

        public int BurstShotIntervalFrames { get; set; } = 9;

        public float BulletSpeed { get; set; } = 3.0f;

        public float BulletLifetimeSeconds { get; set; } = 2.0f;

        public int BulletDamage { get; set; } = 1;

        public int AbsorbableEnergyAmount { get; set; } = 1;

        public EnemyBulletBehaviorTypeContract BulletBehaviorType { get; set; } = EnemyBulletBehaviorTypeContract.Straight;

        public Vector3 SpawnOffset { get; set; } = Vector3.Zero;

        public Vector3 FireDirection { get; set; } = new Vector3(0f, -1f, 0f);
    }

    public sealed class BossActionCommandContract
    {
        public int TriggerFrame { get; set; }

        public BossActionCommandType CommandType { get; set; } = BossActionCommandType.SpawnBulletPattern;

        public string AnimationStateName { get; set; } = string.Empty;

        public BossBulletPatternDefinitionContract BulletPattern { get; set; }

        public int? EmitterDurationFrames { get; set; }

        public string SignalId { get; set; } = string.Empty;

        public int CrossFadeFrames { get; set; }

        public string EnemyDefinitionId { get; set; } = string.Empty;

        public Vector3 SpawnOffset { get; set; } = Vector3.Zero;

        public string EffectId { get; set; } = string.Empty;

        public Vector3 EffectLocalOffset { get; set; } = Vector3.Zero;

        public string SoundId { get; set; } = string.Empty;

        public float VolumeScale { get; set; } = 1.0f;
    }

    public sealed class BossMoveWindowPayloadContract
    {
        public Vector3 VelocityPerSecond { get; set; } = Vector3.Zero;
    }

    public sealed class BossHitboxWindowPayloadContract
    {
        public Vector3 Offset { get; set; } = Vector3.Zero;

        public float Radius { get; set; } = 0.5f;

        public int Damage { get; set; } = 1;
    }

    public sealed class BossHurtboxWindowPayloadContract
    {
        public Vector3 Offset { get; set; } = Vector3.Zero;

        public float Radius { get; set; } = 0.5f;
    }

    public sealed class BossCancelWindowPayloadContract
    {
        public string CancelTag { get; set; } = string.Empty;
    }

    public sealed class BossActionWindowContract
    {
        public string Id { get; set; } = string.Empty;

        public BossActionWindowType WindowType { get; set; } = BossActionWindowType.MoveWindow;

        public int StartFrameInclusive { get; set; }

        public int EndFrameExclusive { get; set; }

        public BossMoveWindowPayloadContract MoveWindow { get; set; }

        public BossHitboxWindowPayloadContract HitboxWindow { get; set; }

        public BossHurtboxWindowPayloadContract HurtboxWindow { get; set; }

        public BossCancelWindowPayloadContract CancelWindow { get; set; }
    }

    public sealed class BossActionDefinitionContract
    {
        public string Id { get; set; } = string.Empty;

        public string AnimationStateName { get; set; } = string.Empty;

        public BossActionEndConditionType EndConditionType { get; set; } = BossActionEndConditionType.Manual;

        public BossActionCancelPolicy CancelPolicy { get; set; } = BossActionCancelPolicy.AlwaysCancelable;

        public int TotalDurationFrames { get; set; }

        public IReadOnlyList<BossActionCommandContract> Commands { get; set; } = Array.Empty<BossActionCommandContract>();

        public IReadOnlyList<BossActionWindowContract> Windows { get; set; } = Array.Empty<BossActionWindowContract>();
    }

    public sealed class BossActionPlanContract
    {
        public IReadOnlyList<string> OpeningSequenceActionIds { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> RandomActionIds { get; set; } = Array.Empty<string>();

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

        public BossActionPlanContract ActionPlan { get; set; } = new BossActionPlanContract();

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

        public string InitialStateId { get; set; } = string.Empty;

        public IReadOnlyList<BossStateDefinitionContract> States { get; set; } = Array.Empty<BossStateDefinitionContract>();

        public IReadOnlyList<BossActionDefinitionContract> Actions { get; set; } = Array.Empty<BossActionDefinitionContract>();
    }
}
