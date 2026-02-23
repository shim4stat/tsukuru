using Game.Domain.Battle;
using UnityEngine;

namespace Game.Presentation
{
    public class KeyInputManager : MonoBehaviour
    {
        private PlayerMoveManager playerMoveManager;

        public void Initialize(PlayerMoveManager playermovemanager)
        {
            playerMoveManager = playermovemanager;
        }

        void Update()
        {
            UnityEngine.Vector2Int inputDir = UnityEngine.Vector2Int.zero;

            if (Input.GetKey(KeyCode.W)) inputDir += UnityEngine.Vector2Int.up;
            if (Input.GetKey(KeyCode.S)) inputDir += UnityEngine.Vector2Int.down;
            if (Input.GetKey(KeyCode.A)) inputDir += UnityEngine.Vector2Int.left;
            if (Input.GetKey(KeyCode.D)) inputDir += UnityEngine.Vector2Int.right;

            // Debug.Log($"Input Direction: {inputDir}");

            playerMoveManager.SetInputDirection((inputDir.x, inputDir.y));

            if (Input.GetKeyDown(KeyCode.Space))
            {
                playerMoveManager.SetInputDash();
            }
        }
    }
}
