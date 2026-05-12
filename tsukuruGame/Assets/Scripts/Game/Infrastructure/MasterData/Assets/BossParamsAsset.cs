using System;
using System.Collections.Generic;
using Game.Contracts.MasterData.Models;
using UnityEngine;
using UnityEngine.Serialization;

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
    public sealed class BossBulletPatternDefinitionAsset
    {
        [SerializeField] private BossAttackPatternType patternType = BossAttackPatternType.SingleShot;
        [SerializeField] private int initialDelayFrames = -1;
        [SerializeField] private int fireIntervalFrames = BossActionTimelineConstants.FramesPerSecond;
        [SerializeField] private int shotCount = 1;
        [SerializeField] private float spreadDegrees;
        [SerializeField] private int burstShotCount = 3;
        [SerializeField] private int burstShotIntervalFrames = 9;
        [SerializeField] private float bulletSpeed = 3.0f;
        [SerializeField] private float bulletLifetimeSeconds = 2.0f;
        [SerializeField] private int bulletDamage = 1;
        [SerializeField] private int absorbableEnergyAmount = 1;
        [SerializeField] private EnemyBulletBehaviorTypeContract bulletBehaviorType = EnemyBulletBehaviorTypeContract.Straight;
        [SerializeField] private Vector3 spawnOffset = Vector3.zero;
        [SerializeField] private Vector3 fireDirection = Vector3.down;

        public BossAttackPatternType PatternType => patternType;
        public int InitialDelayFrames => initialDelayFrames;
        public int FireIntervalFrames => fireIntervalFrames;
        public int ShotCount => shotCount;
        public float SpreadDegrees => spreadDegrees;
        public int BurstShotCount => burstShotCount;
        public int BurstShotIntervalFrames => burstShotIntervalFrames;
        public float BulletSpeed => bulletSpeed;
        public float BulletLifetimeSeconds => bulletLifetimeSeconds;
        public int BulletDamage => bulletDamage;
        public int AbsorbableEnergyAmount => absorbableEnergyAmount;
        public EnemyBulletBehaviorTypeContract BulletBehaviorType => bulletBehaviorType;
        public Vector3 SpawnOffset => spawnOffset;
        public Vector3 FireDirection => fireDirection;
    }

    [Serializable]
    public sealed class BossActionCommandAsset
    {
        [SerializeField] private int triggerFrame;
        [SerializeField] private BossActionCommandType commandType = BossActionCommandType.SpawnBulletPattern;
        [SerializeField] private string animationStateName = string.Empty;
        [SerializeField] private BossBulletPatternDefinitionAsset bulletPattern = new BossBulletPatternDefinitionAsset();
        [SerializeField] private int emitterDurationFrames = -1;
        [SerializeField] private string signalId = string.Empty;
        [SerializeField] private int crossFadeFrames;
        [SerializeField] private string enemyDefinitionId = string.Empty;
        [SerializeField] private Vector3 spawnOffset = Vector3.zero;
        [SerializeField] private string effectId = string.Empty;
        [SerializeField] private Vector3 effectLocalOffset = Vector3.zero;
        [SerializeField] private string soundId = string.Empty;
        [SerializeField] private float volumeScale = 1.0f;

        public int TriggerFrame => triggerFrame;
        public BossActionCommandType CommandType => commandType;
        public string AnimationStateName => animationStateName;
        public BossBulletPatternDefinitionAsset BulletPattern => bulletPattern;
        public int? EmitterDurationFrames => emitterDurationFrames > 0 ? emitterDurationFrames : (int?)null;
        public string SignalId => signalId;
        public int CrossFadeFrames => crossFadeFrames;
        public string EnemyDefinitionId => enemyDefinitionId;
        public Vector3 SpawnOffset => spawnOffset;
        public string EffectId => effectId;
        public Vector3 EffectLocalOffset => effectLocalOffset;
        public string SoundId => soundId;
        public float VolumeScale => volumeScale;
    }

    [Serializable]
    public sealed class BossMoveWindowPayloadAsset
    {
        [SerializeField] private Vector3 velocityPerSecond = Vector3.zero;

        public Vector3 VelocityPerSecond => velocityPerSecond;
    }

    [Serializable]
    public sealed class BossHitboxWindowPayloadAsset
    {
        [SerializeField] private Vector3 offset = Vector3.zero;
        [SerializeField] private float radius = 0.5f;
        [SerializeField] private int damage = 1;

        public Vector3 Offset => offset;
        public float Radius => radius;
        public int Damage => damage;
    }

    [Serializable]
    public sealed class BossHurtboxWindowPayloadAsset
    {
        [SerializeField] private Vector3 offset = Vector3.zero;
        [SerializeField] private float radius = 0.5f;

        public Vector3 Offset => offset;
        public float Radius => radius;
    }

    [Serializable]
    public sealed class BossCancelWindowPayloadAsset
    {
        [SerializeField] private string cancelTag = string.Empty;

        public string CancelTag => cancelTag;
    }

    [Serializable]
    public sealed class BossActionWindowAsset
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private BossActionWindowType windowType = BossActionWindowType.MoveWindow;
        [SerializeField] private int startFrameInclusive;
        [SerializeField] private int endFrameExclusive;
        [SerializeField] private BossMoveWindowPayloadAsset moveWindow = new BossMoveWindowPayloadAsset();
        [SerializeField] private BossHitboxWindowPayloadAsset hitboxWindow = new BossHitboxWindowPayloadAsset();
        [SerializeField] private BossHurtboxWindowPayloadAsset hurtboxWindow = new BossHurtboxWindowPayloadAsset();
        [SerializeField] private BossCancelWindowPayloadAsset cancelWindow = new BossCancelWindowPayloadAsset();

        public string Id => id;
        public BossActionWindowType WindowType => windowType;
        public int StartFrameInclusive => startFrameInclusive;
        public int EndFrameExclusive => endFrameExclusive;
        public BossMoveWindowPayloadAsset MoveWindow => moveWindow;
        public BossHitboxWindowPayloadAsset HitboxWindow => hitboxWindow;
        public BossHurtboxWindowPayloadAsset HurtboxWindow => hurtboxWindow;
        public BossCancelWindowPayloadAsset CancelWindow => cancelWindow;
    }

    [Serializable]
    public sealed class BossActionDefinitionAsset
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string actionTypeId = string.Empty;
        [SerializeField] private string configKey = string.Empty;
        [SerializeField] private string animationStateName = string.Empty;
        [SerializeField] private BossActionEndConditionType endConditionType = BossActionEndConditionType.Manual;
        [SerializeField] private BossActionCancelPolicy cancelPolicy = BossActionCancelPolicy.AlwaysCancelable;
        [SerializeField] private int totalDurationFrames;
        [SerializeField] private List<BossActionCommandAsset> commands = new List<BossActionCommandAsset>();
        [SerializeField] private List<BossActionWindowAsset> windows = new List<BossActionWindowAsset>();

        public string Id => id;
        public string ActionTypeId => actionTypeId;
        public string ConfigKey => configKey;
        public string AnimationStateName => animationStateName;
        public BossActionEndConditionType EndConditionType => endConditionType;
        public BossActionCancelPolicy CancelPolicy => cancelPolicy;
        public int TotalDurationFrames => totalDurationFrames;
        public IReadOnlyList<BossActionCommandAsset> Commands => commands;
        public IReadOnlyList<BossActionWindowAsset> Windows => windows;
    }

    [Serializable]
    public sealed class BossActionPlanAsset
    {
        [FormerlySerializedAs("openingSequenceAttackIds")]
        [SerializeField] private List<string> openingSequenceActionIds = new List<string>();
        [FormerlySerializedAs("randomAttackIds")]
        [SerializeField] private List<string> randomActionIds = new List<string>();
        [SerializeField] private int historyWindow = 2;

        public IReadOnlyList<string> OpeningSequenceActionIds => openingSequenceActionIds;
        public IReadOnlyList<string> RandomActionIds => randomActionIds;
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
        [FormerlySerializedAs("attackPlan")]
        [SerializeField] private BossActionPlanAsset actionPlan = new BossActionPlanAsset();
        [SerializeField] private List<BossStateTransitionAsset> transitions = new List<BossStateTransitionAsset>();

        public string Id => id;
        public BossStateType StateType => stateType;
        public BossActionPlanAsset ActionPlan => actionPlan;
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
        [FormerlySerializedAs("attacks")]
        [SerializeField] private List<BossActionDefinitionAsset> actions = new List<BossActionDefinitionAsset>();
        [SerializeField] private List<BossPhasePatternAsset> phasePatterns = new List<BossPhasePatternAsset>();

        public string Id => id;
        public IReadOnlyList<int> GaugeMaxHps => gaugeMaxHps;
        public int BaseDropEnergyAmount => baseDropEnergyAmount;
        public float MinDropIntervalSeconds => minDropIntervalSeconds;
        public string InitialStateId => initialStateId;
        public IReadOnlyList<BossStateDefinitionAsset> States => states;
        public IReadOnlyList<BossActionDefinitionAsset> Actions => actions;
    }
}
