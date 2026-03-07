using System;

namespace OneDayGame.Domain.Input
{
    [Serializable]
    public readonly struct InputAxis
    {
        public float X { get; }
        public float Y { get; }

        public InputAxis(float x, float y)
        {
            X = x;
            Y = y;
        }

        public bool IsZero => Math.Abs(X) < float.Epsilon && Math.Abs(Y) < float.Epsilon;

        public float MagnitudeSq => X * X + Y * Y;

        public static InputAxis Zero => new InputAxis(0f, 0f);
    }
}
