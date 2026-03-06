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
            EnemyBulletSpawnParams spawnParams)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            ValidateSpawnParams(spawnParams);
            List<EnemyBullet> enemyBullets = GetEnemyBullets(context);
            List<EnemyBullet> spawned = new List<EnemyBullet>(spawnParams.Count);
            for (int i = 0; i < spawnParams.Count; i++)
            {
                EnemyBullet bullet = factory.CreateEnemyBullet();
                if (bullet == null)
                    throw new InvalidOperationException("IBattleEntityFactory.CreateEnemyBullet returned null.");

                bullet.Initialize(
                    spawnParams.Damage,
                    spawnParams.LifetimeSeconds,
                    spawnParams.AbsorbableEnergyAmount);

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

        private static List<EnemyBullet> GetEnemyBullets(BattleContext context)
        {
            if (context.EnemyBullets == null)
                throw new InvalidOperationException("BattleContext.EnemyBullets is not initialized.");

            return context.EnemyBullets;
        }

        private static void ValidateSpawnParams(EnemyBulletSpawnParams spawnParams)
        {
            if (spawnParams.Count <= 0)
                throw new ArgumentOutOfRangeException(nameof(spawnParams), "spawnParams.Count must be positive.");
            if (spawnParams.Damage < 0)
                throw new ArgumentOutOfRangeException(nameof(spawnParams), "spawnParams.Damage must be non-negative.");
            if (spawnParams.LifetimeSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(spawnParams),
                    "spawnParams.LifetimeSeconds must be positive.");
            }

            if (spawnParams.AbsorbableEnergyAmount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(spawnParams),
                    "spawnParams.AbsorbableEnergyAmount must be non-negative.");
            }
        }
    }
}
