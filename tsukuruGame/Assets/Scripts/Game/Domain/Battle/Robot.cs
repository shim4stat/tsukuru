using System;
using System.Collections.Generic;

namespace Game.Domain.Battle
{
    public class Robot
    {
        public StageMap StageMap { get; }
        public IReadOnlyList<ItemInstance> Items => _items;

        private readonly List<ItemInstance> _items = new List<ItemInstance>();

        public Robot(StageMap stageMap)
        {
            StageMap = stageMap ?? throw new ArgumentNullException(nameof(stageMap));
        }

        /// <summary>
        /// アイテム配置データからグリッド上にアイテムを配置する。
        /// 各配置はグリッド範囲内であることを検証する。
        /// </summary>
        public IReadOnlyList<ItemInstance> PlaceItems(IReadOnlyList<ItemInstance> items)
        {
            _items.Clear();

            if (items == null || items.Count == 0)
                return _items;

            foreach (var item in items)
            {
                if (item.CellX < 0 || item.CellX >= StageMap.Width)
                    throw new ArgumentOutOfRangeException(
                        nameof(item.CellX),
                        $"CellX {item.CellX} is out of range [0, {StageMap.Width}).");

                if (item.CellY < 0 || item.CellY >= StageMap.Height)
                    throw new ArgumentOutOfRangeException(
                        nameof(item.CellY),
                        $"CellY {item.CellY} is out of range [0, {StageMap.Height}).");

                _items.Add(item);
            }

            return _items;
        }
    }
}
