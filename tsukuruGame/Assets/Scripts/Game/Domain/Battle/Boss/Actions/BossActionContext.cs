using System;
using System.Collections.Generic;
using System.Numerics;
using Game.Contracts.MasterData.Models;

namespace Game.Domain.Battle
{
    internal sealed class BossActionContext
    {
        private static readonly IReadOnlyList<EnemyBulletSpawnRequest> EmptySpawnRequests = Array.Empty<EnemyBulletSpawnRequest>();
        private static readonly IReadOnlyList<string> EmptySignals = Array.Empty<string>();
        private static readonly IReadOnlyList<BossActionCommandEvent> EmptyCommandEvents = Array.Empty<BossActionCommandEvent>();
        private static readonly IReadOnlyList<BossActiveHitbox> EmptyHitboxes = Array.Empty<BossActiveHitbox>();
        private static readonly IReadOnlyList<BossActiveHurtbox> EmptyHurtboxes = Array.Empty<BossActiveHurtbox>();
        private static readonly IReadOnlyList<string> EmptyCancelTags = Array.Empty<string>();

        private readonly string _actionId;
        private readonly BattleContext _context;
        private readonly int _durationFrames;
        private List<EnemyBulletSpawnRequest> _spawnRequests;
        private List<string> _emittedSignals;
        private List<BossActionCommandEvent> _commandEvents;
        private List<BossActiveHitbox> _activeHitboxes;
        private List<BossActiveHurtbox> _activeHurtboxes;
        private List<string> _activeCancelTags;
        private bool _isInvincible;
        private bool _usesExplicitHurtboxWindows;
        private Vector3 _moveVelocityPerSecond;

        public BossActionContext(string actionId, BattleContext context, int elapsedFrames, int durationFrames)
        {
            if (string.IsNullOrWhiteSpace(actionId))
                throw new ArgumentException("actionId is null or empty.", nameof(actionId));
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (context.Boss == null)
                throw new InvalidOperationException("BattleContext.Boss is not initialized.");

            _actionId = actionId;
            _context = context;
            ElapsedFrames = Math.Max(0, elapsedFrames);
            _durationFrames = Math.Max(1, durationFrames);
        }

        public int ElapsedFrames { get; }

        public float Progress01
        {
            get
            {
                if (_durationFrames <= 1)
                    return 1f;

                return BossActionMath.Clamp01(ElapsedFrames / (float)(_durationFrames - 1));
            }
        }

        public Boss Boss => _context.Boss;

        public Player Player => _context.Player;

        public float FrameDeltaSeconds => 1.0f / BossActionTimelineConstants.FramesPerSecond;

        public bool IsEvery(int intervalFrames)
        {
            return intervalFrames > 0 && ElapsedFrames % intervalFrames == 0;
        }

        public void MoveBossTo(Vector3 position)
        {
            Boss.SetPosition(position);
        }

        public void MoveBossBy(Vector3 delta)
        {
            Boss.SetPosition(Boss.Position + delta);
        }

        public void SetMoveVelocityPerSecond(Vector3 velocityPerSecond)
        {
            _moveVelocityPerSecond = velocityPerSecond;
        }

        public void FireBullet(Vector3 direction, BossActionBulletConfig bullet)
        {
            if (direction.LengthSquared() <= 0f)
                throw new ArgumentOutOfRangeException(nameof(direction), "direction must be non-zero.");

            _spawnRequests ??= new List<EnemyBulletSpawnRequest>();
            _spawnRequests.Add(bullet.CreateSpawnRequest(Boss.Position, direction));
        }

        public void FireBulletAtPlayer(BossActionBulletConfig bullet)
        {
            Vector3 spawnPosition = Boss.Position + bullet.SpawnOffset;
            Vector3 direction = Player != null
                ? Player.Position - spawnPosition
                : new Vector3(0f, -1f, 0f);
            if (direction.LengthSquared() <= 0f)
                direction = new Vector3(0f, -1f, 0f);

            FireBullet(direction, bullet);
        }

        public void PlayAnimation(string animationStateName, int crossFadeFrames = 0)
        {
            if (string.IsNullOrWhiteSpace(animationStateName))
                return;

            AddCommandEvent(
                BossActionCommandType.PlayAnimation,
                animationStateName,
                Math.Max(0, crossFadeFrames),
                string.Empty,
                Vector3.Zero,
                string.Empty,
                Vector3.Zero,
                string.Empty,
                1.0f);
        }

        public void SpawnEnemy(string enemyDefinitionId, Vector3 spawnOffset)
        {
            if (string.IsNullOrWhiteSpace(enemyDefinitionId))
                return;

            AddCommandEvent(
                BossActionCommandType.SpawnEnemy,
                string.Empty,
                0,
                enemyDefinitionId,
                spawnOffset,
                string.Empty,
                Vector3.Zero,
                string.Empty,
                1.0f);
        }

        public void SpawnEnemy(string enemyDefinitionId)
        {
            SpawnEnemy(enemyDefinitionId, Vector3.Zero);
        }

        public void PlayEffect(string effectId, Vector3 effectLocalOffset)
        {
            if (string.IsNullOrWhiteSpace(effectId))
                return;

            AddCommandEvent(
                BossActionCommandType.PlayEffect,
                string.Empty,
                0,
                string.Empty,
                Vector3.Zero,
                effectId,
                effectLocalOffset,
                string.Empty,
                1.0f);
        }

        public void PlayEffect(string effectId)
        {
            PlayEffect(effectId, Vector3.Zero);
        }

        public void PlaySound(string soundId, float volumeScale = 1.0f)
        {
            if (string.IsNullOrWhiteSpace(soundId))
                return;

            AddCommandEvent(
                BossActionCommandType.PlaySound,
                string.Empty,
                0,
                string.Empty,
                Vector3.Zero,
                string.Empty,
                Vector3.Zero,
                soundId,
                Math.Max(0f, volumeScale));
        }

        public void EmitSignal(string signalId)
        {
            if (string.IsNullOrWhiteSpace(signalId))
                return;

            _emittedSignals ??= new List<string>();
            _emittedSignals.Add(signalId);
        }

        public void SetHitbox(string id, Vector3 offset, float radius, int damage)
        {
            if (radius <= 0f)
                throw new ArgumentOutOfRangeException(nameof(radius), "radius must be positive.");
            if (damage <= 0)
                throw new ArgumentOutOfRangeException(nameof(damage), "damage must be positive.");

            _activeHitboxes ??= new List<BossActiveHitbox>();
            _activeHitboxes.Add(new BossActiveHitbox(id, offset, radius, damage));
        }

        public void SetHitbox(float radius, int damage)
        {
            SetHitbox("body", Vector3.Zero, radius, damage);
        }

        public void SetHurtbox(string id, Vector3 offset, float radius)
        {
            if (radius <= 0f)
                throw new ArgumentOutOfRangeException(nameof(radius), "radius must be positive.");

            _usesExplicitHurtboxWindows = true;
            _activeHurtboxes ??= new List<BossActiveHurtbox>();
            _activeHurtboxes.Add(new BossActiveHurtbox(id, offset, radius));
        }

        public void SetHurtbox(float radius)
        {
            SetHurtbox("body", Vector3.Zero, radius);
        }

        public void SetInvincible()
        {
            _isInvincible = true;
        }

        public void AddCancelTag(string cancelTag)
        {
            if (string.IsNullOrWhiteSpace(cancelTag))
                return;

            _activeCancelTags ??= new List<string>();
            _activeCancelTags.Add(cancelTag);
        }

        public BossActionExecutionResult ToExecutionResult()
        {
            return new BossActionExecutionResult(
                _spawnRequests ?? EmptySpawnRequests,
                _emittedSignals ?? EmptySignals,
                _commandEvents ?? EmptyCommandEvents,
                new BossActionFrameState(
                    _activeHitboxes ?? EmptyHitboxes,
                    _activeHurtboxes ?? EmptyHurtboxes,
                    _isInvincible,
                    _moveVelocityPerSecond,
                    _activeCancelTags ?? EmptyCancelTags,
                    _usesExplicitHurtboxWindows));
        }

        private void AddCommandEvent(
            BossActionCommandType commandType,
            string animationStateName,
            int crossFadeFrames,
            string enemyDefinitionId,
            Vector3 spawnOffset,
            string effectId,
            Vector3 effectLocalOffset,
            string soundId,
            float volumeScale)
        {
            _commandEvents ??= new List<BossActionCommandEvent>();
            _commandEvents.Add(
                new BossActionCommandEvent(
                    _actionId,
                    commandType,
                    ElapsedFrames,
                    animationStateName,
                    crossFadeFrames,
                    enemyDefinitionId,
                    spawnOffset,
                    effectId,
                    effectLocalOffset,
                    soundId,
                    volumeScale));
        }
    }
}
