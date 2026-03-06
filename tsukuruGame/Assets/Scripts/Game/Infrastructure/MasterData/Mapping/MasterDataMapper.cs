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

            return new BossParamsContract
            {
                Id = asset.Id ?? string.Empty,
                GaugeMaxHps = gauges,
                BaseDropEnergyAmount = asset.BaseDropEnergyAmount,
                MinDropIntervalSeconds = asset.MinDropIntervalSeconds,
                ActionIntervalSeconds = asset.ActionIntervalSeconds,
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

        private static NumericsVector3 ToNumericsVector3(UnityEngine.Vector3 source)
        {
            return new NumericsVector3(source.x, source.y, source.z);
        }
    }
}
