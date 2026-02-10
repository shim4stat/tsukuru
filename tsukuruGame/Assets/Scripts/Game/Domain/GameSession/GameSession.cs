// Auto-generated for Step2: GameSession (minimal) - Domain (UnityEngine dependency-free)

using System;

namespace Game.Domain.GameSession
{
    /// <summary>
    /// Single source of truth for the current game flow state.
    ///
    /// Notes:
    /// - This is Domain-layer runtime state (pure C#).
    /// - Scene loading / UI switching decisions are handled by Application/Presentation.
    /// - When <see cref="GameMode"/> is not <see cref="Game.Domain.GameSession.GameMode.InGame"/>,
    ///   <see cref="InGameMode"/> and <see cref="BattlePhase"/> values are treated as "don't care".
    /// </summary>
    public sealed class GameSession
    {
        public GameMode GameMode { get; private set; }
        public InGameMode InGameMode { get; private set; }
        public BattlePhase BattlePhase { get; private set; }

        /// <summary>
        /// Pause flag. Guard: GameOver中はポーズ不可。
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// Current playing stage id (e.g. "stage_01"). Empty when not in-game.
        /// </summary>
        public string CurrentStageId { get; private set; } = string.Empty;

        public bool IsInGame => GameMode == GameMode.InGame;

        public bool IsGameOver => IsInGame && BattlePhase == BattlePhase.GameOver;

        public bool CanPause => IsInGame && !IsGameOver;

        public GameSession()
        {
            // Default to Title so boot flow can start from a safe state.
            EnterTitle();
        }

        /// <summary>
        /// Enter Title mode.
        /// Clears in-game state and unpauses.
        /// </summary>
        public void EnterTitle()
        {
            GameMode = GameMode.Title;
            ResetInGameState();
        }

        /// <summary>
        /// Enter StageSelect mode (floating UI on title).
        /// </summary>
        public void EnterStageSelect()
        {
            GameMode = GameMode.StageSelect;
            // Safety: never keep paused outside InGame.
            IsPaused = false;
        }

        /// <summary>
        /// Enter Option mode (floating UI on title).
        /// </summary>
        public void EnterOption()
        {
            GameMode = GameMode.Option;
            // Safety: never keep paused outside InGame.
            IsPaused = false;
        }

        /// <summary>
        /// Enter InGame mode for the given stage.
        /// </summary>
        /// <param name="stageId">Stage id string (e.g. "stage_01").</param>
        /// <param name="hasIntroStory">If true, starts with <see cref="InGameMode.StoryBeforeBattle"/>; otherwise <see cref="InGameMode.Battle"/>.</param>
        public void EnterInGame(string stageId, bool hasIntroStory)
        {
            if (string.IsNullOrWhiteSpace(stageId))
                throw new ArgumentException("stageId is null or empty.", nameof(stageId));

            GameMode = GameMode.InGame;
            CurrentStageId = stageId;
            IsPaused = false;

            InGameMode = hasIntroStory ? InGameMode.StoryBeforeBattle : InGameMode.Battle;
            // Battle starts from BattleStart (BattleFlowService can advance it).
            BattlePhase = BattlePhase.BattleStart;
        }

        /// <summary>
        /// Update InGame sub-mode.
        /// </summary>
        public void SetInGameMode(InGameMode next)
        {
            EnsureInGame();
            InGameMode = next;

            // Safety: story screens etc. should not be paused.
            if (next != InGameMode.Battle)
                IsPaused = false;
        }

        /// <summary>
        /// Update battle phase.
        /// </summary>
        public void SetBattlePhase(BattlePhase next)
        {
            EnsureInGame();
            BattlePhase = next;

            // Guard: GameOver中はポーズ不可。
            if (next == BattlePhase.GameOver)
                IsPaused = false;
        }

        /// <summary>
        /// Try to set pause state.
        /// Returns false if pausing is not allowed in the current state.
        /// </summary>
        public bool TrySetPaused(bool paused)
        {
            if (!paused)
            {
                IsPaused = false;
                return true;
            }

            if (!CanPause)
                return false;

            IsPaused = true;
            return true;
        }

        /// <summary>
        /// Convenience: toggle pause. Returns false if the result would be "paused" but pausing is not allowed.
        /// </summary>
        public bool TryTogglePause()
        {
            return TrySetPaused(!IsPaused);
        }

        private void ResetInGameState()
        {
            IsPaused = false;
            CurrentStageId = string.Empty;

            // These values are irrelevant outside InGame, but we reset them to stable defaults.
            InGameMode = InGameMode.Battle;
            BattlePhase = BattlePhase.BattleStart;
        }

        private void EnsureInGame()
        {
            if (!IsInGame)
                throw new InvalidOperationException("This operation is only valid while GameMode == InGame.");
        }
    }
}
