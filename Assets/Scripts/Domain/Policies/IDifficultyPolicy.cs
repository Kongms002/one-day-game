using OneDayGame.Domain.Gameplay;

namespace OneDayGame.Domain.Policies
{
    public interface IDifficultyPolicy
    {
        int GetKillsToAdvance(int stage);

        EnemyData GetEnemyData(int stage);
    }
}
