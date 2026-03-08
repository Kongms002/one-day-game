using System;
using System.Collections.Generic;
using OneDayGame.Application;
using OneDayGame.Application.Boss;
using OneDayGame.Domain.Boss;
using OneDayGame.Domain.Randomness;
using OneDayGame.Presentation.Gameplay;
using UnityEngine;

namespace OneDayGame.Presentation.Boss
{
    public sealed class BossSkillBrain : MonoBehaviour
    {
        private const float DecisionInterval = 0.2f;

        private EnemyView _enemy;
        private RunSessionService _runSession;
        private Transform _primaryTarget;

        private BossConfigDefinition _config;
        private IBossSkillCatalog _catalog;
        private ICooldownScheduler _cooldownScheduler;
        private ISkillSelector _skillSelector;
        private IBossSkillExecutor _skillExecutor;
        private IDamageCalculator _damageCalculator;
        private IProjectileSpawner _projectileSpawner;
        private IAreaResolver _areaResolver;

        private readonly Dictionary<int, float> _phaseCooldownModifiers = new Dictionary<int, float>();
        private float _decisionElapsed;

        public void Initialize(BossConfigSO configSo, EnemyView enemy, Transform primaryTarget, RunSessionService runSession, IRandomService randomService)
        {
            _enemy = enemy;
            _primaryTarget = primaryTarget;
            _runSession = runSession;
            _decisionElapsed = 0f;

            if (configSo == null || _enemy == null || _runSession == null || randomService == null)
            {
                DisableAndReset();
                return;
            }

            _config = configSo.BuildDefinition();
            if (_config == null || _config.Phases == null || _config.Phases.Count == 0)
            {
                DisableAndReset();
                return;
            }

            _catalog = new BossSkillCatalog(_config);
            _cooldownScheduler = new BossCooldownScheduler();
            _skillSelector = new WeightedSkillSelector(randomService);
            _skillExecutor = new BossSkillExecutor();
            _damageCalculator = new BossDamageCalculator(_enemy, _primaryTarget, _runSession, randomService);
            _projectileSpawner = new BossProjectileSpawner();
            _areaResolver = new BossAreaResolver();
            BuildPhaseCooldownModifiers(_config);

            enabled = true;
        }

        public void DisableAndReset()
        {
            enabled = false;
            _enemy = null;
            _primaryTarget = null;
            _runSession = null;
            _config = null;
            _catalog = null;
            _cooldownScheduler = null;
            _skillSelector = null;
            _skillExecutor = null;
            _damageCalculator = null;
            _projectileSpawner = null;
            _areaResolver = null;
            _phaseCooldownModifiers.Clear();
            _decisionElapsed = 0f;
        }

        private void Update()
        {
            if (_enemy == null || _runSession == null || _runSession.IsDead || _enemy.IsDead)
            {
                return;
            }

            _cooldownScheduler?.Tick(Time.deltaTime);
            _decisionElapsed += Time.deltaTime;
            if (_decisionElapsed < DecisionInterval)
            {
                return;
            }

            _decisionElapsed = 0f;
            if (_config == null || _catalog == null || _skillSelector == null || _skillExecutor == null || _cooldownScheduler == null)
            {
                return;
            }

            var run = new BossRunContext(_runSession.Stage, _runSession.ElapsedTime, _enemy.CurrentHp, _enemy.MaxHp);
            int phaseIndex = BossPhaseResolver.ResolvePhaseIndex(_config, run.HpRatio);
            var skills = _catalog.GetSkills(phaseIndex);
            var selected = _skillSelector.SelectSkill(skills, _cooldownScheduler, phaseIndex);
            if (selected == null)
            {
                return;
            }

            var targets = BuildTargets();
            float cooldownModifier = ResolvePhaseCooldownModifier(phaseIndex);
            var context = new BossSkillContext(
                run,
                transform,
                targets,
                _damageCalculator,
                _projectileSpawner,
                _areaResolver,
                cooldownModifier);

            _skillExecutor.TryExecute(context, selected, _cooldownScheduler);
        }

        private IReadOnlyList<BossTarget> BuildTargets()
        {
            var targets = new List<BossTarget>(1);
            if (_primaryTarget != null)
            {
                targets.Add(new BossTarget(_primaryTarget, _primaryTarget.position));
            }

            return targets;
        }

        private void BuildPhaseCooldownModifiers(BossConfigDefinition config)
        {
            _phaseCooldownModifiers.Clear();
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

                _phaseCooldownModifiers[phase.PhaseIndex] = Mathf.Max(0.1f, phase.CooldownModifier);
            }
        }

        private float ResolvePhaseCooldownModifier(int phaseIndex)
        {
            if (_phaseCooldownModifiers.TryGetValue(phaseIndex, out float value))
            {
                return value;
            }

            return 1f;
        }

        private sealed class BossDamageCalculator : IDamageCalculator
        {
            private readonly EnemyView _self;
            private readonly Transform _primaryTarget;
            private readonly RunSessionService _runSession;
            private readonly IRandomService _random;

            public BossDamageCalculator(EnemyView self, Transform primaryTarget, RunSessionService runSession, IRandomService random)
            {
                _self = self;
                _primaryTarget = primaryTarget;
                _runSession = runSession;
                _random = random;
            }

            public DamageResult Calculate(BossSkillContext context, BaseDamage damage)
            {
                bool critical = damage.CanCrit && _random != null && _random.Value() <= damage.CritChance;
                float amount = critical ? damage.Amount * 1.5f : damage.Amount;
                if (amount <= 0f || context.Targets == null)
                {
                    return new DamageResult(amount, critical);
                }

                for (int i = 0; i < context.Targets.Count; i++)
                {
                    var target = context.Targets[i];
                    var targetTransform = target.Transform;
                    if (targetTransform == null)
                    {
                        continue;
                    }

                    if (targetTransform.TryGetComponent<EnemyView>(out var enemy) && enemy != null && enemy != _self)
                    {
                        enemy.ApplyDamage(amount);
                        continue;
                    }

                    if (_runSession != null && _primaryTarget != null && targetTransform == _primaryTarget)
                    {
                        _runSession.ApplyDamage(amount);
                    }
                }

                return new DamageResult(amount, critical);
            }
        }

        private sealed class BossProjectileSpawner : IProjectileSpawner
        {
            public void Spawn(ProjectileSpec spec, BossSkillContext context)
            {
                var origin = spec.Origin;
                var destination = origin + spec.Direction * Mathf.Max(0.2f, spec.Speed * Mathf.Min(1f, spec.Lifetime));
                Debug.DrawLine(origin, destination, new Color(1f, 0.35f, 0.2f, 0.9f), Mathf.Max(0.05f, spec.Lifetime));
            }
        }

        private sealed class BossAreaResolver : IAreaResolver
        {
            public IReadOnlyList<BossTarget> ResolveArea(AreaSpec spec, Vector3 center, BossSkillContext context)
            {
                if (context.Targets == null || context.Targets.Count == 0)
                {
                    return Array.Empty<BossTarget>();
                }

                float radiusSq = spec.Radius * spec.Radius;
                var resolved = new List<BossTarget>(context.Targets.Count);
                for (int i = 0; i < context.Targets.Count; i++)
                {
                    var target = context.Targets[i];
                    if ((target.Position - center).sqrMagnitude > radiusSq)
                    {
                        continue;
                    }

                    resolved.Add(target);
                    if (spec.MaxTargets > 0 && resolved.Count >= spec.MaxTargets)
                    {
                        break;
                    }
                }

                return resolved;
            }
        }
    }
}
