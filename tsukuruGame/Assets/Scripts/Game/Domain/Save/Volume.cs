using System;

namespace Game.Domain.Save
{
    public readonly struct Volume
    {
        public float Value { get; }

        public bool IsEnabled { get; }

        public Volume(float value, bool isEnabled)
        {
            if (value < 0.0f || value > 1.0f)
                throw new ArgumentOutOfRangeException(nameof(value), "Volume value must be in range [0, 1].");

            Value = value;
            IsEnabled = isEnabled;
        }

        public static Volume DefaultBgm()
        {
            return new Volume(1.0f, true);
        }

        public static Volume DefaultSe()
        {
            return new Volume(1.0f, true);
        }
    }
}
