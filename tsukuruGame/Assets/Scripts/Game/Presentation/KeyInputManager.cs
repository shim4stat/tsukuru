using Game.Domain.Battle;
using UnityEngine;

namespace Game.Presentation
{
    public class KeyInputManager : MonoBehaviour
    {
        private Player player;
        private Robot robot;

        public void Initialize(Player player, Robot robot)
        {
            this.player = player;
            this.robot = robot;
        }

        void Update()
        {
            if (player == null) return;

            Vector2Int inputDir = Vector2Int.zero;

            if (Input.GetKey(KeyCode.W)) inputDir += Vector2Int.up;
            if (Input.GetKey(KeyCode.S)) inputDir += Vector2Int.down;
            if (Input.GetKey(KeyCode.A)) inputDir += Vector2Int.left;
            if (Input.GetKey(KeyCode.D)) inputDir += Vector2Int.right;

            player.Move(new System.Numerics.Vector2(inputDir.x, inputDir.y), robot, Time.deltaTime);

            if (Input.GetKeyDown(KeyCode.Space))
            {
                player.InputDash();
            }
        }
    }
}
