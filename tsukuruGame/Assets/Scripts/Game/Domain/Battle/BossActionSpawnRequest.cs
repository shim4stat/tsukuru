using System;

namespace Game.Domain.Battle
{
    /// <summary>
    /// Minimal spawn request payload produced by BossActionService.
    /// </summary>
    [Obsolete("Use EnemyBulletSpawnRequest instead.", true)]
    public readonly struct BossActionSpawnRequest
    {
        public BossActionSpawnRequest(int phaseIndex, int spawnCount, float intervalSeconds)
        {
            if (phaseIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(phaseIndex), "phaseIndex must be non-negative.");
            if (spawnCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(spawnCount), "spawnCount must be positive.");
            if (intervalSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(intervalSeconds), "intervalSeconds must be positive.");

            PhaseIndex = phaseIndex;
            SpawnCount = spawnCount;
            IntervalSeconds = intervalSeconds;
        }

        public int PhaseIndex { get; }

        public int SpawnCount { get; }

        public float IntervalSeconds { get; }
    }
}
