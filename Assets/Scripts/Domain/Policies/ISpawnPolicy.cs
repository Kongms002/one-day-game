using OneDayGame.Domain.Gameplay;
using OneDayGame.Domain.Randomness;

namespace OneDayGame.Domain.Policies
{
    public interface ISpawnPolicy
    {
        float GetSpawnInterval(int stage, int activeEnemyCount);

        SpawnRequest CreateEnemyRequest(int stage, IRandomService randomService, IMapPolicy mapPolicy, EnemyData enemyData);
    }
}
