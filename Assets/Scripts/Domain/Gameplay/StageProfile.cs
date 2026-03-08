using System;
using System.Collections.Generic;
using UnityEngine;

namespace OneDayGame.Domain.Gameplay
{
    public interface IStageProfileProvider
    {
        StageProfile ResolveProfile(int stage);
    }

    [Serializable]
    public sealed class StageProfile
    {
        public int Stage { get; }

        public int KillsToAdvance { get; }

        public string EnemyType { get; }

        public float EnemyMaxHp { get; }

        public float EnemyMoveSpeed { get; }

        public float EnemyContactDamage { get; }

        public int EnemyScoreValue { get; }

        public float EnemyContactRadius { get; }

        public float WeaponDisplayDamage { get; }

        public float WeaponDamageIncrement { get; }

        public float WeaponAttackRange { get; }

        public float WeaponAttackCooldown { get; }

        public int WeaponProjectileCount { get; }

        public float WeaponUltimateRadius { get; }

        public float WeaponUltimateCost { get; }

        public float WeaponUltimateMultiplier { get; }

        public float WeaponDotPerSecond { get; }

        public bool WeaponHasDot { get; }

        public float SpawnXMin { get; }

        public float SpawnXMax { get; }

        public float SpawnYMin { get; }

        public float SpawnYMax { get; }

        public float PlayerMinX { get; }

        public float PlayerMaxX { get; }

        public float PlayerMinY { get; }

        public float PlayerMaxY { get; }

        public float SpawnIntervalBase { get; }

        public float SpawnIntervalPerStage { get; }

        public float SpawnIntervalMin { get; }

        public float SpawnIntervalMax { get; }

        public float SpawnIntervalPerActiveEnemy { get; }

        public bool IsBossEnabled { get; }

        public int BossEvery { get; }

        public int BossOffset { get; }

        public float BossHpMultiplier { get; }

        public float BossSpeedMultiplier { get; }

        public float BossDamageMultiplier { get; }

        public float BossContactRadiusMultiplier { get; }

        public int BossScoreMultiplier { get; }

        public string WeaponDisplayName { get; }

        public string WeaponDescription { get; }

        public bool HasWeaponDot => WeaponHasDot;

        public bool IsBossStage(int stage)
        {
            if (!IsBossEnabled || BossEvery <= 0)
            {
                return false;
            }

            return Math.Max(1, stage) % Math.Max(1, BossEvery) == Math.Max(0, BossOffset) % Math.Max(1, BossEvery);
        }

        public StageProfile(
            int stage,
            int killsToAdvance,
            string enemyType,
            float enemyMaxHp,
            float enemyMoveSpeed,
            float enemyContactDamage,
            int enemyScoreValue,
            float enemyContactRadius,
            float weaponDisplayDamage,
            float weaponDamageIncrement,
            float weaponAttackRange,
            float weaponAttackCooldown,
            int weaponProjectileCount,
            float weaponUltimateRadius,
            float weaponUltimateCost,
            float weaponUltimateMultiplier,
            float weaponDotPerSecond,
            bool weaponHasDot,
            float spawnXMin,
            float spawnXMax,
            float spawnYMin,
            float spawnYMax,
            float playerMinX,
            float playerMaxX,
            float playerMinY,
            float playerMaxY,
            float spawnIntervalBase,
            float spawnIntervalPerStage,
            float spawnIntervalMin,
            float spawnIntervalMax,
            float spawnIntervalPerActiveEnemy,
            bool isBossEnabled,
            int bossEvery,
            int bossOffset,
            float bossHpMultiplier,
            float bossSpeedMultiplier,
            float bossDamageMultiplier,
            float bossContactRadiusMultiplier,
            int bossScoreMultiplier,
            string weaponDisplayName,
            string weaponDescription)
        {
            Stage = Math.Max(1, stage);
            KillsToAdvance = Math.Max(1, killsToAdvance);
            EnemyType = enemyType;
            EnemyMaxHp = Math.Max(0.1f, enemyMaxHp);
            EnemyMoveSpeed = Math.Max(0.1f, enemyMoveSpeed);
            EnemyContactDamage = Math.Max(0.1f, enemyContactDamage);
            EnemyScoreValue = Math.Max(1, enemyScoreValue);
            EnemyContactRadius = Math.Max(0.05f, enemyContactRadius);
            WeaponDisplayDamage = Math.Max(0.1f, weaponDisplayDamage);
            WeaponDamageIncrement = weaponDamageIncrement;
            WeaponAttackRange = Math.Max(0.1f, weaponAttackRange);
            WeaponAttackCooldown = Math.Max(0.01f, weaponAttackCooldown);
            WeaponProjectileCount = Math.Max(1, weaponProjectileCount);
            WeaponUltimateRadius = Math.Max(0.1f, weaponUltimateRadius);
            WeaponUltimateCost = Math.Max(0.1f, weaponUltimateCost);
            WeaponUltimateMultiplier = Math.Max(1f, weaponUltimateMultiplier);
            WeaponDotPerSecond = Math.Max(0f, weaponDotPerSecond);
            WeaponHasDot = weaponHasDot;
            SpawnXMin = spawnXMin;
            SpawnXMax = spawnXMax;
            SpawnYMin = spawnYMin;
            SpawnYMax = spawnYMax;
            PlayerMinX = playerMinX;
            PlayerMaxX = playerMaxX;
            PlayerMinY = playerMinY;
            PlayerMaxY = playerMaxY;
            SpawnIntervalBase = Math.Max(0.05f, spawnIntervalBase);
            SpawnIntervalPerStage = spawnIntervalPerStage;
            SpawnIntervalMin = Math.Max(0.05f, spawnIntervalMin);
            SpawnIntervalMax = Math.Max(SpawnIntervalMin, spawnIntervalMax);
            SpawnIntervalPerActiveEnemy = Math.Max(0.001f, spawnIntervalPerActiveEnemy);
            IsBossEnabled = isBossEnabled;
            BossEvery = Math.Max(0, bossEvery);
            BossOffset = Math.Max(0, bossOffset);
            BossHpMultiplier = Math.Max(1f, bossHpMultiplier);
            BossSpeedMultiplier = Math.Max(0.1f, bossSpeedMultiplier);
            BossDamageMultiplier = Math.Max(1f, bossDamageMultiplier);
            BossContactRadiusMultiplier = Math.Max(1f, bossContactRadiusMultiplier);
            BossScoreMultiplier = Math.Max(1, bossScoreMultiplier);
            WeaponDisplayName = string.IsNullOrEmpty(weaponDisplayName) ? "Photon Blade" : weaponDisplayName;
            WeaponDescription = string.IsNullOrEmpty(weaponDescription) ? "Orbiting blade that slices nearby enemies." : weaponDescription;
        }
    }

    [Serializable]
    public sealed class StageBandSettings
    {
        [Min(1)]
        public int StageStart = 1;

        [Min(1)]
        public int StageEnd = 10;

        public string EnemyType = "Normal";

        public int KillsToAdvance = 6;

        public float EnemyBaseMaxHp = 10f;

        public float EnemyHpPerStage = 2.8f;

        public float EnemyBaseMoveSpeed = 1.3f;

        public float EnemySpeedPerStage = 0.16f;

        public float EnemyBaseContactDamage = 8f;

        public float EnemyDamagePerStage = 2f;

        public float EnemyBaseContactRadius = 0.4f;

        public float EnemyRadiusPerStage = 0.02f;

        public int EnemyBaseScore = 10;

        public int EnemyScorePerStage = 1;

        public string WeaponDisplayName = "Photon Blade";

        public string WeaponDescription = "Orbiting blade that slices nearby enemies. Ultimate overload wipes a large radius.";

        public float WeaponDamage = 20f;

        public float WeaponDamagePerStage = 1.5f;

        public float WeaponAttackRange = 0.85f;

        public float WeaponRangePerStage = 0.05f;

        public float WeaponAttackCooldown = 0.25f;

        public float WeaponUltimateRadius = 2.8f;

        public float WeaponUltimateRadiusPerStage = 0.25f;

        public float WeaponUltimateCost = 25f;

        public float WeaponUltimateMultiplier = 1f;

        public float WeaponUltimateMultiplierPerStage = 0.35f;

        public bool WeaponHasDamageOverTime;

        public float WeaponDamageOverTime;

        public int WeaponProjectileCount = 1;

        public float SpawnXMin = -7.6f;

        public float SpawnXMax = 7.6f;

        public float SpawnYMin = 5.2f;

        public float SpawnYMax = 7.5f;

        public float PlayerMinX = -7.8f;

        public float PlayerMaxX = 7.8f;

        public float PlayerMinY = -4.8f;

        public float PlayerMaxY = 4.8f;

        public float SpawnIntervalBase = 1.6f;

        public float SpawnIntervalPerStage = 0.12f;

        public float SpawnIntervalMin = 0.45f;

        public float SpawnIntervalMax = 2.2f;

        public float SpawnIntervalPerActiveEnemy = 0.08f;

        public bool IsBossEnabled;

        public int BossEvery = 0;

        public int BossOffset = 0;

        public float BossHpMultiplier = 2.2f;

        public float BossSpeedMultiplier = 1f;

        public float BossDamageMultiplier = 2f;

        public float BossContactRadiusMultiplier = 1.6f;

        public int BossScoreMultiplier = 4;

        public StageProfile Resolve(int stage)
        {
            int safeStage = Math.Max(1, stage);
            int relativeStage = Math.Max(0, safeStage - StageStart);

            return new StageProfile(
                safeStage,
                KillsToAdvance + (relativeStage * 2),
                EnemyType,
                EnemyBaseMaxHp + EnemyHpPerStage * relativeStage,
                EnemyBaseMoveSpeed + EnemySpeedPerStage * relativeStage,
                EnemyBaseContactDamage + EnemyDamagePerStage * relativeStage,
                EnemyBaseScore + EnemyScorePerStage * relativeStage,
                EnemyBaseContactRadius + EnemyRadiusPerStage * relativeStage,
                WeaponDamage + WeaponDamagePerStage * relativeStage,
                WeaponDamagePerStage,
                WeaponAttackRange + WeaponRangePerStage * relativeStage,
                WeaponAttackCooldown,
                WeaponProjectileCount,
                WeaponUltimateRadius + WeaponUltimateRadiusPerStage * relativeStage,
                WeaponUltimateCost,
                WeaponUltimateMultiplier + WeaponUltimateMultiplierPerStage * relativeStage,
                WeaponDamageOverTime,
                WeaponHasDamageOverTime,
                SpawnXMin,
                SpawnXMax,
                SpawnYMin,
                SpawnYMax,
                PlayerMinX,
                PlayerMaxX,
                PlayerMinY,
                PlayerMaxY,
                SpawnIntervalBase,
                SpawnIntervalPerStage,
                SpawnIntervalMin,
                SpawnIntervalMax,
                SpawnIntervalPerActiveEnemy,
                IsBossEnabled,
                BossEvery,
                BossOffset,
                BossHpMultiplier,
                BossSpeedMultiplier,
                BossDamageMultiplier,
                BossContactRadiusMultiplier,
                BossScoreMultiplier,
                WeaponDisplayName,
                WeaponDescription);
        }
    }

    [CreateAssetMenu(fileName = "StageConfig", menuName = "OneDayGame/Stage Config")]
    public sealed class StageConfig : ScriptableObject, IStageProfileProvider
    {
        [SerializeField]
        private List<StageBandSettings> _bandProfiles = new List<StageBandSettings>();

        [SerializeField]
        private StageBandSettings _fallbackBand = new StageBandSettings();

        public StageProfile ResolveProfile(int stage)
        {
            int safeStage = Math.Max(1, stage);

            if (_bandProfiles == null || _bandProfiles.Count == 0)
            {
                return _fallbackBand.Resolve(safeStage);
            }

            for (int i = 0; i < _bandProfiles.Count; i++)
            {
                var profile = _bandProfiles[i];
                if (profile == null)
                {
                    continue;
                }

                int start = Math.Max(1, profile.StageStart);
                int end = Math.Max(start, profile.StageEnd);

                if (safeStage >= start && safeStage <= end)
                {
                    return profile.Resolve(safeStage);
                }
            }

            return ExtendFallback(safeStage, _bandProfiles[_bandProfiles.Count - 1]);
        }

        private static StageProfile ExtendFallback(int stage, StageBandSettings lastBand)
        {
            if (lastBand == null)
            {
                return new StageBandSettings().Resolve(stage);
            }

            int safeStage = Math.Max(1, stage);
            return lastBand.Resolve(safeStage);
        }
    }

    public sealed class DefaultStageProfileProvider : IStageProfileProvider
    {
        public StageProfile ResolveProfile(int stage)
        {
            return new StageBandSettings().Resolve(Math.Max(1, stage));
        }
    }
}
