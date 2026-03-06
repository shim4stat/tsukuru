using System;
using System.Collections.Generic;
using Game.Contracts.MasterData.Models;
using UnityEngine;

namespace Game.Infrastructure.MasterData.Assets
{
    [Serializable]
    public sealed class BossPhasePatternAsset
    {
        [SerializeField] private BossAttackPatternType patternType = BossAttackPatternType.SingleShot;
        [SerializeField] private float fireIntervalSeconds = 1.0f;
        [SerializeField] private int shotCount = 1;
        [SerializeField] private float spreadDegrees;
        [SerializeField] private int burstShotCount = 3;
        [SerializeField] private float burstShotIntervalSeconds = 0.15f;
        [SerializeField] private float bulletSpeed = 3.0f;
        [SerializeField] private float bulletLifetimeSeconds = 2.0f;
        [SerializeField] private int bulletDamage = 1;
        [SerializeField] private int absorbableEnergyAmount = 1;
        [SerializeField] private EnemyBulletBehaviorTypeContract bulletBehaviorType = EnemyBulletBehaviorTypeContract.Straight;
        [SerializeField] private Vector3 spawnOffset = Vector3.zero;
        [SerializeField] private Vector3 fireDirection = Vector3.down;

        public BossAttackPatternType PatternType => patternType;
        public float FireIntervalSeconds => fireIntervalSeconds;
        public int ShotCount => shotCount;
        public float SpreadDegrees => spreadDegrees;
        public int BurstShotCount => burstShotCount;
        public float BurstShotIntervalSeconds => burstShotIntervalSeconds;
        public float BulletSpeed => bulletSpeed;
        public float BulletLifetimeSeconds => bulletLifetimeSeconds;
        public int BulletDamage => bulletDamage;
        public int AbsorbableEnergyAmount => absorbableEnergyAmount;
        public EnemyBulletBehaviorTypeContract BulletBehaviorType => bulletBehaviorType;
        public Vector3 SpawnOffset => spawnOffset;
        public Vector3 FireDirection => fireDirection;
    }

    [CreateAssetMenu(menuName = "Game/MasterData/BossParams", fileName = "BossParams")]
    public sealed class BossParamsAsset : ScriptableObject
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private List<int> gaugeMaxHps = new List<int>();
        [SerializeField] private int baseDropEnergyAmount;
        [SerializeField] private float minDropIntervalSeconds;
        [SerializeField] private float actionIntervalSeconds = 1.0f;
        [SerializeField] private List<BossPhasePatternAsset> phasePatterns = new List<BossPhasePatternAsset>();

        public string Id => id;
        public IReadOnlyList<int> GaugeMaxHps => gaugeMaxHps;
        public int BaseDropEnergyAmount => baseDropEnergyAmount;
        public float MinDropIntervalSeconds => minDropIntervalSeconds;
        public float ActionIntervalSeconds => actionIntervalSeconds;
        public IReadOnlyList<BossPhasePatternAsset> PhasePatterns => phasePatterns;
    }
}
