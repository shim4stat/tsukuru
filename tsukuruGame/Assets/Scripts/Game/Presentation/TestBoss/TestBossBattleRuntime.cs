using System;
using System.Collections.Generic;
using NumericsVector3 = System.Numerics.Vector3;
using Game.Contracts.MasterData.Models;
using Game.Domain.Battle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Presentation.TestBoss
{
    internal sealed class TestBossBattleRuntime : IDisposable
    {
        private readonly Transform _parent;
        private readonly BattleContext _context;
        private readonly PlayerParamsContract _playerParams;
        private readonly EnemyBulletService _enemyBulletService;
        private readonly TestBossBossView _bossPrefab;
        private readonly TestBossBulletView _bulletPrefab;
        private readonly bool _usePrefabViews;
        private readonly Dictionary<EnemyBullet, ITestBossEntityView> _bulletViews =
            new Dictionary<EnemyBullet, ITestBossEntityView>();

        private readonly Color _playerNormalColor = new Color(0.15f, 0.85f, 1.0f, 1.0f);
        private readonly Color _playerDashColor = new Color(0.35f, 1.0f, 0.45f, 1.0f);
        private readonly Color _playerHitColor = new Color(1.0f, 0.95f, 0.35f, 1.0f);
        private readonly Color _playerDeadColor = new Color(0.4f, 0.4f, 0.4f, 1.0f);

        private GameObject _root;
        private PlayerMoveManager _playerMoveManager;
        private ITestBossTintableView _playerView;
        private ITestBossTintableView _bossView;
        private float _playerDamageInvulnerabilityRemaining;
        private bool _isInitialized;

        public TestBossBattleRuntime(
            Transform parent,
            BattleContext context,
            PlayerParamsContract playerParams,
            EnemyBulletService enemyBulletService,
            TestBossBossView bossPrefab,
            TestBossBulletView bulletPrefab)
        {
            _parent = parent;
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _playerParams = playerParams ?? throw new ArgumentNullException(nameof(playerParams));
            _enemyBulletService = enemyBulletService ?? throw new ArgumentNullException(nameof(enemyBulletService));
            _bossPrefab = bossPrefab;
            _bulletPrefab = bulletPrefab;
            _usePrefabViews = _bossPrefab != null && _bulletPrefab != null;
        }

        public void Initialize()
        {
            if (_context.Player == null)
                throw new InvalidOperationException("BattleContext.Player is not initialized.");
            if (_context.Boss == null)
                throw new InvalidOperationException("BattleContext.Boss is not initialized.");

            _root = new GameObject("TestBossBattleRuntime");
            if (_parent != null)
                _root.transform.SetParent(_parent, false);

            _context.Player.InitializeStats(Mathf.Max(1, _playerParams.MaxHp));
            _playerMoveManager = new PlayerMoveManager(_context, TestBossStageMapLoader.LoadDefault());

            _playerView = CreateFallbackPlayerView();
            _bossView = CreateBossView();
            _playerView.SetHitboxVisible(true);
            _bossView.SetHitboxVisible(true);

            SyncActorViews();
            UpdatePlayerVisualState();
            _isInitialized = true;
        }

        public void TickBeforeBattleSimulation(float deltaTime)
        {
            EnsureInitialized();

            if (_playerDamageInvulnerabilityRemaining > 0f)
            {
                _playerDamageInvulnerabilityRemaining -= deltaTime;
                if (_playerDamageInvulnerabilityRemaining < 0f)
                    _playerDamageInvulnerabilityRemaining = 0f;
            }

            if (_context.Player.IsAlive())
                ApplyInput();

            _playerMoveManager.Update(deltaTime);
        }

        public void TickAfterBattleSimulation()
        {
            EnsureInitialized();

            SyncActorViews();
            SyncBulletViews();

            if (_context.Phase == BattlePhase.Combat)
            {
                ResolveBossContactDamage();
                ResolveEnemyBulletHits();
            }

            CleanupInactiveBulletViews();
            UpdatePlayerVisualState();
        }

        public void Dispose()
        {
            foreach (KeyValuePair<EnemyBullet, ITestBossEntityView> pair in _bulletViews)
                pair.Value.Dispose();

            _bulletViews.Clear();
            _playerView?.Dispose();
            _bossView?.Dispose();

            if (_root != null)
                UnityEngine.Object.Destroy(_root);
        }

        private void ApplyInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            Vector2Int inputDirection = Vector2Int.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                inputDirection += Vector2Int.up;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                inputDirection += Vector2Int.down;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                inputDirection += Vector2Int.left;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                inputDirection += Vector2Int.right;

            _playerMoveManager.SetInputDirection((inputDirection.x, inputDirection.y));

            if (keyboard.spaceKey.wasPressedThisFrame)
                _playerMoveManager.SetInputDash();
        }

        private void SyncActorViews()
        {
            _playerView.SetVisible(_context.Player.IsAlive());
            _playerView.SetPosition(_context.Player.Position);
            _bossView.SetVisible(_context.Boss.IsAlive());
            _bossView.SetPosition(_context.Boss.Position);
        }

        private void SyncBulletViews()
        {
            if (_context.EnemyBullets == null)
                return;

            for (int i = 0; i < _context.EnemyBullets.Count; i++)
            {
                EnemyBullet bullet = _context.EnemyBullets[i];
                if (bullet == null)
                    continue;

                if (!_bulletViews.TryGetValue(bullet, out ITestBossEntityView view))
                {
                    view = CreateBulletView();
                    view.SetHitboxVisible(true);
                    _bulletViews.Add(bullet, view);
                }

                view.SetVisible(!bullet.IsVanished);
                view.SetPosition(bullet.Position);
            }
        }

        private void ResolveBossContactDamage()
        {
            if (!_context.Boss.IsAlive())
                return;

            if (AreOverlapping(_context.Player.Position, TestBossConstants.PlayerHitboxRadius, _context.Boss.Position, _bossView.HitboxRadius))
                TryApplyDamage(TestBossConstants.BossContactDamage);
        }

        private void ResolveEnemyBulletHits()
        {
            if (_context.EnemyBullets == null)
                return;

            for (int i = 0; i < _context.EnemyBullets.Count; i++)
            {
                EnemyBullet bullet = _context.EnemyBullets[i];
                if (bullet == null || bullet.IsVanished)
                    continue;

                float bulletHitboxRadius = _bulletViews.TryGetValue(bullet, out ITestBossEntityView bulletView)
                    ? bulletView.HitboxRadius
                    : TestBossConstants.BulletHitboxRadius;

                if (!AreOverlapping(_context.Player.Position, TestBossConstants.PlayerHitboxRadius, bullet.Position, bulletHitboxRadius))
                    continue;

                if (!TryApplyDamage(bullet.Damage))
                    continue;

                _enemyBulletService.MarkVanished(_context, bullet);
            }
        }

        private bool TryApplyDamage(int damage)
        {
            if (damage <= 0)
                return false;
            if (!_context.Player.IsAlive())
                return false;
            if (_context.Player.IsDashing)
                return false;
            if (_playerDamageInvulnerabilityRemaining > 0f)
                return false;

            bool applied = _context.Player.ApplyDamage(damage);
            if (!applied)
                return false;

            _playerDamageInvulnerabilityRemaining = TestBossConstants.PlayerDamageInvulnerabilitySeconds;
            return true;
        }

        private void CleanupInactiveBulletViews()
        {
            if (_bulletViews.Count == 0)
                return;

            List<EnemyBullet> removeTargets = null;
            foreach (KeyValuePair<EnemyBullet, ITestBossEntityView> pair in _bulletViews)
            {
                EnemyBullet bullet = pair.Key;
                if (bullet != null && !bullet.IsVanished && _context.EnemyBullets != null && _context.EnemyBullets.Contains(bullet))
                    continue;

                removeTargets ??= new List<EnemyBullet>();
                removeTargets.Add(bullet);
            }

            if (removeTargets == null)
                return;

            for (int i = 0; i < removeTargets.Count; i++)
            {
                EnemyBullet bullet = removeTargets[i];
                if (_bulletViews.TryGetValue(bullet, out ITestBossEntityView view))
                {
                    view.Dispose();
                    _bulletViews.Remove(bullet);
                }
            }
        }

        private void UpdatePlayerVisualState()
        {
            if (!_context.Player.IsAlive())
            {
                _playerView.SetBodyColor(_playerDeadColor);
                return;
            }

            if (_context.Player.IsDashing)
            {
                _playerView.SetBodyColor(_playerDashColor);
                return;
            }

            if (_playerDamageInvulnerabilityRemaining > 0f)
            {
                _playerView.SetBodyColor(_playerHitColor);
                return;
            }

            _playerView.SetBodyColor(_playerNormalColor);
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("TestBossBattleRuntime is not initialized.");
        }

        private static bool AreOverlapping(
            NumericsVector3 aPosition,
            float aRadius,
            NumericsVector3 bPosition,
            float bRadius)
        {
            float combinedRadius = aRadius + bRadius;
            return NumericsVector3.DistanceSquared(aPosition, bPosition) <= combinedRadius * combinedRadius;
        }

        private ITestBossTintableView CreateFallbackPlayerView()
        {
            return TestBossRuntimeCircleView.Create(
                "TestBossPlayerView",
                _root.transform,
                TestBossConstants.PlayerBodyDiameter,
                TestBossConstants.PlayerHitboxRadius * 2f,
                _playerNormalColor,
                new Color(0.15f, 0.85f, 1.0f, 0.18f),
                bodySortingOrder: 20,
                hitboxSortingOrder: 10);
        }

        private ITestBossTintableView CreateBossView()
        {
            if (_usePrefabViews)
            {
                TestBossBossView bossInstance = UnityEngine.Object.Instantiate(_bossPrefab, _root.transform);
                bossInstance.name = "TestBossBossView";
                return bossInstance;
            }

            return TestBossRuntimeCircleView.Create(
                "TestBossBossView",
                _root.transform,
                TestBossConstants.BossBodyDiameter,
                TestBossConstants.BossHitboxRadius * 2f,
                new Color(1.0f, 0.25f, 0.25f, 1.0f),
                new Color(1.0f, 0.2f, 0.2f, 0.18f),
                bodySortingOrder: 20,
                hitboxSortingOrder: 10);
        }

        private ITestBossEntityView CreateBulletView()
        {
            if (_usePrefabViews)
            {
                TestBossBulletView bulletInstance = UnityEngine.Object.Instantiate(_bulletPrefab, _root.transform);
                bulletInstance.name = "TestBossEnemyBulletView";
                return bulletInstance;
            }

            return TestBossRuntimeCircleView.Create(
                "TestBossEnemyBulletView",
                _root.transform,
                TestBossConstants.BulletBodyDiameter,
                TestBossConstants.BulletHitboxRadius * 2f,
                new Color(1.0f, 0.85f, 0.2f, 1.0f),
                new Color(1.0f, 0.85f, 0.2f, 0.2f),
                bodySortingOrder: 18,
                hitboxSortingOrder: 8);
        }
    }
}
