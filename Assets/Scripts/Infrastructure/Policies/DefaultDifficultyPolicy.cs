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
                    profile.EnemyContactRadius,
                    ResolveArchetype(profile.EnemyType, stage));
            }

            float hp = 10f + (stage - 1) * 2.8f;
            float speed = 1.3f + (stage - 1) * 0.16f;
            float dmg = 8f + (stage - 1) * 2f;
            float contactRadius = 0.4f + stage * 0.02f;
            int score = 10 + stage;

            return new EnemyData(hp, speed, dmg, score, contactRadius, ResolveArchetype(null, stage));
        }

        private static EnemyArchetype ResolveArchetype(string enemyType, int stage)
        {
            if (!string.IsNullOrWhiteSpace(enemyType))
            {
                string normalized = enemyType.Trim().ToLowerInvariant();
                if (normalized.Contains("tank") || normalized.Contains("heavy"))
                {
                    return EnemyArchetype.Tank;
                }

                if (normalized.Contains("swift") || normalized.Contains("fast"))
                {
                    return EnemyArchetype.Swift;
                }

                if (normalized.Contains("berserk") || normalized.Contains("rage"))
                {
                    return EnemyArchetype.Berserker;
                }

                if (normalized.Contains("self") || normalized.Contains("explode") || normalized.Contains("bomb"))
                {
                    return EnemyArchetype.SelfDestruct;
                }

                if (normalized.Contains("multiply") || normalized.Contains("split"))
                {
                    return EnemyArchetype.Multiply;
                }

                if (normalized.Contains("swarm") || normalized.Contains("pack"))
                {
                    return EnemyArchetype.Swarm;
                }
            }

            int mod = Math.Max(1, stage) % 6;
            if (mod == 0)
            {
                return EnemyArchetype.Berserker;
            }

            if (mod == 3)
            {
                return EnemyArchetype.Tank;
            }

            if (mod == 5)
            {
                return EnemyArchetype.Swift;
            }

            if (mod == 2)
            {
                return EnemyArchetype.Swarm;
            }

            if (mod == 4)
            {
                return EnemyArchetype.Multiply;
            }

            return EnemyArchetype.Normal;
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
