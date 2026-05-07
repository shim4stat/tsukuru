using Game.Contracts.MasterData.Models;

namespace Game.Presentation.TestBoss.Data
{
    internal static class TestBossDefinitionProvider
    {
        private const string VerticalSweepActionId = "vertical_sweep";
        private const string LeftOrbitAimedActionId = "left_orbit_aimed";
        private const string PlayerChargeReturnActionId = "player_charge_return";

        public static BossParamsContract CreateStage01Boss()
        {
            return new BossParamsContract
            {
                Id = TestBossConstants.Stage01BossId,
                GaugeMaxHps = new[] { 30 },
                BaseDropEnergyAmount = 0,
                MinDropIntervalSeconds = 0f,
                InitialStateId = "intro",
                Actions = new BossActionDefinitionContract[]
                {
                    CreateProceduralAction(VerticalSweepActionId, BossActionTypeIds.VerticalSweepShot),
                    CreateProceduralAction(LeftOrbitAimedActionId, BossActionTypeIds.LeftOrbitAimedShot),
                    CreateProceduralAction(PlayerChargeReturnActionId, BossActionTypeIds.PlayerChargeAndReturn),
                },
                States = new BossStateDefinitionContract[]
                {
                    new BossStateDefinitionContract
                    {
                        Id = "intro",
                        StateType = BossStateType.Intro,
                        Transitions = new BossStateTransitionContract[]
                        {
                            new BossStateTransitionContract
                            {
                                ConditionType = BossTransitionConditionType.ExternalSignal,
                                SignalId = BossStateSignalIds.IntroFinished,
                                NextStateId = "phase_01",
                            },
                        },
                    },
                    new BossStateDefinitionContract
                    {
                        Id = "phase_01",
                        StateType = BossStateType.Phase,
                        ActionPlan = new BossActionPlanContract
                        {
                            OpeningSequenceActionIds = new[]
                            {
                                VerticalSweepActionId,
                                LeftOrbitAimedActionId,
                                PlayerChargeReturnActionId,
                            },
                            RandomActionIds = new[]
                            {
                                VerticalSweepActionId,
                                LeftOrbitAimedActionId,
                                PlayerChargeReturnActionId,
                            },
                            HistoryWindow = 1,
                        },
                        Transitions = new BossStateTransitionContract[]
                        {
                            new BossStateTransitionContract
                            {
                                ConditionType = BossTransitionConditionType.CurrentHpRateAtOrBelow,
                                Threshold = 0f,
                                NextStateId = "dead",
                            },
                        },
                    },
                    new BossStateDefinitionContract
                    {
                        Id = "dead",
                        StateType = BossStateType.Dead,
                        Transitions = new BossStateTransitionContract[]
                        {
                            new BossStateTransitionContract
                            {
                                ConditionType = BossTransitionConditionType.ElapsedTime,
                                Threshold = 0.5f,
                                NextStateId = string.Empty,
                            },
                        },
                    },
                },
            };
        }

        private static BossActionDefinitionContract CreateProceduralAction(string id, string actionTypeId)
        {
            return new BossActionDefinitionContract
            {
                Id = id,
                ActionTypeId = actionTypeId,
                ConfigKey = string.Empty,
                EndConditionType = BossActionEndConditionType.Manual,
                CancelPolicy = BossActionCancelPolicy.AlwaysCancelable,
            };
        }
    }
}
