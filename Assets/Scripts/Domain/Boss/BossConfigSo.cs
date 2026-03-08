using System;
using System.Collections.Generic;
using UnityEngine;

namespace OneDayGame.Domain.Boss
{
    [CreateAssetMenu(fileName = "BossConfig", menuName = "OneDayGame/Boss/Boss Config")]
    public sealed class BossConfigSO : ScriptableObject
    {
        [SerializeField]
        private string _bossId = "boss_default";

        [SerializeField]
        [Min(1f)]
        private float _baseHp = 100f;

        [SerializeField]
        [Min(0.1f)]
        private float _baseMoveSpeed = 1f;

        [SerializeField]
        private BossPhaseSO[] _phases = Array.Empty<BossPhaseSO>();

        public BossConfigDefinition BuildDefinition()
        {
            var phases = new List<BossPhaseDefinition>();
            for (int i = 0; i < _phases.Length; i++)
            {
                var phase = _phases[i];
                if (phase == null)
                {
                    continue;
                }

                phases.Add(phase.BuildDefinition());
            }

            phases.Sort((a, b) => a.PhaseIndex.CompareTo(b.PhaseIndex));

            return new BossConfigDefinition
            {
                BossId = string.IsNullOrWhiteSpace(_bossId) ? name : _bossId,
                BaseHp = Mathf.Max(1f, _baseHp),
                BaseMoveSpeed = Mathf.Max(0.1f, _baseMoveSpeed),
                Phases = phases
            };
        }
    }

    [CreateAssetMenu(fileName = "BossPhase", menuName = "OneDayGame/Boss/Boss Phase")]
    public sealed class BossPhaseSO : ScriptableObject
    {
        [SerializeField]
        [Min(0)]
        private int _phaseIndex;

        [SerializeField]
        [Range(0f, 1f)]
        private float _hpMinRatio;

        [SerializeField]
        [Range(0f, 1f)]
        private float _hpMaxRatio = 1f;

        [SerializeField]
        [Min(0.1f)]
        private float _cooldownModifier = 1f;

        [SerializeField]
        private BossSkillSO[] _skills = Array.Empty<BossSkillSO>();

        public BossPhaseDefinition BuildDefinition()
        {
            var skills = new List<BossSkillDefinition>();
            for (int i = 0; i < _skills.Length; i++)
            {
                var skill = _skills[i];
                if (skill == null)
                {
                    continue;
                }

                skills.Add(skill.BuildDefinition());
            }

            float min = Mathf.Clamp01(_hpMinRatio);
            float max = Mathf.Clamp01(_hpMaxRatio);
            if (max < min)
            {
                float swap = min;
                min = max;
                max = swap;
            }

            return new BossPhaseDefinition
            {
                PhaseIndex = Mathf.Max(0, _phaseIndex),
                HpMinRatio = min,
                HpMaxRatio = max,
                CooldownModifier = Mathf.Max(0.1f, _cooldownModifier),
                Skills = skills
            };
        }
    }

    [CreateAssetMenu(fileName = "BossSkill", menuName = "OneDayGame/Boss/Boss Skill")]
    public sealed class BossSkillSO : ScriptableObject
    {
        [SerializeField]
        private string _skillId = "skill_default";

        [SerializeField]
        [Min(0.01f)]
        private float _cooldown = 2f;

        [SerializeField]
        [Min(1)]
        private int _weight = 10;

        [SerializeField]
        [Min(0f)]
        private float _castTime;

        [SerializeField]
        private BossSkillExecutionOrder _executionOrder = BossSkillExecutionOrder.Sequential;

        [SerializeField]
        private int _phaseMask = -1;

        [SerializeField]
        private TargetingStrategySO _targeting;

        [SerializeField]
        private SkillEffectSO[] _effects = Array.Empty<SkillEffectSO>();

        public BossSkillDefinition BuildDefinition()
        {
            ITargetingStrategy targeting = _targeting != null
                ? _targeting.Build()
                : new CurrentTargetsStrategy();

            var effects = new List<ISkillEffect>();
            for (int i = 0; i < _effects.Length; i++)
            {
                var effect = _effects[i];
                if (effect == null)
                {
                    continue;
                }

                effects.Add(effect.Build());
            }

            return new BossSkillDefinition
            {
                SkillId = string.IsNullOrWhiteSpace(_skillId) ? name : _skillId,
                Cooldown = Mathf.Max(0.01f, _cooldown),
                Weight = Mathf.Max(1, _weight),
                CastTime = Mathf.Max(0f, _castTime),
                ExecutionOrder = _executionOrder,
                PhaseMask = _phaseMask,
                Targeting = targeting,
                Effects = effects
            };
        }
    }

    public abstract class TargetingStrategySO : ScriptableObject
    {
        public abstract ITargetingStrategy Build();
    }

    [CreateAssetMenu(fileName = "Targeting_Current", menuName = "OneDayGame/Boss/Targeting/Current Targets")]
    public sealed class CurrentTargetsTargetingSO : TargetingStrategySO
    {
        public override ITargetingStrategy Build()
        {
            return new CurrentTargetsStrategy();
        }
    }

    [CreateAssetMenu(fileName = "Targeting_Nearest", menuName = "OneDayGame/Boss/Targeting/Nearest N")]
    public sealed class NearestNTargetingSO : TargetingStrategySO
    {
        [SerializeField]
        [Min(1)]
        private int _count = 1;

        public override ITargetingStrategy Build()
        {
            return new NearestNTargetingStrategy(Mathf.Max(1, _count));
        }
    }

    public abstract class SkillEffectSO : ScriptableObject
    {
        public abstract ISkillEffect Build();
    }

    [CreateAssetMenu(fileName = "Effect_Damage", menuName = "OneDayGame/Boss/Effect/Damage")]
    public sealed class DamageEffectSO : SkillEffectSO
    {
        [SerializeField]
        [Min(0f)]
        private float _amount = 10f;

        [SerializeField]
        [Range(0f, 1f)]
        private float _criticalChance;

        public override ISkillEffect Build()
        {
            return new DamageSkillEffect(new BaseDamage(_amount, _criticalChance > 0f, _criticalChance));
        }
    }

    [CreateAssetMenu(fileName = "Effect_Projectile", menuName = "OneDayGame/Boss/Effect/Projectile")]
    public sealed class ProjectileEffectSO : SkillEffectSO
    {
        [SerializeField]
        private string _projectileId = "boss_projectile";

        [SerializeField]
        [Min(1)]
        private int _count = 1;

        [SerializeField]
        [Min(0f)]
        private float _speed = 8f;

        [SerializeField]
        [Min(0.01f)]
        private float _lifetime = 2f;

        public override ISkillEffect Build()
        {
            return new ProjectileSkillEffect(
                string.IsNullOrWhiteSpace(_projectileId) ? "boss_projectile" : _projectileId,
                Mathf.Max(1, _count),
                Mathf.Max(0f, _speed),
                Mathf.Max(0.01f, _lifetime));
        }
    }

    [CreateAssetMenu(fileName = "Effect_AreaDamage", menuName = "OneDayGame/Boss/Effect/Area Damage")]
    public sealed class AreaDamageEffectSO : SkillEffectSO
    {
        [SerializeField]
        [Min(0.1f)]
        private float _radius = 2f;

        [SerializeField]
        [Min(1)]
        private int _maxTargets = 8;

        [SerializeField]
        [Min(0f)]
        private float _amount = 12f;

        public override ISkillEffect Build()
        {
            return new AreaDamageSkillEffect(_radius, _maxTargets, new BaseDamage(_amount, false, 0f));
        }
    }

    internal sealed class CurrentTargetsStrategy : ITargetingStrategy
    {
        public IReadOnlyList<BossTarget> ResolveTargets(BossSkillContext context)
        {
            return context.Targets ?? Array.Empty<BossTarget>();
        }
    }

    internal sealed class NearestNTargetingStrategy : ITargetingStrategy
    {
        private readonly int _count;

        public NearestNTargetingStrategy(int count)
        {
            _count = Mathf.Max(1, count);
        }

        public IReadOnlyList<BossTarget> ResolveTargets(BossSkillContext context)
        {
            var targets = context.Targets;
            if (targets == null || targets.Count == 0)
            {
                return Array.Empty<BossTarget>();
            }

            if (context.Caster == null)
            {
                return targets;
            }

            var sorted = new List<BossTarget>(targets);
            Vector3 origin = context.Caster.position;
            sorted.Sort((a, b) =>
            {
                float da = (a.Position - origin).sqrMagnitude;
                float db = (b.Position - origin).sqrMagnitude;
                return da.CompareTo(db);
            });

            int take = Mathf.Min(_count, sorted.Count);
            if (take == sorted.Count)
            {
                return sorted;
            }

            return sorted.GetRange(0, take);
        }
    }

    internal sealed class DamageSkillEffect : ISkillEffect
    {
        private readonly BaseDamage _damage;

        public DamageSkillEffect(BaseDamage damage)
        {
            _damage = damage;
        }

        public void Apply(BossSkillContext context)
        {
            if (context.DamageCalculator == null)
            {
                return;
            }

            context.DamageCalculator.Calculate(context, _damage);
        }
    }

    internal sealed class ProjectileSkillEffect : ISkillEffect
    {
        private readonly string _projectileId;
        private readonly int _count;
        private readonly float _speed;
        private readonly float _lifetime;

        public ProjectileSkillEffect(string projectileId, int count, float speed, float lifetime)
        {
            _projectileId = projectileId;
            _count = Mathf.Max(1, count);
            _speed = Mathf.Max(0f, speed);
            _lifetime = Mathf.Max(0.01f, lifetime);
        }

        public void Apply(BossSkillContext context)
        {
            if (context.ProjectileSpawner == null || context.Caster == null)
            {
                return;
            }

            var targets = context.Targets;
            if (targets == null || targets.Count == 0)
            {
                return;
            }

            int spawnCount = Mathf.Min(_count, targets.Count);
            Vector3 origin = context.Caster.position;
            for (int i = 0; i < spawnCount; i++)
            {
                var target = targets[i];
                Vector3 direction = target.Position - origin;
                var spec = new ProjectileSpec(_projectileId, origin, direction, _speed, _lifetime);
                context.ProjectileSpawner.Spawn(spec, context);
            }
        }
    }

    internal sealed class AreaDamageSkillEffect : ISkillEffect
    {
        private readonly AreaSpec _areaSpec;
        private readonly BaseDamage _damage;

        public AreaDamageSkillEffect(float radius, int maxTargets, BaseDamage damage)
        {
            _areaSpec = new AreaSpec(radius, maxTargets);
            _damage = damage;
        }

        public void Apply(BossSkillContext context)
        {
            if (context.AreaResolver == null || context.Caster == null || context.DamageCalculator == null)
            {
                return;
            }

            var targets = context.AreaResolver.ResolveArea(_areaSpec, context.Caster.position, context);
            if (targets == null || targets.Count == 0)
            {
                return;
            }

            var areaContext = new BossSkillContext(
                context.Run,
                context.Caster,
                targets,
                context.DamageCalculator,
                context.ProjectileSpawner,
                context.AreaResolver,
                context.PhaseCooldownModifier);
            context.DamageCalculator.Calculate(areaContext, _damage);
        }
    }
}
