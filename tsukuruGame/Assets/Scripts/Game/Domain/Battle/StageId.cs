namespace Game.Domain.Battle
{
    public readonly struct StageId
    {
        public int Value { get; }
        public StageId(int value) { Value = value; }
    }
}
