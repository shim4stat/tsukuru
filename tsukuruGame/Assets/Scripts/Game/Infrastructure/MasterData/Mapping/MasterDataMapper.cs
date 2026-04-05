using System;
using System.Collections.Generic;
using NumericsVector3 = System.Numerics.Vector3;
using Game.Contracts.MasterData.Models;
using Game.Infrastructure.MasterData.Assets;

namespace Game.Infrastructure.MasterData.Mapping
{
    public static class MasterDataMapper
    {
        public static StageDefinitionContract ToContract(StageDefinitionAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            return new StageDefinitionContract
            {
                Id = asset.Id ?? string.Empty,
                DisplayName = asset.DisplayName ?? string.Empty,
                BossId = asset.BossId ?? string.Empty,
                OrderIndex = asset.OrderIndex,
                HasIntroStory = asset.HasIntroStory,
                IntroStoryId = asset.IntroStoryId ?? string.Empty,
                HasOutroStory = asset.HasOutroStory,
                OutroStoryId = asset.OutroStoryId ?? string.Empty,
            };
        }

        public static PlayerParamsContract ToContract(PlayerParamsAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            return new PlayerParamsContract
            {
                MaxHp = asset.MaxHp,
                MaxEnergy = asset.MaxEnergy,
                MaxSpecialEnergy = asset.MaxSpecialEnergy,
            };
        }

        public static BossParamsContract ToContract(BossParamsAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            List<int> gauges = new List<int>();
            IReadOnlyList<int> source = asset.GaugeMaxHps;
            if (source != null)
            {
                for (int i = 0; i < source.Count; i++)
                    gauges.Add(source[i]);
            }

            List<BossPhasePatternContract> phasePatterns = new List<BossPhasePatternContract>();
            IReadOnlyList<BossPhasePatternAsset> phaseSource = asset.PhasePatterns;
            if (phaseSource != null)
            {
                for (int i = 0; i < phaseSource.Count; i++)
                {
                    BossPhasePatternAsset phase = phaseSource[i];
                    if (phase == null)
                        continue;

                    phasePatterns.Add(ToContract(phase));
                }
            }

            List<BossAttackDefinitionContract> attacks = new List<BossAttackDefinitionContract>();
            IReadOnlyList<BossAttackDefinitionAsset> attackSource = asset.Attacks;
            if (attackSource != null)
            {
                for (int i = 0; i < attackSource.Count; i++)
                {
                    BossAttackDefinitionAsset attack = attackSource[i];
                    if (attack == null)
                        continue;

                    attacks.Add(ToContract(attack));
                }
            }

            List<BossStateDefinitionContract> states = new List<BossStateDefinitionContract>();
            IReadOnlyList<BossStateDefinitionAsset> stateSource = asset.States;
            if (stateSource != null)
            {
                for (int i = 0; i < stateSource.Count; i++)
                {
                    BossStateDefinitionAsset state = stateSource[i];
                    if (state == null)
                        continue;

                    states.Add(ToContract(state));
                }
            }

            string initialStateId = asset.InitialStateId ?? string.Empty;
            if (states.Count == 0 || attacks.Count == 0 || string.IsNullOrWhiteSpace(initialStateId))
                BuildLegacyBossBehavior(gauges, asset.ActionIntervalSeconds, phasePatterns, out initialStateId, out states, out attacks);

            return new BossParamsContract
            {
                Id = asset.Id ?? string.Empty,
                GaugeMaxHps = gauges,
                BaseDropEnergyAmount = asset.BaseDropEnergyAmount,
                MinDropIntervalSeconds = asset.MinDropIntervalSeconds,
                ActionIntervalSeconds = asset.ActionIntervalSeconds,
                InitialStateId = initialStateId,
                States = states,
                Attacks = attacks,
                PhasePatterns = phasePatterns,
            };
        }

        public static AttackSequenceContract ToContract(AttackSequenceAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            return new AttackSequenceContract
            {
                Id = asset.Id ?? string.Empty,
                IsSpecial = asset.IsSpecial,
                EnergyCost = asset.EnergyCost,
                SpecialEnergyCost = asset.SpecialEnergyCost,
                PhaseStartSeconds = asset.PhaseStartSeconds,
                PhaseAttackSeconds = asset.PhaseAttackSeconds,
                PhaseEndSeconds = asset.PhaseEndSeconds,
                RobotBulletId = asset.RobotBulletId ?? string.Empty,
                DropMultiplier = asset.DropMultiplier,
            };
        }

        public static StorySequenceContract ToContract(StorySequenceAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            List<StoryPageContract> pages = new List<StoryPageContract>();
            IReadOnlyList<StoryPageAsset> source = asset.Pages;
            if (source != null)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    StoryPageAsset page = source[i];
                    if (page == null)
                        continue;

                    pages.Add(new StoryPageContract
                    {
                        Text = page.Text ?? string.Empty,
                        SpeakerId = page.SpeakerId ?? string.Empty,
                    });
                }
            }

            return new StorySequenceContract
            {
                Id = asset.Id ?? string.Empty,
                Pages = pages,
            };
        }

        private static BossPhasePatternContract ToContract(BossPhasePatternAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            return new BossPhasePatternContract
            {
                PatternType = asset.PatternType,
                FireIntervalSeconds = asset.FireIntervalSeconds,
                ShotCount = asset.ShotCount,
                SpreadDegrees = asset.SpreadDegrees,
                BurstShotCount = asset.BurstShotCount,
                BurstShotIntervalSeconds = asset.BurstShotIntervalSeconds,
                BulletSpeed = asset.BulletSpeed,
                BulletLifetimeSeconds = asset.BulletLifetimeSeconds,
                BulletDamage = asset.BulletDamage,
                AbsorbableEnergyAmount = asset.AbsorbableEnergyAmount,
                BulletBehaviorType = asset.BulletBehaviorType,
                SpawnOffset = ToNumericsVector3(asset.SpawnOffset),
                FireDirection = ToNumericsVector3(asset.FireDirection),
            };
        }

        private static BossAttackDefinitionContract ToContract(BossAttackDefinitionAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            return new BossAttackDefinitionContract
            {
                Id = asset.Id ?? string.Empty,
                PatternType = asset.PatternType,
                FireIntervalSeconds = asset.FireIntervalSeconds,
                ShotCount = asset.ShotCount,
                SpreadDegrees = asset.SpreadDegrees,
                BurstShotCount = asset.BurstShotCount,
                BurstShotIntervalSeconds = asset.BurstShotIntervalSeconds,
                BulletSpeed = asset.BulletSpeed,
                BulletLifetimeSeconds = asset.BulletLifetimeSeconds,
                BulletDamage = asset.BulletDamage,
                AbsorbableEnergyAmount = asset.AbsorbableEnergyAmount,
                BulletBehaviorType = asset.BulletBehaviorType,
                SpawnOffset = ToNumericsVector3(asset.SpawnOffset),
                FireDirection = ToNumericsVector3(asset.FireDirection),
                ActiveDurationSeconds = asset.ActiveDurationSeconds,
            };
        }

        private static BossAttackPlanContract ToContract(BossAttackPlanAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            List<string> openingSequence = new List<string>();
            IReadOnlyList<string> openingSource = asset.OpeningSequenceAttackIds;
            if (openingSource != null)
            {
                for (int i = 0; i < openingSource.Count; i++)
                    openingSequence.Add(openingSource[i] ?? string.Empty);
            }

            List<string> randomSequence = new List<string>();
            IReadOnlyList<string> randomSource = asset.RandomAttackIds;
            if (randomSource != null)
            {
                for (int i = 0; i < randomSource.Count; i++)
                    randomSequence.Add(randomSource[i] ?? string.Empty);
            }

            return new BossAttackPlanContract
            {
                OpeningSequenceAttackIds = openingSequence,
                RandomAttackIds = randomSequence,
                HistoryWindow = asset.HistoryWindow,
            };
        }

        private static BossStateTransitionContract ToContract(BossStateTransitionAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            return new BossStateTransitionContract
            {
                NextStateId = asset.NextStateId ?? string.Empty,
                ConditionType = asset.ConditionType,
                Threshold = asset.Threshold,
                SignalId = asset.SignalId ?? string.Empty,
            };
        }

        private static BossStateDefinitionContract ToContract(BossStateDefinitionAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            List<BossStateTransitionContract> transitions = new List<BossStateTransitionContract>();
            IReadOnlyList<BossStateTransitionAsset> transitionSource = asset.Transitions;
            if (transitionSource != null)
            {
                for (int i = 0; i < transitionSource.Count; i++)
                {
                    BossStateTransitionAsset transition = transitionSource[i];
                    if (transition == null)
                        continue;

                    transitions.Add(ToContract(transition));
                }
            }

            return new BossStateDefinitionContract
            {
                Id = asset.Id ?? string.Empty,
                StateType = asset.StateType,
                AttackPlan = asset.AttackPlan != null ? ToContract(asset.AttackPlan) : new BossAttackPlanContract(),
                Transitions = transitions,
            };
        }

        private static void BuildLegacyBossBehavior(
            IReadOnlyList<int> gauges,
            float actionIntervalSeconds,
            IReadOnlyList<BossPhasePatternContract> phasePatterns,
            out string initialStateId,
            out List<BossStateDefinitionContract> states,
            out List<BossAttackDefinitionContract> attacks)
        {
            states = new List<BossStateDefinitionContract>();
            attacks = new List<BossAttackDefinitionContract>();

            if (gauges == null || gauges.Count == 0)
            {
                initialStateId = string.Empty;
                return;
            }

            List<BossAttackDefinitionContract> legacyAttacks = BuildLegacyAttacks(gauges.Count, actionIntervalSeconds, phasePatterns);
            attacks.AddRange(legacyAttacks);

            const string introStateId = "legacy_intro";
            const string deadStateId = "legacy_dead";

            states.Add(
                new BossStateDefinitionContract
                {
                    Id = introStateId,
                    StateType = BossStateType.Intro,
                    Transitions = new BossStateTransitionContract[]
                    {
                        new BossStateTransitionContract
                        {
                            ConditionType = BossTransitionConditionType.ExternalSignal,
                            SignalId = BossStateSignalIds.IntroFinished,
                            NextStateId = "legacy_phase_0",
                        },
                    },
                });

            for (int i = 0; i < legacyAttacks.Count; i++)
            {
                string stateId = $"legacy_phase_{i}";
                string attackId = legacyAttacks[i].Id;
                BossStateTransitionContract transition;
                if (i < legacyAttacks.Count - 1)
                {
                    transition = new BossStateTransitionContract
                    {
                        ConditionType = BossTransitionConditionType.CurrentGaugeIndexAtOrAbove,
                        Threshold = i + 1,
                        NextStateId = $"legacy_phase_{i + 1}",
                    };
                }
                else
                {
                    transition = new BossStateTransitionContract
                    {
                        ConditionType = BossTransitionConditionType.CurrentHpRateAtOrBelow,
                        Threshold = 0f,
                        NextStateId = deadStateId,
                    };
                }

                states.Add(
                    new BossStateDefinitionContract
                    {
                        Id = stateId,
                        StateType = BossStateType.Phase,
                        AttackPlan = new BossAttackPlanContract
                        {
                            OpeningSequenceAttackIds = new[] { attackId },
                            RandomAttackIds = new[] { attackId },
                            HistoryWindow = 0,
                        },
                        Transitions = new[] { transition },
                    });
            }

            states.Add(
                new BossStateDefinitionContract
                {
                    Id = deadStateId,
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
                });

            initialStateId = introStateId;
        }

        private static List<BossAttackDefinitionContract> BuildLegacyAttacks(
            int gaugeCount,
            float actionIntervalSeconds,
            IReadOnlyList<BossPhasePatternContract> phasePatterns)
        {
            List<BossAttackDefinitionContract> attacks = new List<BossAttackDefinitionContract>(gaugeCount);
            for (int i = 0; i < gaugeCount; i++)
            {
                BossPhasePatternContract sourcePattern =
                    phasePatterns != null && i < phasePatterns.Count && phasePatterns[i] != null
                        ? phasePatterns[i]
                        : CreateDefaultLegacyPhasePattern(i, gaugeCount, actionIntervalSeconds);

                attacks.Add(
                    new BossAttackDefinitionContract
                    {
                        Id = $"legacy_attack_{i}",
                        PatternType = sourcePattern.PatternType,
                        FireIntervalSeconds = sourcePattern.FireIntervalSeconds,
                        ShotCount = sourcePattern.ShotCount,
                        SpreadDegrees = sourcePattern.SpreadDegrees,
                        BurstShotCount = sourcePattern.BurstShotCount,
                        BurstShotIntervalSeconds = sourcePattern.BurstShotIntervalSeconds,
                        BulletSpeed = sourcePattern.BulletSpeed,
                        BulletLifetimeSeconds = sourcePattern.BulletLifetimeSeconds,
                        BulletDamage = sourcePattern.BulletDamage,
                        AbsorbableEnergyAmount = sourcePattern.AbsorbableEnergyAmount,
                        BulletBehaviorType = sourcePattern.BulletBehaviorType,
                        SpawnOffset = sourcePattern.SpawnOffset,
                        FireDirection = sourcePattern.FireDirection,
                        ActiveDurationSeconds = float.PositiveInfinity,
                    });
            }

            return attacks;
        }

        private static BossPhasePatternContract CreateDefaultLegacyPhasePattern(
            int gaugeIndex,
            int gaugeCount,
            float actionIntervalSeconds)
        {
            BossPhasePatternContract pattern = CreateBaseLegacyPhasePattern(actionIntervalSeconds);
            if (gaugeIndex == 0)
                return pattern;

            if (gaugeIndex == gaugeCount - 1)
            {
                pattern.PatternType = BossAttackPatternType.BurstShot;
                pattern.BurstShotCount = 3;
                pattern.BurstShotIntervalSeconds = 0.15f;
                return pattern;
            }

            pattern.PatternType = BossAttackPatternType.NWayShot;
            pattern.ShotCount = 3;
            pattern.SpreadDegrees = 30.0f;
            return pattern;
        }

        private static BossPhasePatternContract CreateBaseLegacyPhasePattern(float actionIntervalSeconds)
        {
            return new BossPhasePatternContract
            {
                PatternType = BossAttackPatternType.SingleShot,
                FireIntervalSeconds = actionIntervalSeconds > 0f ? actionIntervalSeconds : 1.0f,
                ShotCount = 1,
                SpreadDegrees = 0f,
                BurstShotCount = 3,
                BurstShotIntervalSeconds = 0.15f,
                BulletSpeed = 3.0f,
                BulletLifetimeSeconds = 2.0f,
                BulletDamage = 1,
                AbsorbableEnergyAmount = 1,
                BulletBehaviorType = EnemyBulletBehaviorTypeContract.Straight,
                SpawnOffset = NumericsVector3.Zero,
                FireDirection = new NumericsVector3(0f, -1f, 0f),
            };
        }

        private static NumericsVector3 ToNumericsVector3(UnityEngine.Vector3 source)
        {
            return new NumericsVector3(source.x, source.y, source.z);
        }
    }
}
