using System;
using System.Collections.Generic;
using Game.Contracts.MasterData.Models;

namespace Game.Domain.Battle
{
    /// <summary>
    /// マスターデータ定義 1 件ぶんの攻撃を実行するランタイム表現。
    /// 発射パターンと有効時間を束ねて、状態側から扱いやすくする。
    /// </summary>
    internal sealed class ConfiguredBossAttack : IBossAttack
    {
        private static readonly IReadOnlyList<EnemyBulletSpawnRequest> EmptyRequests = Array.Empty<EnemyBulletSpawnRequest>();

        private readonly BossAttackDefinitionContract _definition;
        private readonly IBossAttackPattern _pattern;

        private float _elapsedSeconds;
        private bool _isActive;

        public ConfiguredBossAttack(BossAttackDefinitionContract definition)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (string.IsNullOrWhiteSpace(_definition.Id))
                throw new InvalidOperationException("Boss attack id is null or empty.");
            if (_definition.ActiveDurationSeconds <= 0f && !float.IsPositiveInfinity(_definition.ActiveDurationSeconds))
            {
                throw new InvalidOperationException(
                    $"Boss attack active duration must be positive. attackId={_definition.Id}, activeDurationSeconds={_definition.ActiveDurationSeconds}");
            }

            _pattern = BossAttackPatternFactory.BuildPattern(_definition);
        }

        public string Id => _definition.Id;

        public bool IsCompleted { get; private set; }

        public void Enter(BattleContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            _pattern.Reset();
            _elapsedSeconds = 0f;
            _isActive = true;
            IsCompleted = false;
        }

        public IReadOnlyList<EnemyBulletSpawnRequest> Update(BattleContext context, float deltaTime)
        {
            if (!_isActive || IsCompleted || deltaTime <= 0f)
                return EmptyRequests;

            float remainingDuration = _definition.ActiveDurationSeconds - _elapsedSeconds;
            // 攻撃の有効時間を超えてパターンが進み過ぎないよう、このフレームで使う時間を切り詰める。
            float updateDelta = float.IsPositiveInfinity(_definition.ActiveDurationSeconds)
                ? deltaTime
                : Math.Max(0f, Math.Min(deltaTime, remainingDuration));

            IReadOnlyList<EnemyBulletSpawnRequest> requests = updateDelta > 0f
                ? _pattern.Update(context, updateDelta)
                : EmptyRequests;

            _elapsedSeconds += updateDelta;
            if (!float.IsPositiveInfinity(_definition.ActiveDurationSeconds) && _elapsedSeconds >= _definition.ActiveDurationSeconds)
                IsCompleted = true;

            return requests;
        }

        public void Exit()
        {
            _isActive = false;
        }
    }
}
