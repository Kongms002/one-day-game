using OneDayGame.Domain.Randomness;
using UnityEngine;

namespace OneDayGame.Infrastructure.Services
{
    public sealed class UnityRandomService : IRandomService
    {
        public float Range(float minInclusive, float maxInclusive)
        {
            return Random.Range(minInclusive, maxInclusive);
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            return Random.Range(minInclusive, maxExclusive);
        }

        public float Value()
        {
            return Random.value;
        }
    }
}
