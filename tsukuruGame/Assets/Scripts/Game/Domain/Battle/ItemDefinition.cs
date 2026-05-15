namespace Game.Domain.Battle
{
    public sealed class ItemDefinition
    {
        private const int DefaultBaseEnergyAmount = 1;

        public ItemType ItemType { get; }
        public int BaseEnergyAmount { get; }

        public ItemDefinition(ItemType itemType, int baseEnergyAmount = DefaultBaseEnergyAmount)
        {
            ItemType = itemType;
            BaseEnergyAmount = baseEnergyAmount > 0 ? baseEnergyAmount : DefaultBaseEnergyAmount;
        }
    }
}
