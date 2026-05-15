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
                WalkSpeed = asset.WalkSpeed,
                DashSpeed = asset.DashSpeed,
                DashDuration = asset.DashDuration,
                DashCooldown = asset.DashCooldown,
                DashDeceleration = asset.DashDeceleration,
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

            List<BossActionDefinitionContract> actions = new List<BossActionDefinitionContract>();
            IReadOnlyList<BossActionDefinitionAsset> actionSource = asset.Actions;
            if (actionSource != null)
            {
                for (int i = 0; i < actionSource.Count; i++)
                {
                    BossActionDefinitionAsset action = actionSource[i];
                    if (action == null)
                        continue;

                    actions.Add(ToContract(action));
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

            return new BossParamsContract
            {
                Id = asset.Id ?? string.Empty,
                GaugeMaxHps = gauges,
                BaseDropEnergyAmount = asset.BaseDropEnergyAmount,
                MinDropIntervalSeconds = asset.MinDropIntervalSeconds,
                InitialStateId = asset.InitialStateId ?? string.Empty,
                States = states,
                Actions = actions,
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

        private static BossBulletPatternDefinitionContract ToContract(BossBulletPatternDefinitionAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            return new BossBulletPatternDefinitionContract
            {
                PatternType = asset.PatternType,
                InitialDelayFrames = asset.InitialDelayFrames,
                FireIntervalFrames = asset.FireIntervalFrames,
                ShotCount = asset.ShotCount,
                SpreadDegrees = asset.SpreadDegrees,
                BurstShotCount = asset.BurstShotCount,
                BurstShotIntervalFrames = asset.BurstShotIntervalFrames,
                BulletSpeed = asset.BulletSpeed,
                BulletLifetimeSeconds = asset.BulletLifetimeSeconds,
                BulletDamage = asset.BulletDamage,
                AbsorbableEnergyAmount = asset.AbsorbableEnergyAmount,
                BulletBehaviorType = asset.BulletBehaviorType,
                SpawnOffset = ToNumericsVector3(asset.SpawnOffset),
                FireDirection = ToNumericsVector3(asset.FireDirection),
            };
        }

        private static BossActionCommandContract ToContract(BossActionCommandAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            return new BossActionCommandContract
            {
                TriggerFrame = asset.TriggerFrame,
                CommandType = asset.CommandType,
                AnimationStateName = asset.AnimationStateName ?? string.Empty,
                BulletPattern = asset.BulletPattern != null ? ToContract(asset.BulletPattern) : null,
                EmitterDurationFrames = asset.EmitterDurationFrames,
                SignalId = asset.SignalId ?? string.Empty,
                CrossFadeFrames = asset.CrossFadeFrames,
                EnemyDefinitionId = asset.EnemyDefinitionId ?? string.Empty,
                SpawnOffset = ToNumericsVector3(asset.SpawnOffset),
                EffectId = asset.EffectId ?? string.Empty,
                EffectLocalOffset = ToNumericsVector3(asset.EffectLocalOffset),
                SoundId = asset.SoundId ?? string.Empty,
                VolumeScale = asset.VolumeScale,
            };
        }

        private static BossMoveWindowPayloadContract ToContract(BossMoveWindowPayloadAsset asset)
        {
            if (asset == null)
                return null;

            return new BossMoveWindowPayloadContract
            {
                VelocityPerSecond = ToNumericsVector3(asset.VelocityPerSecond),
            };
        }

        private static BossHitboxWindowPayloadContract ToContract(BossHitboxWindowPayloadAsset asset)
        {
            if (asset == null)
                return null;

            return new BossHitboxWindowPayloadContract
            {
                Offset = ToNumericsVector3(asset.Offset),
                Radius = asset.Radius,
                Damage = asset.Damage,
            };
        }

        private static BossHurtboxWindowPayloadContract ToContract(BossHurtboxWindowPayloadAsset asset)
        {
            if (asset == null)
                return null;

            return new BossHurtboxWindowPayloadContract
            {
                Offset = ToNumericsVector3(asset.Offset),
                Radius = asset.Radius,
            };
        }

        private static BossCancelWindowPayloadContract ToContract(BossCancelWindowPayloadAsset asset)
        {
            if (asset == null)
                return null;

            return new BossCancelWindowPayloadContract
            {
                CancelTag = asset.CancelTag ?? string.Empty,
            };
        }

        private static BossActionWindowContract ToContract(BossActionWindowAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            return new BossActionWindowContract
            {
                Id = asset.Id ?? string.Empty,
                WindowType = asset.WindowType,
                StartFrameInclusive = asset.StartFrameInclusive,
                EndFrameExclusive = asset.EndFrameExclusive,
                MoveWindow = ToContract(asset.MoveWindow),
                HitboxWindow = ToContract(asset.HitboxWindow),
                HurtboxWindow = ToContract(asset.HurtboxWindow),
                CancelWindow = ToContract(asset.CancelWindow),
            };
        }

        private static BossActionDefinitionContract ToContract(BossActionDefinitionAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            List<BossActionCommandContract> commands = new List<BossActionCommandContract>();
            IReadOnlyList<BossActionCommandAsset> commandSource = asset.Commands;
            if (commandSource != null)
            {
                for (int i = 0; i < commandSource.Count; i++)
                {
                    BossActionCommandAsset command = commandSource[i];
                    if (command == null)
                        continue;

                    commands.Add(ToContract(command));
                }
            }

            List<BossActionWindowContract> windows = new List<BossActionWindowContract>();
            IReadOnlyList<BossActionWindowAsset> windowSource = asset.Windows;
            if (windowSource != null)
            {
                for (int i = 0; i < windowSource.Count; i++)
                {
                    BossActionWindowAsset window = windowSource[i];
                    if (window == null)
                        continue;

                    windows.Add(ToContract(window));
                }
            }

            return new BossActionDefinitionContract
            {
                Id = asset.Id ?? string.Empty,
                ActionTypeId = asset.ActionTypeId ?? string.Empty,
                ConfigKey = asset.ConfigKey ?? string.Empty,
                AnimationStateName = asset.AnimationStateName ?? string.Empty,
                EndConditionType = asset.EndConditionType,
                CancelPolicy = asset.CancelPolicy,
                TotalDurationFrames = asset.TotalDurationFrames,
                Commands = commands,
                Windows = windows,
            };
        }

        private static BossActionPlanContract ToContract(BossActionPlanAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            List<string> openingSequence = new List<string>();
            IReadOnlyList<string> openingSource = asset.OpeningSequenceActionIds;
            if (openingSource != null)
            {
                for (int i = 0; i < openingSource.Count; i++)
                    openingSequence.Add(openingSource[i] ?? string.Empty);
            }

            List<string> randomSequence = new List<string>();
            IReadOnlyList<string> randomSource = asset.RandomActionIds;
            if (randomSource != null)
            {
                for (int i = 0; i < randomSource.Count; i++)
                    randomSequence.Add(randomSource[i] ?? string.Empty);
            }

            return new BossActionPlanContract
            {
                OpeningSequenceActionIds = openingSequence,
                RandomActionIds = randomSequence,
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
                ActionPlan = asset.ActionPlan != null ? ToContract(asset.ActionPlan) : new BossActionPlanContract(),
                Transitions = transitions,
            };
        }

        private static NumericsVector3 ToNumericsVector3(UnityEngine.Vector3 source)
        {
            return new NumericsVector3(source.x, source.y, source.z);
        }
    }
}
