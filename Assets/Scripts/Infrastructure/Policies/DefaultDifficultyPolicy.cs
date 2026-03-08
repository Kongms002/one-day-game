using System;
using OneDayGame.Domain.Gameplay;
using OneDayGame.Domain.Policies;

namespace OneDayGame.Infrastructure.Policies
{
    public sealed class DefaultDifficultyPolicy : IDifficultyPolicy
    {
        private readonly IStageProfileProvider _stageProfileProvider;

        public DefaultDifficultyPolicy() : this((IStageProfileProvider) null)
        {
        }

        public DefaultDifficultyPolicy(IStageProfileProvider stageProfileProvider)
        {
            _stageProfileProvider = stageProfileProvider ?? new DefaultStageProfileProvider();
        }

        public int GetKillsToAdvance(int stage)
        {
            var profile = ResolveProfile(stage);
            if (profile != null)
            {
                return profile.KillsToAdvance;
            }

            return 6 + (stage - 1) * 2;
        }

        public EnemyData GetEnemyData(int stage)
        {
            var profile = ResolveProfile(stage);
            if (profile != null)
            {
                return new EnemyData(
                    profile.EnemyMaxHp,
                    profile.EnemyMoveSpeed,
                    profile.EnemyContactDamage,
                    profile.EnemyScoreValue,
                    profile.EnemyContactRadius);
            }

            float hp = 10f + (stage - 1) * 2.8f;
            float speed = 1.3f + (stage - 1) * 0.16f;
            float dmg = 8f + (stage - 1) * 2f;
            float contactRadius = 0.4f + stage * 0.02f;
            int score = 10 + stage;

            return new EnemyData(hp, speed, dmg, score, contactRadius);
        }

        private StageProfile ResolveProfile(int stage)
        {
            if (_stageProfileProvider == null)
            {
                return null;
            }

            return _stageProfileProvider.ResolveProfile(Math.Max(1, stage));
        }
    }
}
