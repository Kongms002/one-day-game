using UnityEngine;

namespace OneDayGame.Domain.Weapons
{
    public sealed class WeaponDefinition
    {
        public WeaponDefinition(
            WeaponId id,
            string displayName,
            string description,
            WeaponType type,
            WeaponTargetingMode targetingMode,
            float baseDamage,
            float baseRange,
            float baseCooldown,
            int projectileCount,
            float dotPerSecond)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            Type = type;
            TargetingMode = targetingMode;
            BaseDamage = Mathf.Max(0f, baseDamage);
            BaseRange = Mathf.Max(0.1f, baseRange);
            BaseCooldown = Mathf.Max(0.05f, baseCooldown);
            ProjectileCount = Mathf.Max(1, projectileCount);
            DotPerSecond = Mathf.Max(0f, dotPerSecond);
        }

        public WeaponId Id { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public WeaponType Type { get; }

        public WeaponTargetingMode TargetingMode { get; }

        public float BaseDamage { get; }

        public float BaseRange { get; }

        public float BaseCooldown { get; }

        public int ProjectileCount { get; }

        public float DotPerSecond { get; }

        public WeaponStats Evaluate(int stage, int level)
        {
            int safeStage = Mathf.Max(1, stage);
            int safeLevel = Mathf.Max(1, level);
            float stageScale = 1f + (safeStage - 1) * 0.065f;
            float levelScale = 1f + (safeLevel - 1) * 0.2f;
            float cooldownScale = Mathf.Max(0.35f, 1f - (safeLevel - 1) * 0.06f);
            return new WeaponStats(
                BaseDamage * stageScale * levelScale,
                BaseRange * Mathf.Lerp(1f, 1.35f, (safeLevel - 1) / 5f),
                BaseCooldown * cooldownScale,
                ProjectileCount + Mathf.FloorToInt((safeLevel - 1) / 2f),
                DotPerSecond * stageScale * Mathf.Lerp(1f, 1.45f, (safeLevel - 1) / 5f));
        }
    }
}
