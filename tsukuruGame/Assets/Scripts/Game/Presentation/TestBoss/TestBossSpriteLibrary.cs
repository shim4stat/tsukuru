using UnityEngine;

namespace Game.Presentation.TestBoss
{
    internal static class TestBossSpriteLibrary
    {
        private static Sprite _circleSprite;

        public static Sprite GetCircleSprite()
        {
            if (_circleSprite == null)
                _circleSprite = CreateCircleSprite(64);

            return _circleSprite;
        }

        private static Sprite CreateCircleSprite(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            texture.name = "TestBossCircleSprite";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            float radius = (size - 1) * 0.5f;
            Vector2 center = new Vector2(radius, radius);
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(x, y);
                    bool inside = Vector2.Distance(point, center) <= radius;
                    pixels[(y * size) + x] = inside
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(255, 255, 255, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size);
        }
    }
}
