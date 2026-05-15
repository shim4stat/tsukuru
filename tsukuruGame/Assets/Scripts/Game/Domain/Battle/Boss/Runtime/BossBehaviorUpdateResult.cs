using System;
using System.Collections.Generic;
using System.Numerics;
using Game.Contracts.MasterData.Models;

namespace Game.Domain.Battle
{
    public readonly struct BossActionCommandEvent
    {
        public BossActionCommandEvent(
            string actionId,
            BossActionCommandType commandType,
            int triggerFrame,
            string animationStateName,
            int crossFadeFrames,
            string enemyDefinitionId,
            Vector3 spawnOffset,
            string effectId,
            Vector3 effectLocalOffset,
            string soundId,
            float volumeScale)
        {
            ActionId = actionId ?? string.Empty;
            CommandType = commandType;
            TriggerFrame = triggerFrame;
            AnimationStateName = animationStateName ?? string.Empty;
            CrossFadeFrames = crossFadeFrames;
            EnemyDefinitionId = enemyDefinitionId ?? string.Empty;
            SpawnOffset = spawnOffset;
            EffectId = effectId ?? string.Empty;
            EffectLocalOffset = effectLocalOffset;
            SoundId = soundId ?? string.Empty;
            VolumeScale = volumeScale;
        }

        public string ActionId { get; }

        public BossActionCommandType CommandType { get; }

        public int TriggerFrame { get; }

        public string AnimationStateName { get; }

        public int CrossFadeFrames { get; }

        public string EnemyDefinitionId { get; }

        public Vector3 SpawnOffset { get; }

        public string EffectId { get; }

        public Vector3 EffectLocalOffset { get; }

        public string SoundId { get; }

        public float VolumeScale { get; }
    }

    public readonly struct BossActiveHitbox
    {
        public BossActiveHitbox(string id, Vector3 offset, float radius, int damage)
        {
            Id = id ?? string.Empty;
            Offset = offset;
            Radius = radius;
            Damage = damage;
        }

        public string Id { get; }

        public Vector3 Offset { get; }

        public float Radius { get; }

        public int Damage { get; }
    }

    public readonly struct BossActiveHurtbox
    {
        public BossActiveHurtbox(string id, Vector3 offset, float radius)
        {
            Id = id ?? string.Empty;
            Offset = offset;
            Radius = radius;
        }

        public string Id { get; }

        public Vector3 Offset { get; }

        public float Radius { get; }
    }

    public readonly struct BossActionFrameState
    {
        private static readonly IReadOnlyList<BossActiveHitbox> EmptyHitboxes = Array.Empty<BossActiveHitbox>();
        private static readonly IReadOnlyList<BossActiveHurtbox> EmptyHurtboxes = Array.Empty<BossActiveHurtbox>();
        private static readonly IReadOnlyList<string> EmptyCancelTags = Array.Empty<string>();

        public static BossActionFrameState Empty => new BossActionFrameState(
            EmptyHitboxes,
            EmptyHurtboxes,
            false,
            Vector3.Zero,
            EmptyCancelTags,
            false);

        public BossActionFrameState(
            IReadOnlyList<BossActiveHitbox> activeHitboxes,
            IReadOnlyList<BossActiveHurtbox> activeHurtboxes,
            bool isInvincible,
            Vector3 moveVelocityPerSecond,
            IReadOnlyList<string> activeCancelTags,
            bool usesExplicitHurtboxWindows)
        {
            ActiveHitboxes = activeHitboxes ?? EmptyHitboxes;
            ActiveHurtboxes = activeHurtboxes ?? EmptyHurtboxes;
            IsInvincible = isInvincible;
            MoveVelocityPerSecond = moveVelocityPerSecond;
            ActiveCancelTags = activeCancelTags ?? EmptyCancelTags;
            UsesExplicitHurtboxWindows = usesExplicitHurtboxWindows;
        }

        public IReadOnlyList<BossActiveHitbox> ActiveHitboxes { get; }

        public IReadOnlyList<BossActiveHurtbox> ActiveHurtboxes { get; }

        public bool IsInvincible { get; }

        public Vector3 MoveVelocityPerSecond { get; }

        public IReadOnlyList<string> ActiveCancelTags { get; }

        public bool UsesExplicitHurtboxWindows { get; }
    }

    /// <summary>
    /// ボス挙動更新で戦闘進行側へ通知するイベント種別。
    /// </summary>
    public enum BossBehaviorSignal
    {
        None = 0,
        IntroCompleted = 1,
        DeadCompleted = 2,
    }

    /// <summary>
    /// 1 フレーム分のボス挙動更新結果。
    /// 弾生成要求と、演出や進行に使うシグナルをまとめて返す。
    /// </summary>
    public readonly struct BossBehaviorUpdateResult
    {
        private static readonly IReadOnlyList<EnemyBulletSpawnRequest> EmptyRequests = Array.Empty<EnemyBulletSpawnRequest>();
        private static readonly IReadOnlyList<BossActionCommandEvent> EmptyCommandEvents = Array.Empty<BossActionCommandEvent>();

        public static BossBehaviorUpdateResult Empty => new BossBehaviorUpdateResult(
            EmptyRequests,
            EmptyCommandEvents,
            BossBehaviorSignal.None,
            BossActionFrameState.Empty);

        public BossBehaviorUpdateResult(
            IReadOnlyList<EnemyBulletSpawnRequest> spawnRequests,
            IReadOnlyList<BossActionCommandEvent> commandEvents,
            BossBehaviorSignal signal,
            BossActionFrameState frameState)
        {
            SpawnRequests = spawnRequests ?? throw new ArgumentNullException(nameof(spawnRequests));
            CommandEvents = commandEvents ?? EmptyCommandEvents;
            Signal = signal;
            FrameState = frameState;
        }

        public IReadOnlyList<EnemyBulletSpawnRequest> SpawnRequests { get; }

        public IReadOnlyList<BossActionCommandEvent> CommandEvents { get; }

        public BossBehaviorSignal Signal { get; }

        public BossActionFrameState FrameState { get; }
    }
}
