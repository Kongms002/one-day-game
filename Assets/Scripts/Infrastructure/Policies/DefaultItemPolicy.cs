using OneDayGame.Domain.Policies;
using OneDayGame.Domain.Randomness;
using UnityEngine;

namespace OneDayGame.Infrastructure.Policies
{
    public sealed class DefaultItemPolicy : IItemPolicy
    {
        public float GetMedKitSpawnInterval(int stage)
        {
            return 6.5f - Mathf.Min(stage - 1, 5) * 0.35f;
        }

        public bool ShouldSpawnMedKit(int stage, float elapsedSinceLastSpawn, IRandomService randomService)
        {
            float chance = Mathf.Min(0.55f, 0.08f + stage * 0.04f);
            return randomService.Value() < chance;
        }

        public float GetMedKitHealAmount(int stage)
        {
            return 10f + (stage - 1) * 2f;
        }

        public int GetMedKitScore(int stage)
        {
            return 5;
        }
    }
}
