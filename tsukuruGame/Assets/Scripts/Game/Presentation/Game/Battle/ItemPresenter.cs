using System;
using System.Collections.Generic;
using Game.Domain.Battle;
using UnityEngine;

namespace Game.Presentation.Game.Battle
{
    /// <summary>
    /// BattleContext.Itemsに基づき、Collider付きアイテムGameObjectを生成・管理する。
    /// </summary>
    public sealed class ItemPresenter : IDisposable
    {
        private readonly Transform _parent;
        private readonly Dictionary<ItemType, GameObject> _prefabMap;
        private readonly List<GameObject> _spawnedItems = new List<GameObject>();
        private readonly EnergyPickupService _energyPickupService;
        private readonly BattleContext _battleContext;
        private bool _disposed;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="parent">生成したアイテムの親Transform</param>
        /// <param name="prefabMap">ItemTypeごとのプレハブマップ</param>
        /// <param name="energyPickupService">エネルギー取得ドメインサービス</param>
        /// <param name="battleContext">バトルコンテキスト</param>
        public ItemPresenter(Transform parent, Dictionary<ItemType, GameObject> prefabMap, EnergyPickupService energyPickupService, BattleContext battleContext)
        {
            _parent = parent;
            _prefabMap = prefabMap;
            _energyPickupService = energyPickupService;
            _battleContext = battleContext;
        }

        /// <summary>
        /// Domainのアイテムリストからグリッド上にCollider付きGameObjectを配置する。
        /// </summary>
        public void SpawnItems(IReadOnlyList<ItemInstance> items)
        {
            ClearItems();

            if (items == null || items.Count == 0)
                return;

            foreach (var item in items)
            {
                var go = CreateItemGameObject(item);
                _spawnedItems.Add(go);
            }
        }

        public void ClearItems()
        {
            foreach (var go in _spawnedItems)
            {
                if (go != null)
                    UnityEngine.Object.Destroy(go);
            }
            _spawnedItems.Clear();
        }

        public void Dispose()
        {
            if (_disposed) return;
            ClearItems();
            _disposed = true;
        }

        private GameObject CreateItemGameObject(ItemInstance item)
        {
            if (!_prefabMap.TryGetValue(item.ItemType, out var prefab) || prefab == null)
                throw new InvalidOperationException($"Prefab for ItemType '{item.ItemType}' is not assigned. Assign the prefab in the Inspector.");

            var go = UnityEngine.Object.Instantiate(prefab, _parent);

            var controller = go.GetComponent<ItemController>();
            if (controller == null)
                controller = go.AddComponent<ItemController>();

            controller.Initialize(item);
            controller.OnPlayerEntered = HandlePlayerEntered;
            return go;
        }

        private void HandlePlayerEntered(ItemController controller)
        {
            if (_energyPickupService.TryPickup(_battleContext, controller.ItemInstance))
            {
                _spawnedItems.Remove(controller.gameObject);
                UnityEngine.Object.Destroy(controller.gameObject);
            }
        }
    }
}
