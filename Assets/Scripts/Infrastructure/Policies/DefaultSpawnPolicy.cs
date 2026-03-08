using OneDayGame.Domain.Gameplay;
using OneDayGame.Domain.Policies;
using OneDayGame.Domain.Randomness;
using OneDayGame.Domain;
using UnityEngine;

namespace OneDayGame.Infrastructure.Policies
{
    public sealed class DefaultSpawnPolicy : ISpawnPolicy
    {
        private readonly IStageProfileProvider _stageProfileProvider;

        public DefaultSpawnPolicy()
            : this((IStageProfileProvider) null)
        {
        }

        public DefaultSpawnPolicy(IStageProfileProvider stageProfileProvider)
        {
            _stageProfileProvider = stageProfileProvider ?? new DefaultStageProfileProvider();
        }

        public float GetSpawnInterval(int stage, int activeEnemyCount)
        {
            var profile = ResolveProfile(stage);
            float relativeStage = Mathf.Max(1, stage) - 1;
            float interval = profile.SpawnIntervalBase - relativeStage * profile.SpawnIntervalPerStage;
            interval -= activeEnemyCount * profile.SpawnIntervalPerActiveEnemy;
            return Mathf.Max(profile.SpawnIntervalMin, Mathf.Min(profile.SpawnIntervalMax, interval));
        }

        public SpawnRequest CreateEnemyRequest(int stage, IRandomService randomService, IMapPolicy mapPolicy, EnemyData enemyData)
        {
            float x = randomService.Range(mapPolicy.SpawnXMin, mapPolicy.SpawnXMax);
            float y = randomService.Range(mapPolicy.SpawnYMin, mapPolicy.SpawnYMax);
            return SpawnRequest.Enemy(x, y, enemyData);
        }

        private StageProfile ResolveProfile(int stage)
        {
            return _stageProfileProvider.ResolveProfile(Mathf.Max(1, stage));
        }
    }
}
