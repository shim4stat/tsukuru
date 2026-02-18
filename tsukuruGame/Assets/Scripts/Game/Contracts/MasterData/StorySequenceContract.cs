using System;
using System.Collections.Generic;

namespace Game.Contracts.MasterData.Models
{
    /// <summary>
    /// Story sequence read model.
    /// </summary>
    public sealed class StorySequenceContract
    {
        public string Id { get; set; } = string.Empty;

        public IReadOnlyList<StoryPageContract> Pages { get; set; } = Array.Empty<StoryPageContract>();
    }

    public sealed class StoryPageContract
    {
        public string Text { get; set; } = string.Empty;

        public string SpeakerId { get; set; } = string.Empty;
    }
}
