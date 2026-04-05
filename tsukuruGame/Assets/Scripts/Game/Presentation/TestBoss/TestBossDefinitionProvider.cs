using System.Numerics;
using Game.Contracts.MasterData.Models;

namespace Game.Presentation.TestBoss
{
    internal static class TestBossDefinitionProvider
    {
        public static BossParamsContract CreateStage01Boss()
        {
            return new BossParamsContract
            {
                Id = TestBossConstants.Stage01BossId,
                GaugeMaxHps = new[] { 30 },
                BaseDropEnergyAmount = 0,
                MinDropIntervalSeconds = 0f,
                ActionIntervalSeconds = 1.0f,
                InitialStateId = "intro",
                Attacks = new BossAttackDefinitionContract[]
                {
                    new BossAttackDefinitionContract
                    {
                        Id = "phase_01_single",
                        PatternType = BossAttackPatternType.SingleShot,
                        FireIntervalSeconds = 1.0f,
                        ShotCount = 1,
                        SpreadDegrees = 0f,
                        BurstShotCount = 1,
                        BurstShotIntervalSeconds = 0.15f,
                        BulletSpeed = 4.0f,
                        BulletLifetimeSeconds = 3.0f,
                        BulletDamage = 1,
                        AbsorbableEnergyAmount = 1,
                        BulletBehaviorType = EnemyBulletBehaviorTypeContract.Straight,
                        SpawnOffset = new Vector3(0f, -0.5f, 0f),
                        FireDirection = new Vector3(0f, -1f, 0f),
                        ActiveDurationSeconds = float.PositiveInfinity,
                    },
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
                        AttackPlan = new BossAttackPlanContract
                        {
                            OpeningSequenceAttackIds = new[] { "phase_01_single" },
                            RandomAttackIds = new[] { "phase_01_single" },
                            HistoryWindow = 0,
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
    }
}
