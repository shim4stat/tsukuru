using System;

namespace Game.Presentation.Title
{
    public sealed class StageSelectItemViewModel
    {
        public string StageId { get; }

        public string DisplayName { get; }

        public bool IsUnlocked { get; }

        public string BestRank { get; }

        public StageSelectItemViewModel(string stageId, string displayName, bool isUnlocked, string bestRank)
        {
            StageId = stageId ?? throw new ArgumentNullException(nameof(stageId));
            DisplayName = displayName ?? string.Empty;
            IsUnlocked = isUnlocked;
            BestRank = bestRank ?? string.Empty;
        }
    }
}
