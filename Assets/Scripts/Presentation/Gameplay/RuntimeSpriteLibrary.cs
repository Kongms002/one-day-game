using UnityEngine;

namespace OneDayGame.Presentation.Gameplay
{
    public static class RuntimeSpriteLibrary
    {
        private static Sprite s_square;
        private static Sprite s_circle;
        private static Sprite s_diamond;
        private static Sprite s_plus;

        public static Sprite GetSquare()
        {
            if (s_square == null)
            {
                s_square = CreateSprite(16, (x, y, c) => true);
            }

            return s_square;
        }

        public static Sprite GetCircle()
        {
            if (s_circle == null)
            {
                s_circle = CreateSprite(24, (x, y, c) =>
                {
                    float dx = x - c;
                    float dy = y - c;
                    float radius = c - 1f;
                    return (dx * dx) + (dy * dy) <= radius * radius;
                });
            }

            return s_circle;
        }

        public static Sprite GetDiamond()
        {
            if (s_diamond == null)
            {
                s_diamond = CreateSprite(24, (x, y, c) => Mathf.Abs(x - c) + Mathf.Abs(y - c) <= c - 1f);
            }

            return s_diamond;
        }

        public static Sprite GetPlus()
        {
            if (s_plus == null)
            {
                s_plus = CreateSprite(24, (x, y, c) =>
                {
                    float arm = 3f;
                    bool vertical = Mathf.Abs(x - c) <= arm;
                    bool horizontal = Mathf.Abs(y - c) <= arm;
                    bool core = Mathf.Abs(x - c) <= arm + 1f && Mathf.Abs(y - c) <= arm + 1f;
                    return (vertical && Mathf.Abs(y - c) <= c - 2f) || (horizontal && Mathf.Abs(x - c) <= c - 2f) || core;
                });
            }

            return s_plus;
        }

        private static Sprite CreateSprite(int size, System.Func<float, float, float, bool> isFilled)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            float center = (size - 1) * 0.5f;
            var clear = new Color(0f, 0f, 0f, 0f);
            var white = Color.white;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, isFilled(x, y, center) ? white : clear);
                }
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
