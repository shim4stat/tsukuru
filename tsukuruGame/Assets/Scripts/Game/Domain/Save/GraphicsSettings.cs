using System;

namespace Game.Domain.Save
{
    public sealed class GraphicsSettings
    {
        public int Width { get; }

        public int Height { get; }

        public WindowMode WindowMode { get; }

        public GraphicsSettings(int width, int height, WindowMode windowMode)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than zero.");

            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero.");

            Width = width;
            Height = height;
            WindowMode = windowMode;
        }

        public static GraphicsSettings Default()
        {
            return new GraphicsSettings(1920, 1080, WindowMode.Fullscreen);
        }
    }
}
