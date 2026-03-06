using System.Collections.Generic;

namespace Game.Domain.Battle
{
    public interface IBossAttackPattern
    {
        void Reset();

        IReadOnlyList<EnemyBulletSpawnRequest> Update(BattleContext context, float deltaTime);
    }
}
