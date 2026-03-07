using System;

namespace OneDayGame.Domain.Input
{
    public interface IInputPort
    {
        InputAxis MoveAxis { get; }

        bool UltimatePressed { get; }

        bool AnyActionPressed { get; }

        event Action<VectorInputTick> FrameTick;
    }

    public readonly struct VectorInputTick
    {
        public InputAxis MoveAxis { get; }
        public bool UltimatePressed { get; }
        public bool AnyActionPressed { get; }

        public VectorInputTick(InputAxis moveAxis, bool ultimatePressed, bool anyActionPressed)
        {
            MoveAxis = moveAxis;
            UltimatePressed = ultimatePressed;
            AnyActionPressed = anyActionPressed;
        }
    }
}
