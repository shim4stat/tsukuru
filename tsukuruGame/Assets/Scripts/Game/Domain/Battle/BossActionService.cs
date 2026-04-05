using System;
using System.Collections.Generic;
using Game.Contracts.MasterData.Models;

namespace Game.Domain.Battle
{
    /// <summary>
    /// Combat 中に、現在ゲージに対応した弾幕パターンを時間経過で進める簡易サービス。
    /// </summary>
    public sealed class BossActionService
    {
        private static readonly IReadOnlyList<EnemyBulletSpawnRequest> EmptyRequests = Array.Empty<EnemyBulletSpawnRequest>();

        private Boss _boss;
        private IBossAttackPattern[] _patternsByGauge = Array.Empty<IBossAttackPattern>();
        private int _activeGaugeIndex = -1;
        private bool _isInitialized;

        public void Initialize(Boss boss, BossParamsContract bossParams)
        {
            if (boss == null)
                throw new ArgumentNullException(nameof(boss));
            if (bossParams == null)
                throw new ArgumentNullException(nameof(bossParams));
            if (bossParams.GaugeMaxHps == null || bossParams.GaugeMaxHps.Count == 0)
                throw new InvalidOperationException("Boss gaugeMaxHps is null or empty.");

            _boss = boss;
            _patternsByGauge = BossAttackPatternFactory.BuildPatternsByGauge(bossParams);
            _activeGaugeIndex = -1;
            _isInitialized = true;
        }

        public IReadOnlyList<EnemyBulletSpawnRequest> Update(BattleContext context, float deltaTime)
        {
            EnsureInitialized();

            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (context.Boss == null)
                throw new InvalidOperationException("BattleContext.Boss is not initialized.");
            if (!ReferenceEquals(context.Boss, _boss))
                throw new InvalidOperationException("BattleContext.Boss does not match initialized boss.");

            if (context.Phase != BattlePhase.Combat)
                return EmptyRequests;
            if (context.Boss.IsAllGaugesEmpty())
                return EmptyRequests;
            if (deltaTime <= 0f)
                return EmptyRequests;

            int gaugeIndex = context.Boss.GetCurrentGaugeIndex();
            IBossAttackPattern pattern = GetPatternForGauge(gaugeIndex);
            if (_activeGaugeIndex != gaugeIndex)
            {
                // ゲージが切り替わった瞬間は新しいパターンを最初から再開する。
                pattern.Reset();
                _activeGaugeIndex = gaugeIndex;
            }

            return pattern.Update(context, deltaTime);
        }

        public void Reset()
        {
            for (int i = 0; i < _patternsByGauge.Length; i++)
            {
                _patternsByGauge[i]?.Reset();
            }

            _activeGaugeIndex = -1;
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("BossActionService is not initialized.");
        }

        private IBossAttackPattern GetPatternForGauge(int gaugeIndex)
        {
            if (gaugeIndex < 0 || gaugeIndex >= _patternsByGauge.Length)
            {
                throw new InvalidOperationException(
                    $"Boss gauge index is out of range. gaugeIndex={gaugeIndex}, patternCount={_patternsByGauge.Length}");
            }

            IBossAttackPattern pattern = _patternsByGauge[gaugeIndex];
            if (pattern == null)
                throw new InvalidOperationException($"Boss attack pattern is not configured for gaugeIndex={gaugeIndex}.");

            return pattern;
        }
    }
}
