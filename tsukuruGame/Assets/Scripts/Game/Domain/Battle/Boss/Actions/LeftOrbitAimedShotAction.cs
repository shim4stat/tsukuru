using System;
using System.Numerics;
using Game.Contracts.MasterData.Models;

namespace Game.Domain.Battle
{
    internal sealed class LeftOrbitAimedShotAction : BossActionBase
    {
        private readonly LeftOrbitAimedShotConfig _config;
        private Vector3 _entryPosition;
        private bool[] _cycleFired;

        public LeftOrbitAimedShotAction(LeftOrbitAimedShotConfig config)
            : base(config.Id, config.DurationFrames, config.CancelPolicy)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        protected override void OnEnter(BossActionContext action)
        {
            _entryPosition = action.Boss.Position;
            _cycleFired = new bool[_config.RepeatCount];
        }

        protected override void OnFrame(BossActionContext action)
        {
            int cycleIndex = Math.Min(action.ElapsedFrames / _config.CycleFrames, _config.RepeatCount - 1);
            int cycleFrame = action.ElapsedFrames - (cycleIndex * _config.CycleFrames);

            Vector3 position;
            Vector3 cycleStart = cycleIndex == 0 ? _entryPosition : GetStopPosition(cycleIndex - 1);
            Vector3 orbitStart = GetOrbitPosition(cycleIndex, 0f);
            if (cycleFrame < _config.MoveToOrbitFrames)
            {
                float t = cycleFrame / (float)Math.Max(1, _config.MoveToOrbitFrames - 1);
                position = BossActionMath.Lerp(cycleStart, orbitStart, BossActionMath.SmoothStep01(t));
            }
            else if (cycleFrame < _config.MoveToOrbitFrames + _config.OrbitFrames)
            {
                int orbitFrame = cycleFrame - _config.MoveToOrbitFrames;
                float t = orbitFrame / (float)Math.Max(1, _config.OrbitFrames - 1);
                position = GetOrbitPosition(cycleIndex, t);
            }
            else
            {
                position = orbitStart;
                FireAimedShotOnce(action, cycleIndex);
            }

            action.MoveBossTo(position);
            action.SetHurtbox("body", Vector3.Zero, _config.HurtboxRadius);
        }

        private void FireAimedShotOnce(BossActionContext action, int cycleIndex)
        {
            if (_cycleFired == null || cycleIndex < 0 || cycleIndex >= _cycleFired.Length)
                return;
            if (_cycleFired[cycleIndex])
                return;

            action.FireBulletAtPlayer(_config.Bullet);
            _cycleFired[cycleIndex] = true;
        }

        private Vector3 GetOrbitPosition(int cycleIndex, float progress01)
        {
            float startAngle = _config.StartAngleDegrees + (_config.AngleOffsetPerCycleDegrees * cycleIndex);
            float angle = startAngle + (_config.OrbitDegrees * BossActionMath.Clamp01(progress01));
            return _config.OrbitAnchor + (BossActionMath.DirectionFromDegrees(angle) * _config.OrbitRadius);
        }

        private Vector3 GetStopPosition(int cycleIndex)
        {
            return GetOrbitPosition(cycleIndex, 1f);
        }
    }

    internal sealed class LeftOrbitAimedShotConfig
    {
        private const string DefaultConfigKey = "default";

        private LeftOrbitAimedShotConfig(
            string id,
            int repeatCount,
            int moveToOrbitFrames,
            int orbitFrames,
            int stopFrames,
            BossActionCancelPolicy cancelPolicy,
            Vector3 orbitAnchor,
            float orbitRadius,
            float startAngleDegrees,
            float angleOffsetPerCycleDegrees,
            float orbitDegrees,
            BossActionBulletConfig bullet,
            float hurtboxRadius)
        {
            Id = id;
            RepeatCount = repeatCount;
            MoveToOrbitFrames = moveToOrbitFrames;
            OrbitFrames = orbitFrames;
            StopFrames = stopFrames;
            CancelPolicy = cancelPolicy;
            OrbitAnchor = orbitAnchor;
            OrbitRadius = orbitRadius;
            StartAngleDegrees = startAngleDegrees;
            AngleOffsetPerCycleDegrees = angleOffsetPerCycleDegrees;
            OrbitDegrees = orbitDegrees;
            Bullet = bullet;
            HurtboxRadius = hurtboxRadius;
        }

        public string Id { get; }

        public int RepeatCount { get; }

        public int MoveToOrbitFrames { get; }

        public int OrbitFrames { get; }

        public int StopFrames { get; }

        public int CycleFrames => MoveToOrbitFrames + OrbitFrames + StopFrames;

        public int DurationFrames => CycleFrames * RepeatCount;

        public BossActionCancelPolicy CancelPolicy { get; }

        public Vector3 OrbitAnchor { get; }

        public float OrbitRadius { get; }

        public float StartAngleDegrees { get; }

        public float AngleOffsetPerCycleDegrees { get; }

        public float OrbitDegrees { get; }

        public BossActionBulletConfig Bullet { get; }

        public float HurtboxRadius { get; }

        public static LeftOrbitAimedShotConfig CreateDefault(string id, string configKey)
        {
            ValidateConfigKey(id, configKey);
            return new LeftOrbitAimedShotConfig(
                id,
                repeatCount: 3,
                moveToOrbitFrames: 24,
                orbitFrames: 72,
                stopFrames: 24,
                cancelPolicy: BossActionCancelPolicy.AlwaysCancelable,
                orbitAnchor: new Vector3(0.6f, 4.2f, 0f),
                orbitRadius: 0.8f,
                startAngleDegrees: 0f,
                angleOffsetPerCycleDegrees: 120f,
                orbitDegrees: 360f,
                bullet: new BossActionBulletConfig(
                    new Vector3(0f, -0.45f, 0f),
                    speed: 5.4f,
                    damage: 1,
                    lifetimeSeconds: 4.0f,
                    absorbableEnergyAmount: 1),
                hurtboxRadius: 0.6f);
        }

        private static void ValidateConfigKey(string id, string configKey)
        {
            if (string.IsNullOrWhiteSpace(configKey) || string.Equals(configKey, DefaultConfigKey, StringComparison.Ordinal))
                return;

            throw new InvalidOperationException($"Unsupported LeftOrbitAimedShot config key. actionId={id}, configKey={configKey}");
        }
    }
}
