using OneDayGame.Domain.Gameplay;
using OneDayGame.Domain.Policies;
using UnityEngine;

namespace OneDayGame.Application.Boss
{
    public sealed class BossSpawnPolicy
    {
        public bool TryCreateBossSpawn(
            IRunState runState,
            IDifficultyPolicy difficultyPolicy,
            IMapPolicy mapPolicy,
            IStageProfileProvider stageProfileProvider,
            out SpawnRequest request)
        {
            request = default;
            if (runState == null || difficultyPolicy == null || mapPolicy == null || stageProfileProvider == null)
            {
                return false;
            }

            int stage = runState.Stage;
            if (stage <= 0)
            {
                return false;
            }

            var profile = stageProfileProvider.ResolveProfile(stage);
            if (profile == null)
            {
                return false;
            }

            bool shouldSpawnBoss = profile.IsBossStage(stage);
            if (!shouldSpawnBoss)
            {
                shouldSpawnBoss = stage % 10 == 0;
            }

            if (!shouldSpawnBoss)
            {
                return false;
            }

            var baseData = difficultyPolicy.GetEnemyData(stage);
            var bossData = new EnemyData(
                baseData.MaxHp * profile.BossHpMultiplier,
                baseData.MoveSpeed * profile.BossSpeedMultiplier,
                baseData.ContactDamage * profile.BossDamageMultiplier,
                Mathf.Max(1, baseData.ScoreValue * profile.BossScoreMultiplier),
                baseData.ContactRadius * profile.BossContactRadiusMultiplier,
                EnemyArchetype.Tank,
                true);

            float x = (mapPolicy.PlayerMinX + mapPolicy.PlayerMaxX) * 0.5f;
            float y = mapPolicy.PlayerMaxY - 0.8f;
            request = SpawnRequest.Enemy(x, y, bossData);
            return true;
        }
    }
}
