// Step2向けに自動生成: GameSession（最小構成）- Domain（UnityEngine依存なし）

using System;
using Game.Domain.Battle;

namespace Game.Domain.GameSession
{
    /// <summary>
    /// 現在のゲーム進行状態を表す唯一の正しい情報源。
    ///
    /// 注意:
    /// - これはDomain層のランタイム状態（pure C#）です。
    /// - シーン読み込みやUI切り替えの判断はApplication/Presentationで扱います。
    /// - <see cref="GameMode"/> が <see cref="Game.Domain.GameSession.GameMode.InGame"/> 以外のとき、
    ///   <see cref="InGameMode"/> と <see cref="BattlePhase"/> の値は実質的に不問です。
    /// </summary>
    public sealed class GameSession
    {
        public GameMode GameMode { get; private set; }
        public InGameMode InGameMode { get; private set; }
        public BattlePhase BattlePhase { get; private set; }

        /// <summary>
        /// ポーズ状態フラグ。ガード: GameOver中はポーズ不可。
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// 現在プレイ中のステージID（例: "stage_01"）。非InGame時は空文字。
        /// </summary>
        public string CurrentStageId { get; private set; } = string.Empty;

        public bool IsInGame => GameMode == GameMode.InGame;

        public bool IsGameOver => IsInGame && BattlePhase == BattlePhase.GameOver;

        public bool CanPause => IsInGame && !IsGameOver;

        public GameSession()
        {
            // 起動フローが安全な状態から始まるよう、初期値はTitleにする。
            EnterTitle();
        }

        /// <summary>
        /// Titleモードへ遷移する。
        /// InGame状態をクリアし、ポーズを解除する。
        /// </summary>
        public void EnterTitle()
        {
            GameMode = GameMode.Title;
            ResetInGameState();
        }

        /// <summary>
        /// StageSelectモードへ遷移する（タイトル上のフローティングUI）。
        /// </summary>
        public void EnterStageSelect()
        {
            GameMode = GameMode.StageSelect;
            // 安全策: InGame以外でポーズ状態を維持しない。
            IsPaused = false;
        }

        /// <summary>
        /// Optionモードへ遷移する（タイトル上のフローティングUI）。
        /// </summary>
        public void EnterOption()
        {
            GameMode = GameMode.Option;
            // 安全策: InGame以外でポーズ状態を維持しない。
            IsPaused = false;
        }

        /// <summary>
        /// 指定ステージでInGameモードへ遷移する。
        /// </summary>
        /// <param name="stageId">ステージID文字列（例: "stage_01"）。</param>
        /// <param name="hasIntroStory">trueなら <see cref="InGameMode.StoryBeforeBattle"/> から開始し、falseなら <see cref="InGameMode.Battle"/> から開始する。</param>
        public void EnterInGame(string stageId, bool hasIntroStory)
        {
            if (string.IsNullOrWhiteSpace(stageId))
                throw new ArgumentException("stageId is null or empty.", nameof(stageId));

            GameMode = GameMode.InGame;
            CurrentStageId = stageId;
            IsPaused = false;

            InGameMode = hasIntroStory ? InGameMode.StoryBeforeBattle : InGameMode.Battle;
            // BattleはBattleStartから開始する（進行はBattleFlowServiceが担当）。
            BattlePhase = BattlePhase.BattleStart;
        }

        /// <summary>
        /// InGameのサブモードを更新する。
        /// </summary>
        public void SetInGameMode(InGameMode next)
        {
            EnsureInGame();
            InGameMode = next;

            // 安全策: ストーリー画面などではポーズ状態にしない。
            if (next != InGameMode.Battle)
                IsPaused = false;
        }

        /// <summary>
        /// バトルフェーズを更新する。
        /// </summary>
        public void SetBattlePhase(BattlePhase next)
        {
            EnsureInGame();
            BattlePhase = next;

            // ガード: GameOver中はポーズ不可。
            if (next == BattlePhase.GameOver)
                IsPaused = false;
        }

        /// <summary>
        /// ポーズ状態の設定を試みる。
        /// 現在の状態でポーズ不可の場合はfalseを返す。
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
        /// 便宜メソッド: ポーズ状態をトグルする。結果が「ポーズON」になり得るが許可されない場合はfalseを返す。
        /// </summary>
        public bool TryTogglePause()
        {
            return TrySetPaused(!IsPaused);
        }

        private void ResetInGameState()
        {
            IsPaused = false;
            CurrentStageId = string.Empty;

            // これらの値はInGame外では意味を持たないが、安定した既定値に戻しておく。
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
