using System;
using NumericsVector3 = System.Numerics.Vector3;
using UnityEngine;

namespace Game.Presentation.TestBoss
{
    internal sealed class TestBossRuntimeCircleView : ITestBossTintableView
    {
        private readonly GameObject _root;
        private readonly SpriteRenderer _bodyRenderer;
        private readonly GameObject _hitboxRoot;
        private readonly float _hitboxRadius;

        private TestBossRuntimeCircleView(
            GameObject root,
            GameObject hitboxRoot,
            SpriteRenderer bodyRenderer,
            float hitboxRadius)
        {
            _root = root;
            _hitboxRoot = hitboxRoot;
            _bodyRenderer = bodyRenderer;
            _hitboxRadius = hitboxRadius;
        }

        public float HitboxRadius => _hitboxRadius;

        public static TestBossRuntimeCircleView Create(
            string name,
            Transform parent,
            float bodyDiameter,
            float hitboxDiameter,
            Color bodyColor,
            Color hitboxColor,
            int bodySortingOrder,
            int hitboxSortingOrder)
        {
            GameObject root = new GameObject(name);
            if (parent != null)
                root.transform.SetParent(parent, false);

            Sprite circleSprite = TestBossSpriteLibrary.GetCircleSprite();
            SpriteRenderer hitboxRenderer = CreateRenderer("Hitbox", root.transform, circleSprite, hitboxDiameter, hitboxColor, hitboxSortingOrder);
            SpriteRenderer bodyRenderer = CreateRenderer("Body", root.transform, circleSprite, bodyDiameter, bodyColor, bodySortingOrder);

            return new TestBossRuntimeCircleView(root, hitboxRenderer.gameObject, bodyRenderer, hitboxDiameter * 0.5f);
        }

        public void SetPosition(NumericsVector3 position)
        {
            if (_root == null)
                return;

            _root.transform.position = new Vector3(position.X, position.Y, position.Z);
        }

        public void SetBodyColor(Color color)
        {
            if (_bodyRenderer != null)
                _bodyRenderer.color = color;
        }

        public void SetVisible(bool visible)
        {
            if (_root != null)
                _root.SetActive(visible);
        }

        public void SetHitboxVisible(bool visible)
        {
            if (_hitboxRoot != null)
                _hitboxRoot.SetActive(visible);
        }

        public void Dispose()
        {
            if (_root != null)
                UnityEngine.Object.Destroy(_root);
        }

        private static SpriteRenderer CreateRenderer(
            string childName,
            Transform parent,
            Sprite sprite,
            float diameter,
            Color color,
            int sortingOrder)
        {
            GameObject child = new GameObject(childName);
            child.transform.SetParent(parent, false);
            child.transform.localScale = new Vector3(diameter, diameter, 1f);

            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }
    }
}
