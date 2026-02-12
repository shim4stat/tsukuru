using System;

namespace Game.Presentation.Common
{
    public static class GameServicesLocator
    {
        private static GameServices _services;

        public static bool HasServices => _services != null;

        public static void Set(GameServices services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public static GameServices Require()
        {
            if (_services == null)
                throw new InvalidOperationException("GameServices are not initialized.");

            return _services;
        }
    }
}
