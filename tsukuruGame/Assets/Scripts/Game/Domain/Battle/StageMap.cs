using System;
using Game.Contracts.Battle;

namespace Game.Domain.Battle
{
    public class StageMap
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        // 縦の壁: 横(Width-1) x 縦(Height)
        // 配列のインデックスは [x, y]
        public bool[,] VerticalWalls { get; private set; }

        // 横の壁: 横(Width) x 縦(Height-1)
        public bool[,] HorizontalWalls { get; private set; }

        public StageMap(int width, int height, bool[,] verticalWalls, bool[,] horizontalWalls)
        {
            Width = width;
            Height = height;
            VerticalWalls = verticalWalls;
            HorizontalWalls = horizontalWalls;
        }

        public static StageMap CreateFromDto(StageMapDto dto)
        {
            if (dto == null) return null;
            return new StageMap(dto.Width, dto.Height, dto.VerticalWalls, dto.HorizontalWalls);
        }
    }
}
