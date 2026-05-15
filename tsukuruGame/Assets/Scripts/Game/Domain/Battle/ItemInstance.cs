namespace Game.Domain.Battle
{
    public class ItemInstance
    {
        public int CellX { get; }
        public int CellY { get; }
        public ItemDefinition Definition { get; }
        public int MergedCount { get; }
        public ItemType ItemType => Definition.ItemType;

        public ItemInstance(int cellX, int cellY, ItemType itemType)
            : this(cellX, cellY, new ItemDefinition(itemType), 1)
        {
        }

        public ItemInstance(int cellX, int cellY, ItemDefinition definition, int mergedCount)
        {
            CellX = cellX;
            CellY = cellY;
            Definition = definition;
            MergedCount = mergedCount > 0 ? mergedCount : 1;
        }
    }
}
