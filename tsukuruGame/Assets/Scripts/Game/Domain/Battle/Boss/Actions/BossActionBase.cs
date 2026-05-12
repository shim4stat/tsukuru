using System;
using Game.Contracts.MasterData.Models;

namespace Game.Domain.Battle
{
    internal abstract class BossActionBase : IBossAction
    {
        protected BossActionBase(string id, int durationFrames, BossActionCancelPolicy cancelPolicy)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Boss action id is null or empty.", nameof(id));
            if (durationFrames <= 0)
                throw new ArgumentOutOfRangeException(nameof(durationFrames), "durationFrames must be positive.");

            Id = id;
            DurationFrames = durationFrames;
            CancelPolicy = cancelPolicy;
        }

        public string Id { get; }

        public bool IsCompleted { get; private set; }

        public BossActionCancelPolicy CancelPolicy { get; }

        protected int DurationFrames { get; }

        protected int ElapsedFrames { get; private set; }

        private bool IsActive { get; set; }

        private BossActionFrameState CurrentFrameState { get; set; } = BossActionFrameState.Empty;

        public BossActionExecutionResult Enter(BattleContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            IsActive = true;
            IsCompleted = false;
            ElapsedFrames = 0;
            CurrentFrameState = BossActionFrameState.Empty;

            BossActionContext action = CreateContext(context);
            OnEnter(action);
            BossActionExecutionResult result = action.ToExecutionResult();
            CurrentFrameState = result.FrameState;
            return result;
        }

        public BossActionExecutionResult Snapshot()
        {
            return new BossActionExecutionResult(null, null, null, CurrentFrameState);
        }

        public BossActionStepResult Update(BattleContext context, int availableFrames)
        {
            if (!IsActive || IsCompleted)
                return new BossActionStepResult(Snapshot(), 0, IsCompleted);
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (availableFrames <= 0)
                return new BossActionStepResult(Snapshot(), 0, false);

            int frameBudget = Math.Min(availableFrames, DurationFrames - ElapsedFrames);
            if (frameBudget <= 0)
            {
                IsCompleted = true;
                return new BossActionStepResult(Snapshot(), 0, true);
            }

            BossActionExecutionResult accumulated = BossActionExecutionResult.Empty;
            bool hasAccumulated = false;
            int consumedFrames = 0;
            for (int i = 0; i < frameBudget && !IsCompleted; i++)
            {
                BossActionContext action = CreateContext(context);
                OnFrame(action);
                BossActionExecutionResult frameResult = action.ToExecutionResult();
                accumulated = hasAccumulated ? accumulated.Merge(frameResult) : frameResult;
                hasAccumulated = true;
                CurrentFrameState = frameResult.FrameState;

                ElapsedFrames++;
                consumedFrames++;
                if (ElapsedFrames >= DurationFrames)
                    IsCompleted = true;
            }

            BossActionExecutionResult result = hasAccumulated ? accumulated : Snapshot();
            return new BossActionStepResult(result, consumedFrames, IsCompleted);
        }

        public void Exit()
        {
            if (!IsActive)
                return;

            OnExit();
            IsActive = false;
            CurrentFrameState = BossActionFrameState.Empty;
        }

        protected void Complete()
        {
            IsCompleted = true;
        }

        protected virtual void OnEnter(BossActionContext action)
        {
        }

        protected abstract void OnFrame(BossActionContext action);

        protected virtual void OnExit()
        {
        }

        private BossActionContext CreateContext(BattleContext context)
        {
            return new BossActionContext(Id, context, ElapsedFrames, DurationFrames);
        }
    }
}
