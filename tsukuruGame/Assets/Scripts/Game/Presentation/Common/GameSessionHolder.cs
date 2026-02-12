using System;
using Game.Domain.GameSession;
using UnityEngine;

namespace Game.Presentation.Common
{
    public sealed class GameSessionHolder : MonoBehaviour
    {
        public static GameSessionHolder Instance { get; private set; }

        public GameSession Session { get; private set; }

        public bool HasSession => Session != null;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                return;
            }

            if (Instance != this)
                Destroy(gameObject);
        }

        public void Initialize(GameSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (Session != null)
                throw new InvalidOperationException("GameSessionHolder is already initialized.");

            Session = session;
        }
    }
}
