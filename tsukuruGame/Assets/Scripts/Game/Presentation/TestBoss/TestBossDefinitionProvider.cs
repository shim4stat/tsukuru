using System.Numerics;
using Game.Contracts.MasterData.Models;

namespace Game.Presentation.TestBoss
{
    internal static class TestBossDefinitionProvider
    {
        public static BossParamsContract CreateStage01Boss()
        {
            return new BossParamsContract
            {
                Id = TestBossConstants.Stage01BossId,
                GaugeMaxHps = new[] { 30 },
                BaseDropEnergyAmount = 0,
                MinDropIntervalSeconds = 0f,
                ActionIntervalSeconds = 1.0f,
                PhasePatterns = new BossPhasePatternContract[]
                {
                    new BossPhasePatternContract
                    {
                        PatternType = BossAttackPatternType.SingleShot,
                        FireIntervalSeconds = 1.0f,
                        ShotCount = 1,
                        SpreadDegrees = 0f,
                        BurstShotCount = 1,
                        BurstShotIntervalSeconds = 0.15f,
                        BulletSpeed = 4.0f,
                        BulletLifetimeSeconds = 3.0f,
                        BulletDamage = 1,
                        AbsorbableEnergyAmount = 1,
                        BulletBehaviorType = EnemyBulletBehaviorTypeContract.Straight,
                        SpawnOffset = new Vector3(0f, -0.5f, 0f),
                        FireDirection = new Vector3(0f, -1f, 0f),
                    },
                },
            };
        }
    }
}
