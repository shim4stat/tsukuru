using System;
using System.Collections.Generic;
using Game.Contracts.MasterData.Models;

namespace Game.Domain.Battle
{
    internal static class BossActionFactory
    {
        private static readonly BossActionRegistry DefaultRegistry = BossActionRegistry.CreateDefault();

        public static IBossAction Create(BossActionDefinitionContract definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (!IsProceduralAction(definition))
                return new ConfiguredBossAction(definition);

            return DefaultRegistry.Create(definition);
        }

        public static bool IsProceduralAction(BossActionDefinitionContract definition)
        {
            return definition != null && !string.IsNullOrWhiteSpace(definition.ActionTypeId);
        }

        public static bool IsKnownActionType(string actionTypeId)
        {
            return DefaultRegistry.IsKnownActionType(actionTypeId);
        }
    }

    internal sealed class BossActionRegistry
    {
        private readonly Dictionary<string, Func<BossActionDefinitionContract, IBossAction>> _factories =
            new Dictionary<string, Func<BossActionDefinitionContract, IBossAction>>(StringComparer.Ordinal);

        public static BossActionRegistry CreateDefault()
        {
            return new BossActionRegistry()
                .Register(
                    BossActionTypeIds.VerticalSweepShot,
                    definition => new VerticalSweepShotAction(VerticalSweepShotConfig.CreateDefault(definition.Id, definition.ConfigKey)))
                .Register(
                    BossActionTypeIds.LeftOrbitAimedShot,
                    definition => new LeftOrbitAimedShotAction(LeftOrbitAimedShotConfig.CreateDefault(definition.Id, definition.ConfigKey)))
                .Register(
                    BossActionTypeIds.PlayerChargeAndReturn,
                    definition => new PlayerChargeAndReturnAction(PlayerChargeAndReturnConfig.CreateDefault(definition.Id, definition.ConfigKey)));
        }

        public BossActionRegistry Register(string actionTypeId, Func<BossActionDefinitionContract, IBossAction> factory)
        {
            if (string.IsNullOrWhiteSpace(actionTypeId))
                throw new ArgumentException("actionTypeId is null or empty.", nameof(actionTypeId));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _factories[actionTypeId] = factory;
            return this;
        }

        public IBossAction Create(BossActionDefinitionContract definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            string actionTypeId = definition.ActionTypeId ?? string.Empty;
            if (!_factories.TryGetValue(actionTypeId, out Func<BossActionDefinitionContract, IBossAction> factory))
                throw new InvalidOperationException($"Unknown procedural boss action type. actionId={definition.Id}, actionTypeId={actionTypeId}");

            return factory(definition);
        }

        public bool IsKnownActionType(string actionTypeId)
        {
            return !string.IsNullOrWhiteSpace(actionTypeId) && _factories.ContainsKey(actionTypeId);
        }
    }
}
