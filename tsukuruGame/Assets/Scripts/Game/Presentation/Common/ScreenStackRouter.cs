using System;
using System.Collections.Generic;

namespace Game.Presentation.Common
{
    public sealed class ScreenStackRouter
    {
        private sealed class ScreenEntry
        {
            public ScreenEntry(string screenId, int priority, long sequence, Func<bool> isActive, Action closeAction)
            {
                ScreenId = screenId;
                Priority = priority;
                Sequence = sequence;
                IsActive = isActive;
                CloseAction = closeAction;
            }

            public string ScreenId { get; }
            public int Priority { get; }
            public long Sequence { get; }
            public Func<bool> IsActive { get; }
            public Action CloseAction { get; }
        }

        private readonly List<ScreenEntry> _entries = new List<ScreenEntry>();
        private long _nextSequence;

        public void Register(string screenId, int priority, Func<bool> isActive, Action closeAction)
        {
            if (string.IsNullOrWhiteSpace(screenId))
                throw new ArgumentException("screenId is null or empty.", nameof(screenId));
            if (isActive == null)
                throw new ArgumentNullException(nameof(isActive));
            if (closeAction == null)
                throw new ArgumentNullException(nameof(closeAction));

            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_entries[i].ScreenId, screenId, StringComparison.Ordinal))
                    _entries.RemoveAt(i);
            }

            _entries.Add(new ScreenEntry(screenId, priority, _nextSequence++, isActive, closeAction));
        }

        public bool TryHandleBack()
        {
            ScreenEntry top = ResolveTopEntry();
            if (top == null)
                return false;

            top.CloseAction();
            return true;
        }

        public string PeekTopScreenId()
        {
            ScreenEntry top = ResolveTopEntry();
            return top != null ? top.ScreenId : string.Empty;
        }

        public void Clear()
        {
            _entries.Clear();
        }

        private ScreenEntry ResolveTopEntry()
        {
            ScreenEntry best = null;

            for (int i = 0; i < _entries.Count; i++)
            {
                ScreenEntry entry = _entries[i];
                if (!entry.IsActive())
                    continue;

                if (best == null ||
                    entry.Priority > best.Priority ||
                    (entry.Priority == best.Priority && entry.Sequence > best.Sequence))
                {
                    best = entry;
                }
            }

            return best;
        }
    }
}
