using System;
using System.Collections.Generic;

namespace Game.Domain.Battle
{
    public sealed class EnemyBulletService
    {
        private static readonly IReadOnlyList<EnemyBullet> EmptyBullets = Array.Empty<EnemyBullet>();

        public IReadOnlyList<EnemyBullet> Spawn(
            BattleContext context,
            IBattleEntityFactory factory,
            IReadOnlyList<EnemyBulletSpawnRequest> spawnRequests)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            if (spawnRequests == null)
                throw new ArgumentNullException(nameof(spawnRequests));

            if (spawnRequests.Count == 0)
                return EmptyBullets;

            List<EnemyBullet> enemyBullets = GetEnemyBullets(context);
            List<EnemyBullet> spawned = new List<EnemyBullet>(spawnRequests.Count);
            for (int i = 0; i < spawnRequests.Count; i++)
            {
                EnemyBullet bullet = factory.CreateEnemyBullet();
                if (bullet == null)
                    throw new InvalidOperationException("IBattleEntityFactory.CreateEnemyBullet returned null.");

                bullet.Initialize(spawnRequests[i]);
                enemyBullets.Add(bullet);
                spawned.Add(bullet);
            }

            return spawned;
        }

        public IReadOnlyList<EnemyBullet> Update(BattleContext context, float deltaTime)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            List<EnemyBullet> enemyBullets = GetEnemyBullets(context);
            if (enemyBullets.Count == 0 || deltaTime <= 0f)
                return EmptyBullets;

            List<EnemyBullet> vanished = null;
            for (int i = enemyBullets.Count - 1; i >= 0; i--)
            {
                EnemyBullet bullet = enemyBullets[i];
                if (bullet == null)
                    throw new InvalidOperationException($"BattleContext.EnemyBullets contains null at index {i}.");

                ApplyBehavior(bullet, deltaTime);
                bullet.Tick(deltaTime);
                if (!bullet.IsVanished)
                    continue;

                vanished ??= new List<EnemyBullet>();
                vanished.Add(bullet);
                enemyBullets.RemoveAt(i);
            }

            return vanished ?? EmptyBullets;
        }

        public void MarkVanished(BattleContext context, EnemyBullet bullet)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (bullet == null)
                throw new ArgumentNullException(nameof(bullet));

            List<EnemyBullet> enemyBullets = GetEnemyBullets(context);
            if (!enemyBullets.Contains(bullet))
                throw new InvalidOperationException("EnemyBullet is not registered in BattleContext.EnemyBullets.");

            bullet.Vanish();
        }

        private static void ApplyBehavior(EnemyBullet bullet, float deltaTime)
        {
            switch (bullet.BehaviorType)
            {
                case EnemyBulletBehaviorType.Straight:
                case EnemyBulletBehaviorType.Wave:
                case EnemyBulletBehaviorType.Homing:
                default:
                    bullet.Translate(bullet.Velocity * deltaTime);
                    break;
            }
        }

        private static List<EnemyBullet> GetEnemyBullets(BattleContext context)
        {
            if (context.EnemyBullets == null)
                throw new InvalidOperationException("BattleContext.EnemyBullets is not initialized.");

            return context.EnemyBullets;
        }
    }
}
