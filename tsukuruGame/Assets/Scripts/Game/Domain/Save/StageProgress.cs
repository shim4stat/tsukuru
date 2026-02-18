using System;

namespace Game.Domain.Save
{
    public sealed class StageProgress
    {
        public string StageId { get; }

        public bool IsUnlocked { get; private set; }

        public StageRank ClearRank { get; private set; }

        public StageProgress(string stageId, bool isUnlocked, StageRank clearRank)
        {
            if (string.IsNullOrWhiteSpace(stageId))
                throw new ArgumentException("stageId is null or empty.", nameof(stageId));

            StageId = stageId;
            IsUnlocked = isUnlocked;
            ClearRank = clearRank;
        }

        public void Unlock()
        {
            IsUnlocked = true;
        }

        public void MarkCleared(StageRank rank)
        {
            if (rank == StageRank.None)
                throw new ArgumentException("Clear rank cannot be None.", nameof(rank));

            IsUnlocked = true;
            ClearRank = rank;
        }
    }
}
