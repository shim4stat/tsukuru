using System;
using System.IO;
using Newtonsoft.Json;
using Game.Contracts.Battle;

namespace Game.Infrastructure.Battle
{
    public class StageMapRepository : IStageMapRepository
    {
        public StageMapDto LoadStageMap(string filepath)
        {
            if (!File.Exists(filepath))
            {
                throw new FileNotFoundException($"Maze file not found at: {filepath}");
            }

            try
            {
                string jsonString = File.ReadAllText(filepath);
                var mazeData = JsonConvert.DeserializeObject<MazeJsonDto>(jsonString);

                if (mazeData == null) return null;

                int width = mazeData.Width > 0 ? mazeData.Width : 20;
                int height = mazeData.Height > 0 ? mazeData.Height : 20;

                var dto = new StageMapDto
                {
                    Width = width,
                    Height = height,
                    VerticalWalls = new bool[width - 1, height],
                    HorizontalWalls = new bool[width, height - 1]
                };

                if (mazeData.VerticalWalls != null)
                {
                    int jsonHeight = mazeData.VerticalWalls.Length;
                    for (int y = 0; y < jsonHeight && y < height; y++)
                    {
                        var row = mazeData.VerticalWalls[y];
                        if (row == null) continue;

                        int jsonRowWidth = row.Length;
                        for (int x = 0; x < jsonRowWidth && x < width - 1; x++)
                        {
                            dto.VerticalWalls[x, y] = (row[x] == 1);
                        }
                    }
                }

                if (mazeData.HorizontalWalls != null)
                {
                    int jsonHeight = mazeData.HorizontalWalls.Length;
                    for (int y = 0; y < jsonHeight && y < height - 1; y++)
                    {
                        var row = mazeData.HorizontalWalls[y];
                        if (row == null) continue;

                        int jsonRowWidth = row.Length;
                        for (int x = 0; x < jsonRowWidth && x < width; x++)
                        {
                            dto.HorizontalWalls[x, y] = (row[x] == 1);
                        }
                    }
                }

                return dto;
            }
            catch (Exception ex)
            {
                // ログ出力など適切なエラー処理を行ってください
                Console.WriteLine($"Failed to load maze: {ex.Message}");
                throw;
            }
        }

        private class MazeJsonDto
        {
            public int Width { get; set; }
            public int Height { get; set; }

            [JsonProperty("vertical_walls")]
            public int[][] VerticalWalls { get; set; }

            [JsonProperty("horizontal_walls")]
            public int[][] HorizontalWalls { get; set; }
        }
    }
}
