using Game.Contracts.MasterData.Models;

namespace Game.Presentation.TestBoss
{
    internal static class TestBossParamsResolver
    {
        public static bool TryResolve(string stageId, out BossParamsContract bossParams)
        {
            if (!TestBossSelector.ShouldUseForStage(stageId))
            {
                bossParams = null;
                return false;
            }

            bossParams = TestBossDefinitionProvider.CreateStage01Boss();
            return true;
        }
    }
}
