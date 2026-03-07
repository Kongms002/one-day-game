using OneDayGame.Domain.Randomness;

namespace OneDayGame.Domain.Policies
{
    public interface IItemPolicy
    {
        float GetMedKitSpawnInterval(int stage);

        bool ShouldSpawnMedKit(int stage, float elapsedSinceLastSpawn, IRandomService randomService);

        float GetMedKitHealAmount(int stage);

        int GetMedKitScore(int stage);
    }
}
