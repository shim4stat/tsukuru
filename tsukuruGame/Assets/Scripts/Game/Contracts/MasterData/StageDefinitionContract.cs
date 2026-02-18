namespace Game.Contracts.MasterData.Models
{
    /// <summary>
    /// Stage metadata for selection and flow decisions.
    /// </summary>
    public sealed class StageDefinitionContract
    {
        public string Id { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public int OrderIndex { get; set; }

        public bool HasIntroStory { get; set; }

        public string IntroStoryId { get; set; } = string.Empty;

        public bool HasOutroStory { get; set; }

        public string OutroStoryId { get; set; } = string.Empty;
    }
}
