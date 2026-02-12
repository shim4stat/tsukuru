using System;
using UnityEngine;

namespace Game.Infrastructure.MasterData.Assets
{
    [Serializable]
    public sealed class StoryPageAsset
    {
        [SerializeField] private string text = string.Empty;
        [SerializeField] private string speakerId = string.Empty;

        public string Text => text;
        public string SpeakerId => speakerId;
    }
}
