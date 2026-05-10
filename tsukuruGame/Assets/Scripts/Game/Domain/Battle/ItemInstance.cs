namespace Game.Domain.Battle
{
    public class ItemInstance
    {
        public int CellX { get; }
        public int CellY { get; }
        public ItemType ItemType { get; }

        public ItemInstance(int cellX, int cellY, ItemType itemType)
        {
            CellX = cellX;
            CellY = cellY;
            ItemType = itemType;
        }
    }
}
