using System;
using System.Collections.Generic;
using OneDayGame.Domain.Boss;
using OneDayGame.Domain.Randomness;

namespace OneDayGame.Application.Boss
{
    public sealed class BossSkillCatalog : IBossSkillCatalog
    {
        private readonly Dictionary<int, IReadOnlyList<BossSkillDefinition>> _phaseSkills;

        public BossSkillCatalog(BossConfigDefinition config)
        {
            _phaseSkills = new Dictionary<int, IReadOnlyList<BossSkillDefinition>>();
            if (config == null || config.Phases == null)
            {
                return;
            }

            for (int i = 0; i < config.Phases.Count; i++)
            {
                var phase = config.Phases[i];
                if (phase == null)
                {
                    continue;
                }

                _phaseSkills[phase.PhaseIndex] = phase.Skills ?? Array.Empty<BossSkillDefinition>();
            }
        }

        public IReadOnlyList<BossSkillDefinition> GetSkills(int phaseIndex)
        {
            return _phaseSkills.TryGetValue(phaseIndex, out var skills)
                ? skills
                : Array.Empty<BossSkillDefinition>();
        }
    }

    public sealed class BossCooldownScheduler : ICooldownScheduler
    {
        private readonly Dictionary<string, float> _cooldowns = new Dictionary<string, float>();

        public bool IsReady(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId))
            {
                return false;
            }

            if (!_cooldowns.TryGetValue(skillId, out float remaining))
            {
                return true;
            }

            return remaining <= 0f;
        }

        public void StartCooldown(string skillId, float duration)
        {
            if (string.IsNullOrWhiteSpace(skillId))
            {
                return;
            }

            _cooldowns[skillId] = Math.Max(0f, duration);
        }

        public void Tick(float deltaTime)
        {
            if (_cooldowns.Count == 0)
            {
                return;
            }

            var keys = new List<string>(_cooldowns.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                float next = _cooldowns[key] - Math.Max(0f, deltaTime);
                _cooldowns[key] = next <= 0f ? 0f : next;
            }
        }

        public void Reset()
        {
            _cooldowns.Clear();
        }
    }

    public sealed class WeightedSkillSelector : ISkillSelector
    {
        private readonly IRandomService _random;

        public WeightedSkillSelector(IRandomService random)
        {
            _random = random;
        }

        public BossSkillDefinition SelectSkill(IReadOnlyList<BossSkillDefinition> candidates, ICooldownScheduler cooldownScheduler, int phaseIndex)
        {
            if (candidates == null || candidates.Count == 0 || cooldownScheduler == null || _random == null)
            {
                return null;
            }

            int totalWeight = 0;
            var available = new List<BossSkillDefinition>(candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
            {
                var skill = candidates[i];
                if (skill == null || string.IsNullOrWhiteSpace(skill.SkillId))
                {
                    continue;
                }

                if (!cooldownScheduler.IsReady(skill.SkillId))
                {
                    continue;
                }

                if (!IsAllowedByPhaseMask(skill, phaseIndex))
                {
                    continue;
                }

                int weight = Math.Max(1, skill.Weight);
                totalWeight += weight;
                available.Add(skill);
            }

            if (available.Count == 0 || totalWeight <= 0)
            {
                return null;
            }

            float roll = _random.Range(0f, totalWeight);
            float cumulative = 0f;
            for (int i = 0; i < available.Count; i++)
            {
                var skill = available[i];
                cumulative += Math.Max(1, skill.Weight);
                if (roll <= cumulative)
                {
                    return skill;
                }
            }

            return available[available.Count - 1];
        }

        private static bool IsAllowedByPhaseMask(BossSkillDefinition skill, int phaseIndex)
        {
            if (skill == null)
            {
                return false;
            }

            if (skill.PhaseMask < 0)
            {
                return true;
            }

            if (phaseIndex < 0 || phaseIndex >= 31)
            {
                return false;
            }

            int bit = 1 << phaseIndex;
            return (skill.PhaseMask & bit) != 0;
        }
    }

    public sealed class BossSkillExecutor : IBossSkillExecutor
    {
        public bool TryExecute(BossSkillContext context, BossSkillDefinition skill, ICooldownScheduler cooldownScheduler)
        {
            if (skill == null || cooldownScheduler == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(skill.SkillId) || !cooldownScheduler.IsReady(skill.SkillId))
            {
                return false;
            }

            var targeting = skill.Targeting;
            var targets = targeting != null ? targeting.ResolveTargets(context) : context.Targets;
            var executionContext = new BossSkillContext(
                context.Run,
                context.Caster,
                targets,
                context.DamageCalculator,
                context.ProjectileSpawner,
                context.AreaResolver,
                context.PhaseCooldownModifier);

            var effects = skill.Effects;
            if (effects != null)
            {
                for (int i = 0; i < effects.Count; i++)
                {
                    var effect = effects[i];
                    if (effect == null)
                    {
                        continue;
                    }

                    effect.Apply(executionContext);
                }
            }

            float cooldown = Math.Max(0.01f, skill.Cooldown * Math.Max(0.1f, context.PhaseCooldownModifier));
            cooldownScheduler.StartCooldown(skill.SkillId, cooldown);
            return true;
        }
    }

    public static class BossPhaseResolver
    {
        public static int ResolvePhaseIndex(BossConfigDefinition config, float hpRatio)
        {
            if (config == null || config.Phases == null || config.Phases.Count == 0)
            {
                return 0;
            }

            float ratio = hpRatio;
            if (ratio < 0f)
            {
                ratio = 0f;
            }
            else if (ratio > 1f)
            {
                ratio = 1f;
            }
            for (int i = 0; i < config.Phases.Count; i++)
            {
                var phase = config.Phases[i];
                if (phase == null)
                {
                    continue;
                }

                if (ratio >= phase.HpMinRatio && ratio <= phase.HpMaxRatio)
                {
                    return phase.PhaseIndex;
                }
            }

            return config.Phases[config.Phases.Count - 1].PhaseIndex;
        }
    }
}
