using System;
using System.Collections.Generic;
using System.Numerics;

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
            if (!CanApplyBossDamage(context, damageAmount))
                return false;

            context.Boss.TakeDamage(damageAmount);
            return true;
        }

        public bool ApplyBossDamage(
            BattleContext context,
            int damageAmount,
            Vector3 hitPosition,
            float hitRadius)
        {
            if (!CanApplyBossDamage(context, damageAmount))
                return false;
            if (!IsWithinActiveBossHurtbox(context, hitPosition, hitRadius))
                return false;

            context.Boss.TakeDamage(damageAmount);
            return true;
        }

        private static bool CanApplyBossDamage(BattleContext context, int damageAmount)
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

            return true;
        }

        private static bool IsWithinActiveBossHurtbox(BattleContext context, Vector3 hitPosition, float hitRadius)
        {
            BossActionFrameState frameState = context.BossActionFrameState;
            if (!frameState.UsesExplicitHurtboxWindows)
                return true;

            IReadOnlyList<BossActiveHurtbox> activeHurtboxes = frameState.ActiveHurtboxes;
            if (activeHurtboxes == null || activeHurtboxes.Count == 0)
                return false;

            float normalizedHitRadius = Math.Max(0f, hitRadius);
            for (int i = 0; i < activeHurtboxes.Count; i++)
            {
                BossActiveHurtbox hurtbox = activeHurtboxes[i];
                if (hurtbox.Radius <= 0f)
                    continue;

                float combinedRadius = hurtbox.Radius + normalizedHitRadius;
                Vector3 hurtboxPosition = context.Boss.Position + hurtbox.Offset;
                if (Vector3.DistanceSquared(hitPosition, hurtboxPosition) <= combinedRadius * combinedRadius)
                    return true;
            }

            return false;
        }
    }
}
