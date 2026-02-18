using System;
using Game.Contracts.Flow;
using UnityEngine.SceneManagement;

namespace Game.Infrastructure.Flow
{
    public sealed class UnitySceneLoader : ISceneLoader
    {
        private readonly string _titleSceneName;
        private readonly string _gameSceneName;

        public UnitySceneLoader(string titleSceneName = "TitleScene", string gameSceneName = "GameScene")
        {
            if (string.IsNullOrWhiteSpace(titleSceneName))
                throw new ArgumentException("titleSceneName is null or empty.", nameof(titleSceneName));

            if (string.IsNullOrWhiteSpace(gameSceneName))
                throw new ArgumentException("gameSceneName is null or empty.", nameof(gameSceneName));

            _titleSceneName = titleSceneName;
            _gameSceneName = gameSceneName;
        }

        public void LoadTitleScene()
        {
            SceneManager.LoadScene(_titleSceneName);
        }

        public void LoadGameScene()
        {
            SceneManager.LoadScene(_gameSceneName);
        }
    }
}
