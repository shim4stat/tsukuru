namespace Game.Domain.Battle
{
    public sealed class EnergyPickupService
    {
        public bool TryPickup(BattleContext context, ItemInstance item)
        {
            if (context == null || item == null)
                return false;

            if (item.ItemType != ItemType.EnergyPotion)
                return false;

            if (!context.Items.Contains(item))
                return false;

            context.Player.AddEnergy(item.Definition.BaseEnergyAmount * item.MergedCount);
            context.Items.Remove(item);
            return true;
        }
    }
}
