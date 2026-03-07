using OneDayGame.Domain.Gameplay;
using OneDayGame.Domain.Policies;
using OneDayGame.Domain.Randomness;
using UnityEngine;

namespace OneDayGame.Infrastructure.Policies
{
    public sealed class DefaultSpawnPolicy : ISpawnPolicy
    {
        private const float MaxInterval = 2.2f;
        private const float MinInterval = 0.45f;

        public float GetSpawnInterval(int stage, int activeEnemyCount)
        {
            float interval = 1.6f - (Mathf.Min(stage - 1, 8) * 0.12f);
            interval -= activeEnemyCount * 0.08f;
            return Mathf.Max(MinInterval, Mathf.Min(MaxInterval, interval));
        }

        public SpawnRequest CreateEnemyRequest(int stage, IRandomService randomService, IMapPolicy mapPolicy, EnemyData enemyData)
        {
            float x = randomService.Range(mapPolicy.SpawnXMin, mapPolicy.SpawnXMax);
            float y = randomService.Range(mapPolicy.SpawnYMin, mapPolicy.SpawnYMax);
            return SpawnRequest.Enemy(x, y, enemyData);
        }
    }
}
