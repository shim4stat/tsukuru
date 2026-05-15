using System;
using Game.Domain.Battle;
using UnityEngine;

namespace Game.Presentation.Game.Battle
{
    /// <summary>
    /// グリッド上に配置されたアイテムのGameObjectを制御する。
    /// Collider付きオブジェクトとしてシーンに存在し、Domain側のItemInstanceと紐付く。
    /// </summary>
    public class ItemController : MonoBehaviour
    {
        public ItemInstance ItemInstance { get; private set; }

        public Action<ItemController> OnPlayerEntered { get; set; }

        public void Initialize(ItemInstance itemInstance)
        {
            ItemInstance = itemInstance;
            transform.position = new Vector3(itemInstance.CellX, itemInstance.CellY, 0);

            // Prefabのスケールに関係なく、ワールド空間で0.3x0.3のコライダーにする
            var collider = GetComponent<BoxCollider2D>();
            if (collider != null)
            {
                collider.offset = Vector2.zero;
                collider.size = new Vector2(0.3f / transform.lossyScale.x, 0.3f / transform.lossyScale.y);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() != null)
            {
                OnPlayerEntered?.Invoke(this);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() != null)
            {
                OnPlayerEntered?.Invoke(this);
            }
        }
    }
}
