using System;
using System.Collections.Generic;
using NumericsVector3 = System.Numerics.Vector3;
using Game.Contracts.MasterData.Models;
using Game.Domain.Battle;
using Game.Presentation.TestBoss;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Presentation.Game
{
    internal sealed class BossBattleRuntime : IDisposable
    {
        private readonly Transform _parent;
        private readonly BattleContext _context;
        private readonly PlayerParamsContract _playerParams;
        private readonly EnemyBulletService _enemyBulletService;
        private readonly TestBossBossView _bossPrefab;
        private readonly TestBossBulletView _bulletPrefab;
        private readonly bool _usePrefabViews;

        private readonly Color _playerNormalColor = new Color(0.15f, 0.85f, 1.0f, 1.0f);
        private readonly Color _playerDashColor = new Color(0.35f, 1.0f, 0.45f, 1.0f);
        private readonly Color _playerHitColor = new Color(1.0f, 0.95f, 0.35f, 1.0f);
        private readonly Color _playerDeadColor = new Color(0.4f, 0.4f, 0.4f, 1.0f);

        private GameObject _root;
        private PlayerMoveManager _playerMoveManager;
        private IBossBattleTintableView _playerView;
        private IBossBattleTintableView _bossView;
        private BulletManager _bulletManager;
        private float _playerDamageInvulnerabilityRemaining;
        private bool _isInitialized;

        public BossBattleRuntime(
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

            _root = new GameObject("BossBattleRuntime");
            if (_parent != null)
                _root.transform.SetParent(_parent, false);

            _context.Player.InitializeStats(Mathf.Max(1, _playerParams.MaxHp));
            _playerMoveManager = new PlayerMoveManager(_context, TestBossStageMapLoader.LoadDefault());

            _playerView = CreateFallbackPlayerView();
            _bossView = CreateBossView();
            _bulletManager = new BulletManager(_root.transform, _bulletPrefab, _usePrefabViews);

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
            _bulletManager.Sync(_context.EnemyBullets);

            if (_context.Phase == BattlePhase.Combat)
            {
                ResolveBossContactDamage();
                ResolveEnemyBulletHits();
                _bulletManager.Sync(_context.EnemyBullets);
            }

            UpdatePlayerVisualState();
        }

        public void Dispose()
        {
            _bulletManager?.Dispose();
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

        private void ResolveBossContactDamage()
        {
            if (!_context.Boss.IsAlive())
                return;

            if (AreOverlapping(
                    _context.Player.Position,
                    BossBattleRuntimeConstants.PlayerHitboxRadius,
                    _context.Boss.Position,
                    _bossView.HitboxRadius))
            {
                TryApplyDamage(BossBattleRuntimeConstants.BossContactDamage);
            }
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

                if (!_bulletManager.TryGetHitboxRadius(bullet.RuntimeId, out float bulletHitboxRadius))
                    bulletHitboxRadius = BossBattleRuntimeConstants.BulletHitboxRadius;

                if (!AreOverlapping(
                        _context.Player.Position,
                        BossBattleRuntimeConstants.PlayerHitboxRadius,
                        bullet.Position,
                        bulletHitboxRadius))
                {
                    continue;
                }

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

            _playerDamageInvulnerabilityRemaining = BossBattleRuntimeConstants.PlayerDamageInvulnerabilitySeconds;
            return true;
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
                throw new InvalidOperationException("BossBattleRuntime is not initialized.");
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

        private IBossBattleTintableView CreateFallbackPlayerView()
        {
            return TestBossRuntimeCircleView.Create(
                "BossBattlePlayerView",
                _root.transform,
                BossBattleRuntimeConstants.PlayerBodyDiameter,
                BossBattleRuntimeConstants.PlayerHitboxRadius * 2f,
                _playerNormalColor,
                new Color(0.15f, 0.85f, 1.0f, 0.18f),
                bodySortingOrder: 20,
                hitboxSortingOrder: 10);
        }

        private IBossBattleTintableView CreateBossView()
        {
            if (_usePrefabViews)
            {
                TestBossBossView bossInstance = UnityEngine.Object.Instantiate(_bossPrefab, _root.transform);
                bossInstance.name = "BossBattleBossView";
                return bossInstance;
            }

            return TestBossRuntimeCircleView.Create(
                "BossBattleBossView",
                _root.transform,
                BossBattleRuntimeConstants.BossBodyDiameter,
                BossBattleRuntimeConstants.BossHitboxRadius * 2f,
                new Color(1.0f, 0.25f, 0.25f, 1.0f),
                new Color(1.0f, 0.2f, 0.2f, 0.18f),
                bodySortingOrder: 20,
                hitboxSortingOrder: 10);
        }

        private sealed class BulletManager : IDisposable
        {
            private readonly Transform _parent;
            private readonly TestBossBulletView _bulletPrefab;
            private readonly bool _usePrefabViews;
            private readonly Dictionary<int, IBossBattleEntityView> _viewsByRuntimeId =
                new Dictionary<int, IBossBattleEntityView>();

            public BulletManager(
                Transform parent,
                TestBossBulletView bulletPrefab,
                bool usePrefabViews)
            {
                _parent = parent;
                _bulletPrefab = bulletPrefab;
                _usePrefabViews = usePrefabViews;
            }

            public void Sync(IReadOnlyList<EnemyBullet> bullets)
            {
                HashSet<int> activeRuntimeIds = new HashSet<int>();
                if (bullets != null)
                {
                    for (int i = 0; i < bullets.Count; i++)
                    {
                        EnemyBullet bullet = bullets[i];
                        if (bullet == null || bullet.IsVanished)
                            continue;

                        activeRuntimeIds.Add(bullet.RuntimeId);
                        if (!_viewsByRuntimeId.TryGetValue(bullet.RuntimeId, out IBossBattleEntityView view))
                        {
                            view = CreateView();
                            view.SetHitboxVisible(true);
                            _viewsByRuntimeId.Add(bullet.RuntimeId, view);
                        }

                        view.SetVisible(true);
                        view.SetPosition(bullet.Position);
                    }
                }

                if (_viewsByRuntimeId.Count == 0)
                    return;

                List<int> removeTargets = null;
                foreach (KeyValuePair<int, IBossBattleEntityView> pair in _viewsByRuntimeId)
                {
                    if (activeRuntimeIds.Contains(pair.Key))
                        continue;

                    removeTargets ??= new List<int>();
                    removeTargets.Add(pair.Key);
                }

                if (removeTargets == null)
                    return;

                for (int i = 0; i < removeTargets.Count; i++)
                {
                    int runtimeId = removeTargets[i];
                    if (_viewsByRuntimeId.TryGetValue(runtimeId, out IBossBattleEntityView view))
                    {
                        view.Dispose();
                        _viewsByRuntimeId.Remove(runtimeId);
                    }
                }
            }

            public bool TryGetHitboxRadius(int runtimeId, out float radius)
            {
                if (_viewsByRuntimeId.TryGetValue(runtimeId, out IBossBattleEntityView view))
                {
                    radius = view.HitboxRadius;
                    return true;
                }

                radius = 0f;
                return false;
            }

            public void Dispose()
            {
                foreach (KeyValuePair<int, IBossBattleEntityView> pair in _viewsByRuntimeId)
                    pair.Value.Dispose();

                _viewsByRuntimeId.Clear();
            }

            private IBossBattleEntityView CreateView()
            {
                if (_usePrefabViews)
                {
                    TestBossBulletView bulletInstance = UnityEngine.Object.Instantiate(_bulletPrefab, _parent);
                    bulletInstance.name = "BossBattleEnemyBulletView";
                    return bulletInstance;
                }

                return TestBossRuntimeCircleView.Create(
                    "BossBattleEnemyBulletView",
                    _parent,
                    BossBattleRuntimeConstants.BulletBodyDiameter,
                    BossBattleRuntimeConstants.BulletHitboxRadius * 2f,
                    new Color(1.0f, 0.85f, 0.2f, 1.0f),
                    new Color(1.0f, 0.85f, 0.2f, 0.2f),
                    bodySortingOrder: 18,
                    hitboxSortingOrder: 8);
            }
        }
    }
}
