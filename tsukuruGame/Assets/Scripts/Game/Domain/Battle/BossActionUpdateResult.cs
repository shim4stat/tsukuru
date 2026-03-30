using System;
using System.Collections.Generic;

namespace Game.Domain.Battle
{
    public readonly struct BossActionUpdateResult
    {
        private static readonly IReadOnlyList<EnemyBulletSpawnRequest> EmptyRequests = Array.Empty<EnemyBulletSpawnRequest>();

        public static BossActionUpdateResult Empty => new BossActionUpdateResult(EmptyRequests, null);

        public BossActionUpdateResult(
            IReadOnlyList<EnemyBulletSpawnRequest> spawnRequests,
            EnemyBulletClearPolicy? clearPolicy)
        {
            SpawnRequests = spawnRequests ?? throw new ArgumentNullException(nameof(spawnRequests));
            ClearPolicy = clearPolicy;
        }

        public IReadOnlyList<EnemyBulletSpawnRequest> SpawnRequests { get; }

        public EnemyBulletClearPolicy? ClearPolicy { get; }
    }
}
