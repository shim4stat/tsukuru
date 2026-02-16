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

        public static bool TryGet(out GameServices services)
        {
            services = _services;
            return services != null;
        }

        public static GameServices Require()
        {
            if (_services == null)
                throw new InvalidOperationException("GameServices are not initialized.");

            return _services;
        }
    }
}
