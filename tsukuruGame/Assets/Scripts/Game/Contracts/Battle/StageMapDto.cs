namespace Game.Contracts.Battle
{
    public class StageMapDto
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public bool[,] VerticalWalls { get; set; }
        public bool[,] HorizontalWalls { get; set; }
    }
}
