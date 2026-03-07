using OneDayGame.Domain.Policies;

namespace OneDayGame.Infrastructure.Policies
{
    public sealed class DefaultMapPolicy : IMapPolicy
    {
        public float SpawnXMin => -7.6f;

        public float SpawnXMax => 7.6f;

        public float SpawnYMin => 5.2f;

        public float SpawnYMax => 7.5f;

        public float PlayerMinX => -7.8f;

        public float PlayerMaxX => 7.8f;

        public float PlayerMinY => -4.8f;

        public float PlayerMaxY => 4.8f;
    }
}
