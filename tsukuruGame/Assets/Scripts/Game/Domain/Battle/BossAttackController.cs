using System;
using System.Collections.Generic;
using Game.Contracts.MasterData.Models;

namespace Game.Domain.Battle
{
    /// <summary>
    /// 次に実行する攻撃 ID と、オープニング順を消費したかどうかの選択結果。
    /// </summary>
    internal readonly struct AttackSelectionResult
    {
        public AttackSelectionResult(string attackId, bool consumedOpeningSequence)
        {
            AttackId = attackId ?? string.Empty;
            ConsumedOpeningSequence = consumedOpeningSequence;
        }

        public string AttackId { get; }

        public bool ConsumedOpeningSequence { get; }
    }

    /// <summary>
    /// フェーズ状態の中で攻撃の切り替えと継続実行を担当する。
    /// 1 つの攻撃が終わるまで更新し、完了したら次の攻撃を選び直す。
    /// </summary>
    internal sealed class BossAttackController
    {
        private static readonly IReadOnlyList<EnemyBulletSpawnRequest> EmptyRequests = Array.Empty<EnemyBulletSpawnRequest>();

        private readonly BossAttackPlanContract _plan;
        private readonly IReadOnlyDictionary<string, IBossAttack> _attacksById;
        private readonly IAttackSelector _selector;
        private readonly List<string> _attackHistory = new List<string>();

        private IBossAttack _currentAttack;
        private int _openingIndex;

        public BossAttackController(
            BossAttackPlanContract plan,
            IReadOnlyDictionary<string, IBossAttack> attacksById,
            IAttackSelector selector)
        {
            _plan = plan ?? throw new ArgumentNullException(nameof(plan));
            _attacksById = attacksById ?? throw new ArgumentNullException(nameof(attacksById));
            _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        }

        public bool LastAttackCompletedThisUpdate { get; private set; }

        public void Enter()
        {
            // フェーズ突入時は攻撃履歴とオープニング進行を初期化する。
            _currentAttack?.Exit();
            _currentAttack = null;
            _attackHistory.Clear();
            _openingIndex = 0;
            LastAttackCompletedThisUpdate = false;
        }

        public IReadOnlyList<EnemyBulletSpawnRequest> Update(BattleContext context, float deltaTime)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            LastAttackCompletedThisUpdate = false;
            EnsureCurrentAttack(context);
            if (_currentAttack == null)
                return EmptyRequests;

            IReadOnlyList<EnemyBulletSpawnRequest> requests = _currentAttack.Update(context, deltaTime);
            if (!_currentAttack.IsCompleted)
                return requests;

            string completedAttackId = _currentAttack.Id;
            _currentAttack.Exit();
            _currentAttack = null;
            LastAttackCompletedThisUpdate = true;
            RecordHistory(completedAttackId);
            return requests;
        }

        public void Exit()
        {
            _currentAttack?.Exit();
            _currentAttack = null;
            LastAttackCompletedThisUpdate = false;
        }

        private void EnsureCurrentAttack(BattleContext context)
        {
            if (_currentAttack != null)
                return;

            // まずは決め打ちのオープニング順を消化し、その後は履歴を考慮したランダム選択へ移る。
            AttackSelectionResult selection = _selector.SelectNext(_plan, _attackHistory, _openingIndex);
            if (string.IsNullOrWhiteSpace(selection.AttackId))
                return;

            if (!_attacksById.TryGetValue(selection.AttackId, out IBossAttack attack))
                throw new InvalidOperationException($"Boss attack is not configured. attackId={selection.AttackId}");

            _currentAttack = attack;
            _currentAttack.Enter(context);
            if (selection.ConsumedOpeningSequence)
                _openingIndex++;
        }

        private void RecordHistory(string attackId)
        {
            if (string.IsNullOrWhiteSpace(attackId))
                return;

            _attackHistory.Add(attackId);

            // 履歴は直近選択の偏りを抑える用途だけなので、上限を決めて肥大化を防ぐ。
            int maxHistory = Math.Max(4, _plan.HistoryWindow * 4);
            if (_attackHistory.Count > maxHistory)
                _attackHistory.RemoveRange(0, _attackHistory.Count - maxHistory);
        }
    }

    /// <summary>
    /// オープニングシーケンスを消化した後、直近履歴を避けつつランダムで次攻撃を選ぶ。
    /// </summary>
    internal sealed class OpeningSequenceThenRandomSelector : IAttackSelector
    {
        private readonly Random _random;

        public OpeningSequenceThenRandomSelector()
            : this(new Random())
        {
        }

        public OpeningSequenceThenRandomSelector(Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public AttackSelectionResult SelectNext(
            BossAttackPlanContract plan,
            IReadOnlyList<string> attackHistory,
            int openingIndex)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            IReadOnlyList<string> openingSequence = plan.OpeningSequenceAttackIds ?? Array.Empty<string>();
            if (openingIndex >= 0 && openingIndex < openingSequence.Count)
                return new AttackSelectionResult(openingSequence[openingIndex] ?? string.Empty, true);

            IReadOnlyList<string> randomCandidates = plan.RandomAttackIds;
            if (randomCandidates == null || randomCandidates.Count == 0)
                randomCandidates = openingSequence;

            if (randomCandidates == null || randomCandidates.Count == 0)
                return new AttackSelectionResult(string.Empty, false);

            int baseHistoryWindow = Math.Max(0, plan.HistoryWindow);
            int availableHistory = attackHistory != null ? attackHistory.Count : 0;
            int exclusionWindow = Math.Min(baseHistoryWindow, availableHistory);

            List<string> candidates = new List<string>(randomCandidates.Count);
            for (int activeExclusionWindow = exclusionWindow; activeExclusionWindow >= 0; activeExclusionWindow--)
            {
                candidates.Clear();
                for (int i = 0; i < randomCandidates.Count; i++)
                {
                    string attackId = randomCandidates[i];
                    if (string.IsNullOrWhiteSpace(attackId))
                        continue;

                    if (ShouldExclude(attackId, attackHistory, activeExclusionWindow))
                        continue;

                    candidates.Add(attackId);
                }

                // 候補が尽きる場合は除外窓を少しずつ緩め、必ずどこかで選べるようにする。
                if (candidates.Count > 0)
                    break;
            }

            if (candidates.Count == 0)
                return new AttackSelectionResult(string.Empty, false);

            int selectedIndex = _random.Next(candidates.Count);
            return new AttackSelectionResult(candidates[selectedIndex], false);
        }

        private static bool ShouldExclude(string attackId, IReadOnlyList<string> attackHistory, int activeExclusionWindow)
        {
            if (activeExclusionWindow <= 0 || attackHistory == null || attackHistory.Count == 0)
                return false;

            int start = Math.Max(0, attackHistory.Count - activeExclusionWindow);
            for (int i = start; i < attackHistory.Count; i++)
            {
                if (string.Equals(attackId, attackHistory[i], StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
