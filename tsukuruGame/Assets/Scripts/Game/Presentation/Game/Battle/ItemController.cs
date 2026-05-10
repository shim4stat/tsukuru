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

        public void Initialize(ItemInstance itemInstance)
        {
            ItemInstance = itemInstance;
            transform.position = new Vector3(itemInstance.CellX, itemInstance.CellY, 0);
        }
    }
}
