using System;
using Game.Contracts.MasterData.Models;

namespace Game.Domain.Battle
{
    public class Boss
    {
        private int[] _gaugeMaxHps = Array.Empty<int>();
        private int _currentGaugeIndex;
        private int _currentHpInGauge;
        private bool _isInitialized;

        public void Initialize(BossParamsContract bossParams)
        {
            if (bossParams == null)
                throw new ArgumentNullException(nameof(bossParams));

            if (bossParams.GaugeMaxHps == null || bossParams.GaugeMaxHps.Count == 0)
                throw new InvalidOperationException("Boss gaugeMaxHps is null or empty.");

            int[] gauges = new int[bossParams.GaugeMaxHps.Count];
            for (int i = 0; i < bossParams.GaugeMaxHps.Count; i++)
            {
                int gaugeHp = bossParams.GaugeMaxHps[i];
                if (gaugeHp <= 0)
                    throw new InvalidOperationException($"Boss gaugeMaxHps contains invalid value at index {i}: {gaugeHp}");

                gauges[i] = gaugeHp;
            }

            _gaugeMaxHps = gauges;
            _currentGaugeIndex = 0;
            _currentHpInGauge = _gaugeMaxHps[0];
            _isInitialized = true;
        }

        public void TakeDamage(int amount)
        {
            EnsureInitialized();

            if (amount <= 0)
                return;

            if (IsAllGaugesEmpty())
                return;

            int remaining = amount;
            while (remaining > 0 && !IsAllGaugesEmpty())
            {
                int consumed = Math.Min(remaining, _currentHpInGauge);
                _currentHpInGauge -= consumed;
                remaining -= consumed;

                if (_currentHpInGauge == 0 && _currentGaugeIndex < _gaugeMaxHps.Length - 1)
                {
                    _currentGaugeIndex++;
                    _currentHpInGauge = _gaugeMaxHps[_currentGaugeIndex];
                }
            }
        }

        public bool IsAlive()
        {
            EnsureInitialized();
            return !IsAllGaugesEmpty();
        }

        public bool IsCurrentGaugeEmpty()
        {
            EnsureInitialized();
            return _currentHpInGauge <= 0;
        }

        public bool IsAllGaugesEmpty()
        {
            EnsureInitialized();
            return _currentGaugeIndex == _gaugeMaxHps.Length - 1 && _currentHpInGauge <= 0;
        }

        public int GetCurrentGaugeIndex()
        {
            EnsureInitialized();
            return _currentGaugeIndex;
        }

        public int GetCurrentGaugeHp()
        {
            EnsureInitialized();
            return _currentHpInGauge;
        }

        public int GetCurrentGaugeMaxHp()
        {
            EnsureInitialized();
            return _gaugeMaxHps[_currentGaugeIndex];
        }

        public float GetCurrentGaugeHpNormalized()
        {
            EnsureInitialized();

            int maxHp = GetCurrentGaugeMaxHp();
            if (maxHp <= 0)
                return 0f;

            if (_currentHpInGauge <= 0)
                return 0f;

            if (_currentHpInGauge >= maxHp)
                return 1f;

            return (float)_currentHpInGauge / maxHp;
        }

        public int GetGaugeCount()
        {
            EnsureInitialized();
            return _gaugeMaxHps.Length;
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Boss is not initialized.");
        }
    }
}
