using System;
using Game.Application.Flow;

namespace Game.Presentation.TestBoss.Data
{
    internal static class TestBossSelector
    {
        public static bool ShouldUseForStage(string stageId)
        {
            return string.Equals(stageId, DefaultStageIds.Stage01, StringComparison.Ordinal);
        }
    }
}
