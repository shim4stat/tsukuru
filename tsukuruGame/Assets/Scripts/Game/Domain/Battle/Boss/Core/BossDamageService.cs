using System;

namespace Game.Domain.Battle
{
    /// <summary>
    /// Bossへのダメージ適用を担当する。
    /// 命中判定や撃破時の進行通知は扱わない。
    /// </summary>
    public sealed class BossDamageService
    {
        public bool ApplyBossDamage(BattleContext context, int damageAmount)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (context.Boss == null)
                throw new InvalidOperationException("BattleContext.Boss is not initialized.");

            if (damageAmount <= 0)
                return false;

            if (context.Phase != BattlePhase.Combat)
                return false;
            if (context.BossActionFrameState.IsInvincible)
                return false;
            if (context.BossActionFrameState.UsesExplicitHurtboxWindows &&
                (context.BossActionFrameState.ActiveHurtboxes == null || context.BossActionFrameState.ActiveHurtboxes.Count == 0))
            {
                return false;
            }

            context.Boss.TakeDamage(damageAmount);
            return true;
        }
    }
}
