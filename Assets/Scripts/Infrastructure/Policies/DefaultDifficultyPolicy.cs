using OneDayGame.Domain.Gameplay;
using OneDayGame.Domain.Policies;

namespace OneDayGame.Infrastructure.Policies
{
    public sealed class DefaultDifficultyPolicy : IDifficultyPolicy
    {
        public int GetKillsToAdvance(int stage)
        {
            return 6 + (stage - 1) * 2;
        }

        public EnemyData GetEnemyData(int stage)
        {
            float hp = 10f + (stage - 1) * 2.8f;
            float speed = 1.3f + (stage - 1) * 0.16f;
            float dmg = 8f + (stage - 1) * 2f;
            float contactRadius = 0.4f + stage * 0.02f;
            int score = 10 + stage;

            return new EnemyData(hp, speed, dmg, score, contactRadius);
        }
    }
}
