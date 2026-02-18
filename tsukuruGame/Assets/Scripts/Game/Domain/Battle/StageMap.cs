using System;
using System.IO;
using Newtonsoft.Json;

namespace Game.Domain.Battle
{
    public class StageMap
    {
        public const int Width = 20;
        public const int Height = 20;

        // 縦の壁: 横(Width-1) x 縦(Height)
        // 配列のインデックスは [x, y]
        public static readonly bool[,] VerticalWalls = new bool[Width - 1, Height];

        // 横の壁: 横(Width) x 縦(Height-1)
        public static readonly bool[,] HorizontalWalls = new bool[Width, Height - 1];

        /// <summary>
        /// 指定されたパスのJSONファイルを読み込み、静的配列に壁情報を設定します。
        /// </summary>
        public static StageMap LoadFromFile(string filepath)
        {
            if (!File.Exists(filepath))
            {
                throw new FileNotFoundException($"Maze file not found at: {filepath}");
            }

            try
            {
                string jsonString = File.ReadAllText(filepath);

                // 一時的なデータクラス(DTO)にデシリアライズ
                var mazeData = JsonConvert.DeserializeObject<MazeDto>(jsonString);

                if (mazeData == null) return null;

                // 既存の壁情報をクリア
                Array.Clear(VerticalWalls, 0, VerticalWalls.Length);
                Array.Clear(HorizontalWalls, 0, HorizontalWalls.Length);

                // -------------------------------------------------
                // 縦壁の読み込み (JSON: vertical_walls[y][x])
                // -------------------------------------------------
                // JSONデータは行(y)ごとの配列として来るため、y -> x の順でループ
                if (mazeData.VerticalWalls != null)
                {
                    int jsonHeight = mazeData.VerticalWalls.Length;
                    for (int y = 0; y < jsonHeight && y < Height; y++)
                    {
                        var row = mazeData.VerticalWalls[y];
                        if (row == null) continue;

                        int jsonRowWidth = row.Length;
                        for (int x = 0; x < jsonRowWidth && x < Width - 1; x++)
                        {
                            // 1なら壁(true), 0なら通路(false)
                            // クラス定義の配列が [Width-1, Height] なので [x, y] に代入
                            VerticalWalls[x, y] = (row[x] == 1);
                        }
                    }
                }

                // -------------------------------------------------
                // 横壁の読み込み (JSON: horizontal_walls[y][x])
                // -------------------------------------------------
                if (mazeData.HorizontalWalls != null)
                {
                    int jsonHeight = mazeData.HorizontalWalls.Length;
                    for (int y = 0; y < jsonHeight && y < Height - 1; y++)
                    {
                        var row = mazeData.HorizontalWalls[y];
                        if (row == null) continue;

                        int jsonRowWidth = row.Length;
                        for (int x = 0; x < jsonRowWidth && x < Width; x++)
                        {
                            // クラス定義の配列が [Width, Height-1] なので [x, y] に代入
                            HorizontalWalls[x, y] = (row[x] == 1);
                        }
                    }
                }

                return new StageMap();
            }
            catch (Exception ex)
            {
                // ログ出力など適切なエラー処理を行ってください
                Console.WriteLine($"Failed to load maze: {ex.Message}");
                throw;
            }
        }

        // JSONデシリアライズ用の一時クラス (Data Transfer Object)
        private class MazeDto
        {
            public int Width { get; set; }
            public int Height { get; set; }

            // JSONのプロパティ名 "vertical_walls" をマッピング
            [JsonProperty("vertical_walls")]
            public int[][] VerticalWalls { get; set; }

            // JSONのプロパティ名 "horizontal_walls" をマッピング
            [JsonProperty("horizontal_walls")]
            public int[][] HorizontalWalls { get; set; }
        }
    }
}
