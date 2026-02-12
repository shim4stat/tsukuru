using System;
using System.Collections.Generic;
using Game.Infrastructure.MasterData.Assets;

namespace Game.Infrastructure.MasterData.Validation
{
    public static class MasterDataValidator
    {
        public static void ValidateNotNull<T>(T instance, string name) where T : class
        {
            if (instance == null)
                throw new InvalidOperationException($"MasterData is null: {name}");
        }

        public static void ValidateList<T>(IReadOnlyList<T> list, string name) where T : class
        {
            if (list == null)
                throw new InvalidOperationException($"MasterData list is null: {name}");

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null)
                    throw new InvalidOperationException($"MasterData list contains null element: {name}[{i}]");
            }
        }

        public static void ValidateUniqueIds(IReadOnlyList<StageDefinitionAsset> assets, string name)
        {
            ValidateList(assets, name);
            ValidateUniqueIdsInternal(assets, name, a => a.Id);
        }

        public static void ValidateUniqueIds(IReadOnlyList<BossParamsAsset> assets, string name)
        {
            ValidateList(assets, name);
            ValidateUniqueIdsInternal(assets, name, a => a.Id);
        }

        public static void ValidateUniqueIds(IReadOnlyList<AttackSequenceAsset> assets, string name)
        {
            ValidateList(assets, name);
            ValidateUniqueIdsInternal(assets, name, a => a.Id);
        }

        public static void ValidateUniqueIds(IReadOnlyList<StorySequenceAsset> assets, string name)
        {
            ValidateList(assets, name);
            ValidateUniqueIdsInternal(assets, name, a => a.Id);
        }

        private static void ValidateUniqueIdsInternal<T>(IReadOnlyList<T> assets, string name, Func<T, string> idSelector)
            where T : class
        {
            HashSet<string> idSet = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < assets.Count; i++)
            {
                string id = idSelector(assets[i]);
                if (string.IsNullOrWhiteSpace(id))
                    throw new InvalidOperationException($"MasterData id is null or empty: {name}[{i}]");

                if (!idSet.Add(id))
                    throw new InvalidOperationException($"MasterData duplicated id: {name}, id={id}");
            }
        }
    }
}
