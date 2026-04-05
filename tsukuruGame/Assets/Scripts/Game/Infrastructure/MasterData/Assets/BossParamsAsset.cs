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

    [Serializable]
    public sealed class BossAttackDefinitionAsset
    {
        [SerializeField] private string id = string.Empty;
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
        [SerializeField] private float activeDurationSeconds = 1.0f;

        public string Id => id;
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
        public float ActiveDurationSeconds => activeDurationSeconds;
    }

    [Serializable]
    public sealed class BossAttackPlanAsset
    {
        [SerializeField] private List<string> openingSequenceAttackIds = new List<string>();
        [SerializeField] private List<string> randomAttackIds = new List<string>();
        [SerializeField] private int historyWindow = 2;

        public IReadOnlyList<string> OpeningSequenceAttackIds => openingSequenceAttackIds;
        public IReadOnlyList<string> RandomAttackIds => randomAttackIds;
        public int HistoryWindow => historyWindow;
    }

    [Serializable]
    public sealed class BossStateTransitionAsset
    {
        [SerializeField] private string nextStateId = string.Empty;
        [SerializeField] private BossTransitionConditionType conditionType = BossTransitionConditionType.ExternalSignal;
        [SerializeField] private float threshold;
        [SerializeField] private string signalId = string.Empty;

        public string NextStateId => nextStateId;
        public BossTransitionConditionType ConditionType => conditionType;
        public float Threshold => threshold;
        public string SignalId => signalId;
    }

    [Serializable]
    public sealed class BossStateDefinitionAsset
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private BossStateType stateType = BossStateType.Phase;
        [SerializeField] private BossAttackPlanAsset attackPlan = new BossAttackPlanAsset();
        [SerializeField] private List<BossStateTransitionAsset> transitions = new List<BossStateTransitionAsset>();

        public string Id => id;
        public BossStateType StateType => stateType;
        public BossAttackPlanAsset AttackPlan => attackPlan;
        public IReadOnlyList<BossStateTransitionAsset> Transitions => transitions;
    }

    [CreateAssetMenu(menuName = "Game/MasterData/BossParams", fileName = "BossParams")]
    public sealed class BossParamsAsset : ScriptableObject
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private List<int> gaugeMaxHps = new List<int>();
        [SerializeField] private int baseDropEnergyAmount;
        [SerializeField] private float minDropIntervalSeconds;
        [SerializeField] private float actionIntervalSeconds = 1.0f;
        [SerializeField] private string initialStateId = string.Empty;
        [SerializeField] private List<BossStateDefinitionAsset> states = new List<BossStateDefinitionAsset>();
        [SerializeField] private List<BossAttackDefinitionAsset> attacks = new List<BossAttackDefinitionAsset>();
        [SerializeField] private List<BossPhasePatternAsset> phasePatterns = new List<BossPhasePatternAsset>();

        public string Id => id;
        public IReadOnlyList<int> GaugeMaxHps => gaugeMaxHps;
        public int BaseDropEnergyAmount => baseDropEnergyAmount;
        public float MinDropIntervalSeconds => minDropIntervalSeconds;
        public float ActionIntervalSeconds => actionIntervalSeconds;
        public string InitialStateId => initialStateId;
        public IReadOnlyList<BossStateDefinitionAsset> States => states;
        public IReadOnlyList<BossAttackDefinitionAsset> Attacks => attacks;
        public IReadOnlyList<BossPhasePatternAsset> PhasePatterns => phasePatterns;
    }
}
