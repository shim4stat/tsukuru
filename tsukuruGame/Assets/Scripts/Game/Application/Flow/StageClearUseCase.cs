using System;
using System.Collections.Generic;
using Game.Contracts.MasterData;
using Game.Contracts.MasterData.Models;
using Game.Contracts.Save;
using Game.Contracts.Save.Models;
using Game.Domain.Save;

namespace Game.Application.Flow
{
    /// <summary>
    /// ステージクリア結果をSaveDataへ反映し、次ステージ解放を行う。
    /// </summary>
    public sealed class StageClearUseCase
    {
        private readonly ISaveRepository _saveRepository;
        private readonly IMasterDataRepository _masterDataRepository;

        public StageClearUseCase(
            ISaveRepository saveRepository,
            IMasterDataRepository masterDataRepository)
        {
            _saveRepository = saveRepository ?? throw new ArgumentNullException(nameof(saveRepository));
            _masterDataRepository = masterDataRepository ?? throw new ArgumentNullException(nameof(masterDataRepository));
        }

        public void SaveStageClear(string stageId, string clearRank)
        {
            if (string.IsNullOrWhiteSpace(stageId))
                throw new ArgumentException("stageId is null or empty.", nameof(stageId));

            StageRank clearedRank = ParseRankOrThrow(clearRank, nameof(clearRank));

            // 対象ステージがマスターに存在することを先に保証する。
            _ = _masterDataRepository.GetStage(stageId);

            SaveDataContract save = _saveRepository.LoadOrCreateDefault() ?? new SaveDataContract();
            if (save.Stages == null)
                save.Stages = new List<StageProgressContract>();

            UpsertClearedStage(save.Stages, stageId, clearedRank);
            UnlockNextStage(save.Stages, stageId);

            _saveRepository.Save(save);
        }

        private void UnlockNextStage(List<StageProgressContract> progresses, string currentStageId)
        {
            IReadOnlyList<StageDefinitionContract> allStages = _masterDataRepository.GetAllStages();
            if (allStages == null || allStages.Count == 0)
                return;

            List<StageDefinitionContract> ordered = new List<StageDefinitionContract>(allStages.Count);
            for (int i = 0; i < allStages.Count; i++)
            {
                StageDefinitionContract stage = allStages[i];
                if (stage == null)
                    continue;

                ordered.Add(stage);
            }

            ordered.Sort(CompareStageOrder);
            int currentIndex = FindStageIndexById(ordered, currentStageId);
            if (currentIndex < 0)
                throw new InvalidOperationException($"Current stage is not found in master stages: {currentStageId}");

            int nextIndex = currentIndex + 1;
            if (nextIndex >= ordered.Count)
                return;

            string nextStageId = ordered[nextIndex].Id;
            StageProgressContract nextProgress = FindOrCreateProgress(progresses, nextStageId);
            nextProgress.Unlocked = true;
        }

        private static int CompareStageOrder(StageDefinitionContract x, StageDefinitionContract y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (x == null)
                return -1;
            if (y == null)
                return 1;

            int byOrder = x.OrderIndex.CompareTo(y.OrderIndex);
            if (byOrder != 0)
                return byOrder;

            return string.CompareOrdinal(x.Id, y.Id);
        }

        private static int FindStageIndexById(IReadOnlyList<StageDefinitionContract> orderedStages, string stageId)
        {
            for (int i = 0; i < orderedStages.Count; i++)
            {
                StageDefinitionContract stage = orderedStages[i];
                if (stage != null && string.Equals(stage.Id, stageId, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }

        private static void UpsertClearedStage(
            List<StageProgressContract> progresses,
            string stageId,
            StageRank clearedRank)
        {
            StageProgressContract progress = FindOrCreateProgress(progresses, stageId);
            progress.Unlocked = true;
            progress.Cleared = true;

            StageRank currentBest = ParseRankOrNone(progress.BestRank);
            if (clearedRank > currentBest)
                progress.BestRank = ToRankString(clearedRank);
        }

        private static StageProgressContract FindOrCreateProgress(List<StageProgressContract> progresses, string stageId)
        {
            for (int i = 0; i < progresses.Count; i++)
            {
                StageProgressContract progress = progresses[i];
                if (progress == null)
                    continue;

                if (string.Equals(progress.StageId, stageId, StringComparison.Ordinal))
                    return progress;
            }

            StageProgressContract created = new StageProgressContract
            {
                StageId = stageId,
                Unlocked = false,
                Cleared = false,
                BestRank = string.Empty,
            };

            progresses.Add(created);
            return created;
        }

        private static StageRank ParseRankOrThrow(string rankText, string paramName)
        {
            if (string.IsNullOrWhiteSpace(rankText))
                throw new ArgumentException("rank is null or empty.", paramName);

            StageRank rank = ParseRankOrNone(rankText);
            if (rank == StageRank.None)
                throw new ArgumentException($"Unsupported clear rank: {rankText}", paramName);

            return rank;
        }

        private static StageRank ParseRankOrNone(string rankText)
        {
            if (string.IsNullOrWhiteSpace(rankText))
                return StageRank.None;

            string normalized = rankText.Trim().ToUpperInvariant();
            if (normalized == "C")
                return StageRank.C;
            if (normalized == "B")
                return StageRank.B;
            if (normalized == "A")
                return StageRank.A;
            if (normalized == "S")
                return StageRank.S;

            return StageRank.None;
        }

        private static string ToRankString(StageRank rank)
        {
            if (rank == StageRank.C)
                return "C";
            if (rank == StageRank.B)
                return "B";
            if (rank == StageRank.A)
                return "A";
            if (rank == StageRank.S)
                return "S";

            return string.Empty;
        }
    }
}
