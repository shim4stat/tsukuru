using System;
using System.Numerics;
using Game.Contracts.MasterData.Models;

namespace Game.Domain.Battle
{
    internal sealed class PlayerChargeAndReturnAction : BossActionBase
    {
        private readonly PlayerChargeAndReturnConfig _config;
        private Vector3 _startPosition;
        private Vector3 _chargeEndPosition;

        public PlayerChargeAndReturnAction(PlayerChargeAndReturnConfig config)
            : base(config.Id, config.DurationFrames, config.CancelPolicy)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        protected override void OnEnter(BossActionContext action)
        {
            _startPosition = action.Boss.Position;
            Vector3 targetPosition = action.Player != null
                ? action.Player.Position
                : _startPosition + new Vector3(0f, -2f, 0f);
            Vector3 direction = targetPosition - _startPosition;
            if (direction.LengthSquared() <= 0f)
                direction = new Vector3(0f, -1f, 0f);

            _chargeEndPosition = targetPosition + (Vector3.Normalize(direction) * _config.OvershootDistance);
        }

        protected override void OnFrame(BossActionContext action)
        {
            int frame = action.ElapsedFrames;
            Vector3 position;
            if (frame < _config.AimFrames)
            {
                position = _startPosition;
            }
            else if (frame < _config.AimFrames + _config.ChargeFrames)
            {
                int chargeFrame = frame - _config.AimFrames;
                float t = chargeFrame / (float)Math.Max(1, _config.ChargeFrames - 1);
                position = BossActionMath.Lerp(_startPosition, _chargeEndPosition, BossActionMath.SmoothStep01(t));
                action.SetHitbox("charge_body", Vector3.Zero, _config.ChargeHitboxRadius, _config.ChargeDamage);
            }
            else if (frame < _config.AimFrames + _config.ChargeFrames + _config.RecoveryFrames)
            {
                position = _chargeEndPosition;
            }
            else
            {
                int returnFrame = frame - _config.AimFrames - _config.ChargeFrames - _config.RecoveryFrames;
                float t = returnFrame / (float)Math.Max(1, _config.ReturnFrames - 1);
                position = BossActionMath.Lerp(_chargeEndPosition, _config.ReturnPosition, BossActionMath.SmoothStep01(t));
            }

            action.MoveBossTo(position);
            action.SetHurtbox("body", Vector3.Zero, _config.HurtboxRadius);
        }
    }

    internal sealed class PlayerChargeAndReturnConfig
    {
        private const string DefaultConfigKey = "default";

        private PlayerChargeAndReturnConfig(
            string id,
            int aimFrames,
            int chargeFrames,
            int recoveryFrames,
            int returnFrames,
            BossActionCancelPolicy cancelPolicy,
            Vector3 returnPosition,
            float overshootDistance,
            float chargeHitboxRadius,
            int chargeDamage,
            float hurtboxRadius)
        {
            Id = id;
            AimFrames = aimFrames;
            ChargeFrames = chargeFrames;
            RecoveryFrames = recoveryFrames;
            ReturnFrames = returnFrames;
            CancelPolicy = cancelPolicy;
            ReturnPosition = returnPosition;
            OvershootDistance = overshootDistance;
            ChargeHitboxRadius = chargeHitboxRadius;
            ChargeDamage = chargeDamage;
            HurtboxRadius = hurtboxRadius;
        }

        public string Id { get; }

        public int AimFrames { get; }

        public int ChargeFrames { get; }

        public int RecoveryFrames { get; }

        public int ReturnFrames { get; }

        public int DurationFrames => AimFrames + ChargeFrames + RecoveryFrames + ReturnFrames;

        public BossActionCancelPolicy CancelPolicy { get; }

        public Vector3 ReturnPosition { get; }

        public float OvershootDistance { get; }

        public float ChargeHitboxRadius { get; }

        public int ChargeDamage { get; }

        public float HurtboxRadius { get; }

        public static PlayerChargeAndReturnConfig CreateDefault(string id, string configKey)
        {
            ValidateConfigKey(id, configKey);
            return new PlayerChargeAndReturnConfig(
                id,
                aimFrames: 30,
                chargeFrames: 24,
                recoveryFrames: 18,
                returnFrames: 54,
                cancelPolicy: BossActionCancelPolicy.AlwaysCancelable,
                returnPosition: new Vector3(0f, 4f, 0f),
                overshootDistance: 0.9f,
                chargeHitboxRadius: 0.75f,
                chargeDamage: 1,
                hurtboxRadius: 0.6f);
        }

        private static void ValidateConfigKey(string id, string configKey)
        {
            if (string.IsNullOrWhiteSpace(configKey) || string.Equals(configKey, DefaultConfigKey, StringComparison.Ordinal))
                return;

            throw new InvalidOperationException($"Unsupported PlayerChargeAndReturn config key. actionId={id}, configKey={configKey}");
        }
    }
}
