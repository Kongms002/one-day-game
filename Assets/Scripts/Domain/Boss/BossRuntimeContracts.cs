using System;
using System.Collections.Generic;
using UnityEngine;

namespace OneDayGame.Domain.Boss
{
    public enum BossSkillExecutionOrder
    {
        Sequential = 0,
        Parallel = 1
    }

    public readonly struct BossTarget
    {
        public BossTarget(Transform transform, Vector3 position)
        {
            Transform = transform;
            Position = position;
        }

        public Transform Transform { get; }

        public Vector3 Position { get; }
    }

    public readonly struct BaseDamage
    {
        public BaseDamage(float amount, bool canCrit, float critChance)
        {
            Amount = Mathf.Max(0f, amount);
            CanCrit = canCrit;
            CritChance = Mathf.Clamp01(critChance);
        }

        public float Amount { get; }

        public bool CanCrit { get; }

        public float CritChance { get; }
    }

    public readonly struct DamageResult
    {
        public DamageResult(float finalAmount, bool isCritical)
        {
            FinalAmount = Mathf.Max(0f, finalAmount);
            IsCritical = isCritical;
        }

        public float FinalAmount { get; }

        public bool IsCritical { get; }
    }

    public readonly struct ProjectileSpec
    {
        public ProjectileSpec(string projectileId, Vector3 origin, Vector3 direction, float speed, float lifetime)
        {
            ProjectileId = string.IsNullOrWhiteSpace(projectileId) ? "default" : projectileId;
            Origin = origin;
            Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.up;
            Speed = Mathf.Max(0f, speed);
            Lifetime = Mathf.Max(0.01f, lifetime);
        }

        public string ProjectileId { get; }

        public Vector3 Origin { get; }

        public Vector3 Direction { get; }

        public float Speed { get; }

        public float Lifetime { get; }
    }

    public readonly struct AreaSpec
    {
        public AreaSpec(float radius, int maxTargets)
        {
            Radius = Mathf.Max(0.05f, radius);
            MaxTargets = Math.Max(0, maxTargets);
        }

        public float Radius { get; }

        public int MaxTargets { get; }
    }

    public readonly struct BossRunContext
    {
        public BossRunContext(int stage, float elapsedTime, float hp, float maxHp)
        {
            Stage = Math.Max(1, stage);
            ElapsedTime = Mathf.Max(0f, elapsedTime);
            Hp = Mathf.Max(0f, hp);
            MaxHp = Mathf.Max(0.01f, maxHp);
        }

        public int Stage { get; }

        public float ElapsedTime { get; }

        public float Hp { get; }

        public float MaxHp { get; }

        public float HpRatio => Mathf.Clamp01(Hp / MaxHp);
    }

    public readonly struct BossSkillContext
    {
        public BossSkillContext(
            BossRunContext run,
            Transform caster,
            IReadOnlyList<BossTarget> targets,
            IDamageCalculator damageCalculator,
            IProjectileSpawner projectileSpawner,
            IAreaResolver areaResolver,
            float phaseCooldownModifier)
        {
            Run = run;
            Caster = caster;
            Targets = targets;
            DamageCalculator = damageCalculator;
            ProjectileSpawner = projectileSpawner;
            AreaResolver = areaResolver;
            PhaseCooldownModifier = Mathf.Max(0.1f, phaseCooldownModifier);
        }

        public BossRunContext Run { get; }

        public Transform Caster { get; }

        public IReadOnlyList<BossTarget> Targets { get; }

        public IDamageCalculator DamageCalculator { get; }

        public IProjectileSpawner ProjectileSpawner { get; }

        public IAreaResolver AreaResolver { get; }

        public float PhaseCooldownModifier { get; }
    }

    public sealed class BossSkillDefinition
    {
        public string SkillId;
        public float Cooldown;
        public int Weight;
        public float CastTime;
        public BossSkillExecutionOrder ExecutionOrder;
        public int PhaseMask;
        public ITargetingStrategy Targeting;
        public IReadOnlyList<ISkillEffect> Effects;
    }

    public sealed class BossPhaseDefinition
    {
        public int PhaseIndex;
        public float HpMinRatio;
        public float HpMaxRatio;
        public float CooldownModifier;
        public IReadOnlyList<BossSkillDefinition> Skills;
    }

    public sealed class BossConfigDefinition
    {
        public string BossId;
        public float BaseHp;
        public float BaseMoveSpeed;
        public IReadOnlyList<BossPhaseDefinition> Phases;
    }

    public interface ITargetingStrategy
    {
        IReadOnlyList<BossTarget> ResolveTargets(BossSkillContext context);
    }

    public interface ISkillEffect
    {
        void Apply(BossSkillContext context);
    }

    public interface IDamageCalculator
    {
        DamageResult Calculate(BossSkillContext context, BaseDamage damage);
    }

    public interface IProjectileSpawner
    {
        void Spawn(ProjectileSpec spec, BossSkillContext context);
    }

    public interface IAreaResolver
    {
        IReadOnlyList<BossTarget> ResolveArea(AreaSpec spec, Vector3 center, BossSkillContext context);
    }

    public interface IBossSkillCatalog
    {
        IReadOnlyList<BossSkillDefinition> GetSkills(int phaseIndex);
    }

    public interface ICooldownScheduler
    {
        bool IsReady(string skillId);
        void StartCooldown(string skillId, float duration);
        void Tick(float deltaTime);
        void Reset();
    }

    public interface ISkillSelector
    {
        BossSkillDefinition SelectSkill(IReadOnlyList<BossSkillDefinition> candidates, ICooldownScheduler cooldownScheduler, int phaseIndex);
    }

    public interface IBossSkillExecutor
    {
        bool TryExecute(BossSkillContext context, BossSkillDefinition skill, ICooldownScheduler cooldownScheduler);
    }
}
