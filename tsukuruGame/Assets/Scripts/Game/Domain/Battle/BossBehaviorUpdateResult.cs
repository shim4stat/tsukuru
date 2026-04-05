using System;
using System.Collections.Generic;

namespace Game.Domain.Battle
{
    /// <summary>
    /// ボス挙動更新で戦闘進行側へ通知するイベント種別。
    /// </summary>
    public enum BossBehaviorSignal
    {
        None = 0,
        IntroCompleted = 1,
        DeadCompleted = 2,
    }

    /// <summary>
    /// 1 フレーム分のボス挙動更新結果。
    /// 弾生成要求と、演出や進行に使うシグナルをまとめて返す。
    /// </summary>
    public readonly struct BossBehaviorUpdateResult
    {
        private static readonly IReadOnlyList<EnemyBulletSpawnRequest> EmptyRequests = Array.Empty<EnemyBulletSpawnRequest>();

        public static BossBehaviorUpdateResult Empty => new BossBehaviorUpdateResult(EmptyRequests, BossBehaviorSignal.None);

        public BossBehaviorUpdateResult(
            IReadOnlyList<EnemyBulletSpawnRequest> spawnRequests,
            BossBehaviorSignal signal)
        {
            SpawnRequests = spawnRequests ?? throw new ArgumentNullException(nameof(spawnRequests));
            Signal = signal;
        }

        public IReadOnlyList<EnemyBulletSpawnRequest> SpawnRequests { get; }

        public BossBehaviorSignal Signal { get; }
    }
}
