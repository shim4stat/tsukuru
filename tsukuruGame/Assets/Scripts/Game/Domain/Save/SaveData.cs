using System;
using System.Collections.Generic;

namespace Game.Domain.Save
{
    public sealed class SaveData
    {
        private readonly List<StageProgress> _stageProgresses;

        public const int CurrentVersion = 1;

        public int Version { get; private set; }

        public IReadOnlyList<StageProgress> StageProgresses => _stageProgresses;

        public GameSettings Settings { get; private set; }

        public SaveData(int version, List<StageProgress> stageProgresses, GameSettings settings)
        {
            if (version <= 0)
                throw new ArgumentOutOfRangeException(nameof(version), "Version must be greater than zero.");

            Version = version;
            _stageProgresses = stageProgresses ?? throw new ArgumentNullException(nameof(stageProgresses));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public static SaveData CreateDefault()
        {
            return new SaveData(CurrentVersion, new List<StageProgress>(), GameSettings.Default());
        }

        public StageProgress FindStage(string stageId)
        {
            if (string.IsNullOrWhiteSpace(stageId))
                throw new ArgumentException("stageId is null or empty.", nameof(stageId));

            for (int i = 0; i < _stageProgresses.Count; i++)
            {
                StageProgress progress = _stageProgresses[i];
                if (string.Equals(progress.StageId, stageId, StringComparison.Ordinal))
                    return progress;
            }

            return null;
        }

        public void UpsertStageProgress(StageProgress progress)
        {
            if (progress == null)
                throw new ArgumentNullException(nameof(progress));

            for (int i = 0; i < _stageProgresses.Count; i++)
            {
                if (string.Equals(_stageProgresses[i].StageId, progress.StageId, StringComparison.Ordinal))
                {
                    _stageProgresses[i] = progress;
                    return;
                }
            }

            _stageProgresses.Add(progress);
        }

        public void UpdateSettings(GameSettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }
    }
}
