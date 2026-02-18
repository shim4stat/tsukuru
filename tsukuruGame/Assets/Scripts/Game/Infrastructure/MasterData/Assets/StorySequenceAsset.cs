using System.Collections.Generic;
using UnityEngine;

namespace Game.Infrastructure.MasterData.Assets
{
    [CreateAssetMenu(menuName = "Game/MasterData/StorySequence", fileName = "StorySequence")]
    public sealed class StorySequenceAsset : ScriptableObject
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private List<StoryPageAsset> pages = new List<StoryPageAsset>();

        public string Id => id;
        public IReadOnlyList<StoryPageAsset> Pages => pages;
    }
}
