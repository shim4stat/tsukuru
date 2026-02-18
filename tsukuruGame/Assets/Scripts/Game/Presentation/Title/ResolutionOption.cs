using System;

namespace Game.Presentation.Title
{
    public readonly struct ResolutionOption
    {
        public int Width { get; }

        public int Height { get; }

        public string Label { get; }

        public ResolutionOption(int width, int height)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "width must be greater than zero.");

            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), "height must be greater than zero.");

            Width = width;
            Height = height;
            Label = $"{width}x{height}";
        }
    }
}
