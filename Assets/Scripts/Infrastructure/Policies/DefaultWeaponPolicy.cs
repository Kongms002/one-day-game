using System;
using OneDayGame.Domain.Gameplay;
using OneDayGame.Domain.Policies;

namespace OneDayGame.Infrastructure.Policies
{
    public sealed class DefaultWeaponPolicy : IWeaponPolicy
    {
        private readonly IStageProfileProvider _stageProfileProvider;

        public DefaultWeaponPolicy() : this((IStageProfileProvider) null)
        {
        }

        public DefaultWeaponPolicy(IStageProfileProvider stageProfileProvider)
        {
            _stageProfileProvider = stageProfileProvider;
        }

        public string GetWeaponDisplayName(int stage)
        {
            return ResolveProfile(stage).WeaponDisplayName;
        }

        public string GetWeaponDescription(int stage)
        {
            return ResolveProfile(stage).WeaponDescription;
        }

        public float GetDamageOverTimePerSecond(int stage)
        {
            return ResolveProfile(stage).WeaponDotPerSecond;
        }

        public int GetProjectileCount(int stage)
        {
            return ResolveProfile(stage).WeaponProjectileCount;
        }

        public float GetPlayerAttackDamage(int stage)
        {
            return ResolveProfile(stage).WeaponDisplayDamage;
        }

        public float GetPlayerAttackRange(int stage)
        {
            return ResolveProfile(stage).WeaponAttackRange;
        }

        public float GetPlayerAttackCooldown(int stage)
        {
            return ResolveProfile(stage).WeaponAttackCooldown;
        }

        public float GetPlayerUltimateRadius(int stage)
        {
            return ResolveProfile(stage).WeaponUltimateRadius;
        }

        public bool HasDamageOverTime(int stage)
        {
            return ResolveProfile(stage).HasWeaponDot;
        }

        public float GetUltimateMultiplier(int stage)
        {
            return ResolveProfile(stage).WeaponUltimateMultiplier;
        }

        public float GetUltimateCost(int stage)
        {
            return ResolveProfile(stage).WeaponUltimateCost;
        }

        private StageProfile ResolveProfile(int stage)
        {
            var safeStage = Math.Max(1, stage);
            if (_stageProfileProvider != null)
            {
                return _stageProfileProvider.ResolveProfile(safeStage);
            }

            return new StageBandSettings().Resolve(safeStage);
        }
    }
}
