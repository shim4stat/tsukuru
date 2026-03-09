using NumericsVector3 = System.Numerics.Vector3;
using UnityEngine;

namespace Game.Presentation.TestBoss
{
    public sealed class TestBossBulletView : MonoBehaviour, ITestBossEntityView
    {
        [SerializeField] private GameObject hitboxRoot;
        [SerializeField] private float hitboxRadius = TestBossConstants.BulletHitboxRadius;

        public float HitboxRadius => hitboxRadius;

        public void SetPosition(NumericsVector3 position)
        {
            transform.position = new Vector3(position.X, position.Y, position.Z);
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void SetHitboxVisible(bool visible)
        {
            if (hitboxRoot != null)
                hitboxRoot.SetActive(visible);
        }

        public void Dispose()
        {
            if (this != null)
                Destroy(gameObject);
        }
    }
}
