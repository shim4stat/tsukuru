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
        private readonly GameObject _itemPrefab;
        private readonly List<GameObject> _spawnedItems = new List<GameObject>();
        private bool _disposed;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="parent">生成したアイテムの親Transform</param>
        /// <param name="itemPrefab">アイテム用プレハブ。Inspectorで必ず設定すること。</param>
        public ItemPresenter(Transform parent, GameObject itemPrefab)
        {
            _parent = parent;
            _itemPrefab = itemPrefab;
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
            if (_itemPrefab == null)
                throw new InvalidOperationException("itemPrefab is not assigned. Assign the item prefab in the Inspector.");

            var go = UnityEngine.Object.Instantiate(_itemPrefab, _parent);

            var controller = go.GetComponent<ItemController>();
            if (controller == null)
                controller = go.AddComponent<ItemController>();

            controller.Initialize(item);
            return go;
        }
    }
}
