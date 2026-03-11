using System;
using SessionState = Game.Domain.GameSession.GameSession;

namespace Game.Domain.Battle
{
    /// <summary>
    /// Bossへのダメージ適用と撃破時のバトル進行通知を担当する。
    /// 命中判定そのものは扱わない。
    /// </summary>
    public sealed class BossDamageService
    {
        public bool ApplyBossDamage(
            BattleContext context,
            SessionState session,
            BattleFlowService battleFlowService,
            int damageAmount)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            if (battleFlowService == null)
                throw new ArgumentNullException(nameof(battleFlowService));
            if (context.Boss == null)
                throw new InvalidOperationException("BattleContext.Boss is not initialized.");

            if (damageAmount <= 0)
                return false;

            if (context.Phase != BattlePhase.Combat)
                return false;

            context.Boss.TakeDamage(damageAmount);

            if (context.Phase == BattlePhase.Combat && context.Boss.IsAllGaugesEmpty())
                battleFlowService.OnCombatBossHpZero(context, session);

            return true;
        }
    }
}
