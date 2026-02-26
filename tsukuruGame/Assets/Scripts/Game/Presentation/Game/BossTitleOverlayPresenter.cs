using System;
using UnityEngine;

namespace Game.Presentation.Game
{
    /// <summary>
    /// BossTitleOverlayの時間制御（フェードイン/保持/フェードアウト）を担当する。
    /// </summary>
    public sealed class BossTitleOverlayPresenter : IDisposable
    {
        private enum PlaybackPhase
        {
            Idle = 0,
            FadeIn = 1,
            Hold = 2,
            FadeOut = 3,
            Completed = 4,
        }

        private readonly BossTitleOverlayView _view;
        private PlaybackPhase _phase = PlaybackPhase.Idle;
        private float _phaseElapsed;
        private bool _finishedRaised;
        private bool _disposed;

        public event Action Finished;

        public bool IsPlaying =>
            _phase == PlaybackPhase.FadeIn ||
            _phase == PlaybackPhase.Hold ||
            _phase == PlaybackPhase.FadeOut;

        public BossTitleOverlayPresenter(BossTitleOverlayView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public void Open(string titleText)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BossTitleOverlayPresenter));

            if (IsPlaying)
                return;

            _view.SetTitle(titleText);
            _view.Show();
            _view.SetOpacity(0f);

            _phaseElapsed = 0f;
            _finishedRaised = false;
            _phase = PlaybackPhase.FadeIn;

            // 0秒設定に対応して即時に次フェーズへ進める。
            Update(0f);
        }

        public void Update(float deltaTime)
        {
            if (_disposed)
                return;

            if (_phase == PlaybackPhase.Idle || _phase == PlaybackPhase.Completed)
                return;

            float remaining = Mathf.Max(0f, deltaTime);
            bool shouldContinue = true;

            while (shouldContinue)
            {
                shouldContinue = false;

                switch (_phase)
                {
                    case PlaybackPhase.FadeIn:
                        remaining = StepFadeIn(remaining, ref shouldContinue);
                        break;
                    case PlaybackPhase.Hold:
                        remaining = StepHold(remaining, ref shouldContinue);
                        break;
                    case PlaybackPhase.FadeOut:
                        remaining = StepFadeOut(remaining, ref shouldContinue);
                        break;
                    case PlaybackPhase.Idle:
                    case PlaybackPhase.Completed:
                    default:
                        return;
                }

                if (remaining <= 0f && !shouldContinue)
                    return;
            }
        }

        public void ForceHide()
        {
            if (_disposed)
                return;

            _view.Hide();
            _phase = PlaybackPhase.Idle;
            _phaseElapsed = 0f;
            _finishedRaised = false;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Finished = null;
        }

        private float StepFadeIn(float remaining, ref bool shouldContinue)
        {
            float duration = _view.FadeInDurationSeconds;
            if (duration <= 0f)
            {
                _view.SetOpacity(1f);
                _phase = PlaybackPhase.Hold;
                _phaseElapsed = 0f;
                shouldContinue = true;
                return remaining;
            }

            _phaseElapsed += remaining;
            float consumed = Mathf.Min(_phaseElapsed, duration);
            _view.SetOpacity(consumed / duration);

            if (_phaseElapsed < duration)
                return 0f;

            float overflow = _phaseElapsed - duration;
            _view.SetOpacity(1f);
            _phase = PlaybackPhase.Hold;
            _phaseElapsed = 0f;
            shouldContinue = true;
            return overflow;
        }

        private float StepHold(float remaining, ref bool shouldContinue)
        {
            float duration = _view.HoldDurationSeconds;
            if (duration <= 0f)
            {
                _view.SetOpacity(1f);
                _phase = PlaybackPhase.FadeOut;
                _phaseElapsed = 0f;
                shouldContinue = true;
                return remaining;
            }

            _view.SetOpacity(1f);
            _phaseElapsed += remaining;
            if (_phaseElapsed < duration)
                return 0f;

            float overflow = _phaseElapsed - duration;
            _phase = PlaybackPhase.FadeOut;
            _phaseElapsed = 0f;
            shouldContinue = true;
            return overflow;
        }

        private float StepFadeOut(float remaining, ref bool shouldContinue)
        {
            float duration = _view.FadeOutDurationSeconds;
            if (duration <= 0f)
            {
                CompletePlayback();
                return remaining;
            }

            _phaseElapsed += remaining;
            float consumed = Mathf.Min(_phaseElapsed, duration);
            _view.SetOpacity(1f - (consumed / duration));

            if (_phaseElapsed < duration)
                return 0f;

            float overflow = _phaseElapsed - duration;
            CompletePlayback();
            shouldContinue = false;
            return overflow;
        }

        private void CompletePlayback()
        {
            _view.Hide();
            _phase = PlaybackPhase.Completed;
            _phaseElapsed = 0f;

            if (_finishedRaised)
                return;

            _finishedRaised = true;
            Finished?.Invoke();
        }
    }
}
