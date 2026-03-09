using System;
using NumericsVector3 = System.Numerics.Vector3;
using UnityEngine;

namespace Game.Presentation.TestBoss
{
    public sealed class TestBossBossView : MonoBehaviour, ITestBossTintableView
    {
        [SerializeField] private Transform bodyRoot;
        [SerializeField] private GameObject hitboxRoot;
        [SerializeField] private Transform muzzleRoot;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer[] tintTargets = Array.Empty<SpriteRenderer>();
        [SerializeField] private float hitboxRadius = TestBossConstants.BossHitboxRadius;

        public float HitboxRadius => hitboxRadius;
        public Animator Animator => animator;

        public Vector3 GetMuzzlePosition()
        {
            return muzzleRoot != null ? muzzleRoot.position : transform.position;
        }

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

        public void SetBodyColor(Color color)
        {
            SpriteRenderer[] targets = tintTargets;
            if ((targets == null || targets.Length == 0) && bodyRoot != null)
                targets = bodyRoot.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);

            if (targets == null)
                return;

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null)
                    targets[i].color = color;
            }
        }

        public void Dispose()
        {
            if (this != null)
                Destroy(gameObject);
        }
    }
}
