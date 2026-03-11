using Game.Contracts.Flow;
using UnityEngine;

namespace Game.Presentation.TestScenes.Bootstrap
{
    /// <summary>
    /// シーン遷移を実行せず、呼び出し回数だけを記録するテスト用ローダー。
    /// </summary>
    public sealed class NoOpSceneLoader : ISceneLoader
    {
        public int LoadTitleSceneCallCount { get; private set; }

        public int LoadGameSceneCallCount { get; private set; }

        public string LastCalledMethod { get; private set; } = string.Empty;

        public void LoadTitleScene()
        {
            LoadTitleSceneCallCount++;
            LastCalledMethod = nameof(LoadTitleScene);
            Debug.Log("NoOpSceneLoader.LoadTitleScene called.");
        }

        public void LoadGameScene()
        {
            LoadGameSceneCallCount++;
            LastCalledMethod = nameof(LoadGameScene);
            Debug.Log("NoOpSceneLoader.LoadGameScene called.");
        }

        public void Reset()
        {
            LoadTitleSceneCallCount = 0;
            LoadGameSceneCallCount = 0;
            LastCalledMethod = string.Empty;
        }
    }
}
