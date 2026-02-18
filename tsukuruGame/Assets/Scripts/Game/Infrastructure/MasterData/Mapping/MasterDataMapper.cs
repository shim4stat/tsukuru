using System;
using System.Collections.Generic;
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

            return new BossParamsContract
            {
                Id = asset.Id ?? string.Empty,
                GaugeMaxHps = gauges,
                BaseDropEnergyAmount = asset.BaseDropEnergyAmount,
                MinDropIntervalSeconds = asset.MinDropIntervalSeconds,
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
    }
}
