namespace OneDayGame.Domain.Randomness
{
    public interface IRandomService
    {
        float Range(float minInclusive, float maxInclusive);

        int Range(int minInclusive, int maxExclusive);

        float Value();
    }
}
