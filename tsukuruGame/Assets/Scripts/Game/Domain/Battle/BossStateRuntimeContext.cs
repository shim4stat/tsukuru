using System;
using System.Collections.Generic;
using Game.Contracts.MasterData.Models;

namespace Game.Domain.Battle
{
    /// <summary>
    /// ボス状態機械が参照するランタイム共有データ。
    /// 実体のボスと、マスターデータから構築した状態・攻撃定義を束ねる。
    /// </summary>
    internal sealed class BossStateRuntimeContext
    {
        public BossStateRuntimeContext(
            Boss boss,
            IReadOnlyDictionary<string, BossStateDefinitionContract> stateDefinitions,
            IReadOnlyDictionary<string, BossAttackDefinitionContract> attackDefinitions)
        {
            Boss = boss ?? throw new ArgumentNullException(nameof(boss));
            StateDefinitions = stateDefinitions ?? throw new ArgumentNullException(nameof(stateDefinitions));
            AttackDefinitions = attackDefinitions ?? throw new ArgumentNullException(nameof(attackDefinitions));
        }

        public Boss Boss { get; }

        public IReadOnlyDictionary<string, BossStateDefinitionContract> StateDefinitions { get; }

        public IReadOnlyDictionary<string, BossAttackDefinitionContract> AttackDefinitions { get; }
    }
}
