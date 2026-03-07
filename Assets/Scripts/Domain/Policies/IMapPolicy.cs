namespace OneDayGame.Domain.Policies
{
    public interface IMapPolicy
    {
        float SpawnXMin { get; }

        float SpawnXMax { get; }

        float SpawnYMin { get; }

        float SpawnYMax { get; }

        float PlayerMinX { get; }

        float PlayerMaxX { get; }

        float PlayerMinY { get; }

        float PlayerMaxY { get; }
    }
}
