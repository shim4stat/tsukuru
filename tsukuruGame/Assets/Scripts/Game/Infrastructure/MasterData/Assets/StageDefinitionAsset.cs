using UnityEngine;

namespace Game.Infrastructure.MasterData.Assets
{
    [CreateAssetMenu(menuName = "Game/MasterData/StageDefinition", fileName = "StageDefinition")]
    public sealed class StageDefinitionAsset : ScriptableObject
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private int orderIndex;
        [SerializeField] private bool hasIntroStory;
        [SerializeField] private string introStoryId = string.Empty;
        [SerializeField] private bool hasOutroStory;
        [SerializeField] private string outroStoryId = string.Empty;

        public string Id => id;
        public string DisplayName => displayName;
        public int OrderIndex => orderIndex;
        public bool HasIntroStory => hasIntroStory;
        public string IntroStoryId => introStoryId;
        public bool HasOutroStory => hasOutroStory;
        public string OutroStoryId => outroStoryId;
    }
}
