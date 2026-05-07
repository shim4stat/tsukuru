using System;
using System.Collections.Generic;
using Game.Contracts.MasterData.Models;

namespace Game.Domain.Battle
{
    /// <summary>
    /// 次に実行する行動 ID と、オープニング順を消費したかどうかの選択結果。
    /// </summary>
    internal readonly struct ActionSelectionResult
    {
        public ActionSelectionResult(string actionId, bool consumedOpeningSequence)
        {
            ActionId = actionId ?? string.Empty;
            ConsumedOpeningSequence = consumedOpeningSequence;
        }

        public string ActionId { get; }

        public bool ConsumedOpeningSequence { get; }
    }

    /// <summary>
    /// フェーズ状態の中で行動の切り替えと継続実行を担当する。
    /// 1 update 内で複数 action をまたいで frame budget を消費する。
    /// </summary>
    internal sealed class BossActionController
    {
        private readonly BossActionPlanContract _plan;
        private readonly IReadOnlyDictionary<string, IBossAction> _actionsById;
        private readonly IActionSelector _selector;
        private readonly List<string> _actionHistory = new List<string>();

        private IBossAction _currentAction;
        private int _openingIndex;
        private float _frameAccumulator;

        public BossActionController(
            BossActionPlanContract plan,
            IReadOnlyDictionary<string, IBossAction> actionsById,
            IActionSelector selector)
        {
            _plan = plan ?? throw new ArgumentNullException(nameof(plan));
            _actionsById = actionsById ?? throw new ArgumentNullException(nameof(actionsById));
            _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        }

        public bool LastActionCompletedThisUpdate { get; private set; }

        public BossActionFrameState CurrentFrameState { get; private set; } = BossActionFrameState.Empty;

        public void Enter()
        {
            _currentAction?.Exit();
            _currentAction = null;
            _actionHistory.Clear();
            _openingIndex = 0;
            _frameAccumulator = 0f;
            LastActionCompletedThisUpdate = false;
            CurrentFrameState = BossActionFrameState.Empty;
        }

        public BossActionExecutionResult Update(BattleContext context, float deltaTime)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            LastActionCompletedThisUpdate = false;
            BossActionExecutionResult accumulated = BossActionExecutionResult.Empty;
            bool hasAccumulated = false;
            AccumulateTransientResult(EnsureCurrentAction(context), ref accumulated, ref hasAccumulated);

            if (deltaTime <= 0f)
                return hasAccumulated ? accumulated : SnapshotCurrentAction();

            _frameAccumulator += deltaTime * BossActionTimelineConstants.FramesPerSecond;
            int availableFrames = (int)Math.Floor(_frameAccumulator);
            _frameAccumulator -= availableFrames;
            if (availableFrames <= 0)
                return hasAccumulated ? accumulated : SnapshotCurrentAction();

            while (availableFrames > 0)
            {
                AccumulateTransientResult(EnsureCurrentAction(context), ref accumulated, ref hasAccumulated);
                if (_currentAction == null)
                {
                    CurrentFrameState = BossActionFrameState.Empty;
                    break;
                }

                BossActionStepResult stepResult = _currentAction.Update(context, availableFrames);
                if (stepResult.ConsumedFrames < 0 || stepResult.ConsumedFrames > availableFrames)
                {
                    throw new InvalidOperationException(
                        $"Boss action consumed invalid frame count. actionId={_currentAction.Id}, consumed={stepResult.ConsumedFrames}, available={availableFrames}");
                }

                accumulated = hasAccumulated ? accumulated.Merge(stepResult.ExecutionResult) : stepResult.ExecutionResult;
                hasAccumulated = true;
                CurrentFrameState = stepResult.ExecutionResult.FrameState;
                availableFrames -= stepResult.ConsumedFrames;

                if (!stepResult.Completed)
                    break;

                CompleteCurrentAction();
                if (stepResult.ConsumedFrames == 0)
                    break;
            }

            return hasAccumulated ? accumulated : SnapshotCurrentAction();
        }

        public bool CanInterruptCurrentAction(BossTransitionConditionType conditionType)
        {
            if (_currentAction == null || LastActionCompletedThisUpdate)
                return true;
            if (conditionType == BossTransitionConditionType.CurrentActionCompleted)
                return false;
            if (_currentAction.CancelPolicy == BossActionCancelPolicy.AlwaysCancelable)
                return true;

            return CurrentFrameState.ActiveCancelTags != null && CurrentFrameState.ActiveCancelTags.Count > 0;
        }

        public void Exit()
        {
            _currentAction?.Exit();
            _currentAction = null;
            _frameAccumulator = 0f;
            LastActionCompletedThisUpdate = false;
            CurrentFrameState = BossActionFrameState.Empty;
        }

        private BossActionExecutionResult SnapshotCurrentAction()
        {
            if (_currentAction == null)
            {
                CurrentFrameState = BossActionFrameState.Empty;
                return BossActionExecutionResult.Empty;
            }

            BossActionExecutionResult snapshot = _currentAction.Snapshot();
            CurrentFrameState = snapshot.FrameState;
            return snapshot;
        }

        private BossActionExecutionResult EnsureCurrentAction(BattleContext context)
        {
            if (_currentAction != null)
                return BossActionExecutionResult.Empty;

            ActionSelectionResult selection = _selector.SelectNext(_plan, _actionHistory, _openingIndex);
            if (string.IsNullOrWhiteSpace(selection.ActionId))
                return BossActionExecutionResult.Empty;

            if (!_actionsById.TryGetValue(selection.ActionId, out IBossAction action))
                throw new InvalidOperationException($"Boss action is not configured. actionId={selection.ActionId}");

            _currentAction = action;
            BossActionExecutionResult enterResult = _currentAction.Enter(context);
            CurrentFrameState = enterResult.FrameState;
            if (selection.ConsumedOpeningSequence)
                _openingIndex++;

            return enterResult;
        }

        private static void AccumulateTransientResult(
            BossActionExecutionResult result,
            ref BossActionExecutionResult accumulated,
            ref bool hasAccumulated)
        {
            if (!result.HasTransientOutput)
                return;

            accumulated = hasAccumulated ? accumulated.Merge(result) : result;
            hasAccumulated = true;
        }

        private void CompleteCurrentAction()
        {
            string completedActionId = _currentAction.Id;
            _currentAction.Exit();
            _currentAction = null;
            LastActionCompletedThisUpdate = true;
            RecordHistory(completedActionId);
        }

        private void RecordHistory(string actionId)
        {
            if (string.IsNullOrWhiteSpace(actionId))
                return;

            _actionHistory.Add(actionId);

            int maxHistory = Math.Max(4, _plan.HistoryWindow * 4);
            if (_actionHistory.Count > maxHistory)
                _actionHistory.RemoveRange(0, _actionHistory.Count - maxHistory);
        }
    }

    /// <summary>
    /// オープニングシーケンスを消化した後、直近履歴を避けつつランダムで次行動を選ぶ。
    /// </summary>
    internal sealed class OpeningSequenceThenRandomActionSelector : IActionSelector
    {
        private readonly Random _random;

        public OpeningSequenceThenRandomActionSelector()
            : this(new Random())
        {
        }

        public OpeningSequenceThenRandomActionSelector(Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public ActionSelectionResult SelectNext(
            BossActionPlanContract plan,
            IReadOnlyList<string> actionHistory,
            int openingIndex)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            IReadOnlyList<string> openingSequence = plan.OpeningSequenceActionIds ?? Array.Empty<string>();
            if (openingIndex >= 0 && openingIndex < openingSequence.Count)
                return new ActionSelectionResult(openingSequence[openingIndex] ?? string.Empty, true);

            IReadOnlyList<string> randomCandidates = plan.RandomActionIds;
            if (randomCandidates == null || randomCandidates.Count == 0)
                randomCandidates = openingSequence;

            if (randomCandidates == null || randomCandidates.Count == 0)
                return new ActionSelectionResult(string.Empty, false);

            int baseHistoryWindow = Math.Max(0, plan.HistoryWindow);
            int availableHistory = actionHistory != null ? actionHistory.Count : 0;
            int exclusionWindow = Math.Min(baseHistoryWindow, availableHistory);

            List<string> candidates = new List<string>(randomCandidates.Count);
            for (int activeExclusionWindow = exclusionWindow; activeExclusionWindow >= 0; activeExclusionWindow--)
            {
                candidates.Clear();
                for (int i = 0; i < randomCandidates.Count; i++)
                {
                    string actionId = randomCandidates[i];
                    if (string.IsNullOrWhiteSpace(actionId))
                        continue;

                    if (ShouldExclude(actionId, actionHistory, activeExclusionWindow))
                        continue;

                    candidates.Add(actionId);
                }

                if (candidates.Count > 0)
                    break;
            }

            if (candidates.Count == 0)
                return new ActionSelectionResult(string.Empty, false);

            int selectedIndex = _random.Next(candidates.Count);
            return new ActionSelectionResult(candidates[selectedIndex], false);
        }

        private static bool ShouldExclude(string actionId, IReadOnlyList<string> actionHistory, int activeExclusionWindow)
        {
            if (activeExclusionWindow <= 0 || actionHistory == null || actionHistory.Count == 0)
                return false;

            int start = Math.Max(0, actionHistory.Count - activeExclusionWindow);
            for (int i = start; i < actionHistory.Count; i++)
            {
                if (string.Equals(actionId, actionHistory[i], StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
