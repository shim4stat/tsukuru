using System;
using System.Numerics;
using Game.Contracts.MasterData.Models;

namespace Game.Domain.Battle
{
    internal sealed class VerticalSweepShotAction : BossActionBase
    {
        private readonly VerticalSweepShotConfig _config;
        private Vector3 _startPosition;
        private float _currentAngleDegrees;

        public VerticalSweepShotAction(VerticalSweepShotConfig config)
            : base(config.Id, config.DurationFrames, config.CancelPolicy)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        protected override void OnEnter(BossActionContext action)
        {
            _startPosition = action.Boss.Position;
            _currentAngleDegrees = _config.StartAngleDegrees;
        }

        protected override void OnFrame(BossActionContext action)
        {
            float yOffset = (float)Math.Sin(action.Progress01 * BossActionMath.Tau * _config.MoveCycles) *
                _config.MoveAmplitude;
            action.MoveBossTo(_startPosition + new Vector3(0f, yOffset, 0f));

            if (action.IsEvery(_config.FireIntervalFrames))
            {
                action.FireBullet(BossActionMath.DirectionFromDegrees(_currentAngleDegrees), _config.Bullet);
                _currentAngleDegrees += _config.AngleStepDegrees;
            }

            action.SetHurtbox("body", Vector3.Zero, _config.HurtboxRadius);
        }
    }

    internal sealed class VerticalSweepShotConfig
    {
        private const string DefaultConfigKey = "default";

        private VerticalSweepShotConfig(
            string id,
            int durationFrames,
            BossActionCancelPolicy cancelPolicy,
            float moveAmplitude,
            float moveCycles,
            int fireIntervalFrames,
            float startAngleDegrees,
            float angleStepDegrees,
            BossActionBulletConfig bullet,
            float hurtboxRadius)
        {
            Id = id;
            DurationFrames = durationFrames;
            CancelPolicy = cancelPolicy;
            MoveAmplitude = moveAmplitude;
            MoveCycles = moveCycles;
            FireIntervalFrames = fireIntervalFrames;
            StartAngleDegrees = startAngleDegrees;
            AngleStepDegrees = angleStepDegrees;
            Bullet = bullet;
            HurtboxRadius = hurtboxRadius;
        }

        public string Id { get; }

        public int DurationFrames { get; }

        public BossActionCancelPolicy CancelPolicy { get; }

        public float MoveAmplitude { get; }

        public float MoveCycles { get; }

        public int FireIntervalFrames { get; }

        public float StartAngleDegrees { get; }

        public float AngleStepDegrees { get; }

        public BossActionBulletConfig Bullet { get; }

        public float HurtboxRadius { get; }

        public static VerticalSweepShotConfig CreateDefault(string id, string configKey)
        {
            ValidateConfigKey(id, configKey);
            return new VerticalSweepShotConfig(
                id,
                durationFrames: 240,
                cancelPolicy: BossActionCancelPolicy.AlwaysCancelable,
                moveAmplitude: 1.15f,
                moveCycles: 2.0f,
                fireIntervalFrames: 10,
                startAngleDegrees: -115f,
                angleStepDegrees: 7.5f,
                bullet: new BossActionBulletConfig(
                    new Vector3(0f, -0.45f, 0f),
                    speed: 4.8f,
                    damage: 1,
                    lifetimeSeconds: 4.0f,
                    absorbableEnergyAmount: 1),
                hurtboxRadius: 0.6f);
        }

        private static void ValidateConfigKey(string id, string configKey)
        {
            if (string.IsNullOrWhiteSpace(configKey) || string.Equals(configKey, DefaultConfigKey, StringComparison.Ordinal))
                return;

            throw new InvalidOperationException($"Unsupported VerticalSweepShot config key. actionId={id}, configKey={configKey}");
        }
    }
}
