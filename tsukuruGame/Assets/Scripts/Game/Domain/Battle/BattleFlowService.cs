using System;
using SessionState = Game.Domain.GameSession.GameSession;

namespace Game.Domain.Battle
{
    /// <summary>
    /// バトルの最小フェーズ遷移を管理するドメインサービス。
    /// </summary>
    public sealed class BattleFlowService
    {
        public void StartBattle(BattleContext context, SessionState session)
        {
            ValidateArguments(context, session);

            if (!session.IsInGame)
                throw new InvalidOperationException("Battle can only be started while GameSession is InGame.");

            EnsurePhase(context, BattlePhase.BattleStart, nameof(StartBattle));

            // 最小実装ではBattleStartから直接BossBootへ進める。
            SyncPhase(context, session, BattlePhase.BossBoot);
        }

        public void Update(BattleContext context, SessionState session, float deltaTime)
        {
            ValidateArguments(context, session);

            // 将来の演出タイマー接続用。最小実装ではイベント駆動のみ。
            _ = deltaTime;
        }

        public void OnBossBootFinished(BattleContext context, SessionState session)
        {
            ValidateArguments(context, session);
            EnsurePhase(context, BattlePhase.BossBoot, nameof(OnBossBootFinished));
            SyncPhase(context, session, BattlePhase.Combat);
        }

        public void OnCombatBossHpZero(BattleContext context, SessionState session)
        {
            ValidateArguments(context, session);
            EnsurePhase(context, BattlePhase.Combat, nameof(OnCombatBossHpZero));
            SyncPhase(context, session, BattlePhase.BossDefeated);
        }

        public void OnBossDefeatedSequenceFinished(BattleContext context, SessionState session)
        {
            ValidateArguments(context, session);
            EnsurePhase(context, BattlePhase.BossDefeated, nameof(OnBossDefeatedSequenceFinished));
            SyncPhase(context, session, BattlePhase.BattleEnd);
        }

        private static void SyncPhase(BattleContext context, SessionState session, BattlePhase next)
        {
            if (!session.IsInGame)
                throw new InvalidOperationException("Battle phase can only be updated while GameSession is InGame.");

            context.SetPhase(next);
            session.SetBattlePhase(next);
        }

        private static void EnsurePhase(BattleContext context, BattlePhase expected, string operationName)
        {
            if (context.Phase != expected)
            {
                throw new InvalidOperationException(
                    $"{operationName} requires phase {expected}, but current phase is {context.Phase}.");
            }
        }

        private static void ValidateArguments(BattleContext context, SessionState session)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (session == null)
                throw new ArgumentNullException(nameof(session));
        }
    }
}
