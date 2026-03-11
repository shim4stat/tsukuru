using System;
using System.Collections.Generic;
using Game.Contracts.MasterData.Models;

namespace Game.Domain.Battle
{
    /// <summary>
    /// Produces boss action spawn requests on a timer while in Combat.
    /// </summary>
    public sealed class BossActionService
    {
        private static readonly IReadOnlyList<BossActionSpawnRequest> EmptyRequests = Array.Empty<BossActionSpawnRequest>();

        private Boss _boss;
        private float _actionIntervalSeconds;
        private float _elapsedSeconds;
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

            _boss = boss;
            _actionIntervalSeconds = bossParams.ActionIntervalSeconds;
            _elapsedSeconds = 0f;
            _isInitialized = true;
        }

        public IReadOnlyList<BossActionSpawnRequest> Update(BattleContext context, float deltaTime)
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

            _elapsedSeconds += deltaTime;
            if (_elapsedSeconds < _actionIntervalSeconds)
                return EmptyRequests;

            int fireCount = (int)Math.Floor(_elapsedSeconds / _actionIntervalSeconds);
            _elapsedSeconds -= fireCount * _actionIntervalSeconds;

            int phaseIndex = context.Boss.GetCurrentGaugeIndex();
            List<BossActionSpawnRequest> requests = new List<BossActionSpawnRequest>(fireCount);
            for (int i = 0; i < fireCount; i++)
            {
                requests.Add(
                    new BossActionSpawnRequest(
                        phaseIndex: phaseIndex,
                        spawnCount: 1,
                        intervalSeconds: _actionIntervalSeconds));
            }

            return requests;
        }

        public void Reset()
        {
            _elapsedSeconds = 0f;
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("BossActionService is not initialized.");
        }
    }
}
