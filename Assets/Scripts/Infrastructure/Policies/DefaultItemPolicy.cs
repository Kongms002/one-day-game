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

        public float GetMagnetSpawnChance(int stage)
        {
            return Mathf.Min(0.32f, 0.06f + stage * 0.018f);
        }

        public float GetMagnetDuration(int stage)
        {
            return 4.5f + Mathf.Min(4f, stage * 0.18f);
        }

        public float GetMagnetRadius(int stage)
        {
            return 2.6f + Mathf.Min(3f, stage * 0.15f);
        }
    }
}
