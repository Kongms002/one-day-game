using System.Collections.Generic;
using OneDayGame.Domain.Weapons;

namespace OneDayGame.Application
{
    public static class WeaponCatalog
    {
        public static List<WeaponDefinition> CreateDefault()
        {
            return new List<WeaponDefinition>
            {
                CreateMelee(),
                CreateArrow(),
                CreateBoomerang(),
                CreateGroundZone(),
                CreateTurret(),
                CreateBlackHole(),
                CreatePoisonCloud(),
                CreateLaserPet(),
                CreateRagePet(),
                CreateStunPet()
            };
        }

        private static WeaponDefinition CreateMelee()
        {
            return new WeaponDefinition(
                WeaponId.Melee,
                "Rotation Axe",
                "Circular close-range sweep with stable uptime.",
                WeaponType.Rotation,
                WeaponTargetingMode.FixedDirection,
                8.5f,
                1f,
                0.3f,
                1,
                0f);
        }

        private static WeaponDefinition CreateArrow()
        {
            return new WeaponDefinition(
                WeaponId.Arrow,
                "Arrow",
                "Fast projectile with short tracking adjustment.",
                WeaponType.Projectile,
                WeaponTargetingMode.AutoNearest,
                8f,
                8f,
                0.45f,
                1,
                0f);
        }

        private static WeaponDefinition CreateBoomerang()
        {
            return new WeaponDefinition(
                WeaponId.Boomerang,
                "Boomerang",
                "Curve throw that can hit while returning.",
                WeaponType.Projectile,
                WeaponTargetingMode.FixedDirection,
                5.8f,
                4.5f,
                0.4f,
                1,
                0f);
        }

        private static WeaponDefinition CreateGroundZone()
        {
            return new WeaponDefinition(
                WeaponId.GroundZone,
                "Ground Zone",
                "Persistent damaging zone under enemy path.",
                WeaponType.Area,
                WeaponTargetingMode.AutoNearest,
                7f,
                1.5f,
                1.2f,
                1,
                2f);
        }

        private static WeaponDefinition CreateTurret()
        {
            return new WeaponDefinition(
                WeaponId.Turret,
                "Turret",
                "Deploys short burst auto fire around nearby threats.",
                WeaponType.Persistent,
                WeaponTargetingMode.AutoNearest,
                5.2f,
                6f,
                0.65f,
                2,
                0f);
        }

        private static WeaponDefinition CreateBlackHole()
        {
            return new WeaponDefinition(
                WeaponId.BlackHole,
                "Black Hole",
                "Pulls enemies into an unstable singularity zone.",
                WeaponType.Area,
                WeaponTargetingMode.AutoNearest,
                6.2f,
                2.2f,
                1.5f,
                1,
                3f);
        }

        private static WeaponDefinition CreatePoisonCloud()
        {
            return new WeaponDefinition(
                WeaponId.PoisonCloud,
                "Poison Cloud",
                "Persistent toxic cloud that stacks over time.",
                WeaponType.Area,
                WeaponTargetingMode.AutoNearest,
                4f,
                1.8f,
                1.1f,
                1,
                5f);
        }

        private static WeaponDefinition CreateLaserPet()
        {
            return new WeaponDefinition(
                WeaponId.LaserPet,
                "Laser Pet",
                "Companion laser picks nearest target repeatedly.",
                WeaponType.Projectile,
                WeaponTargetingMode.AutoNearest,
                5.8f,
                9f,
                0.38f,
                1,
                0f);
        }

        private static WeaponDefinition CreateRagePet()
        {
            return new WeaponDefinition(
                WeaponId.RagePet,
                "Rage Pet",
                "Companion performs aggressive burst impacts.",
                WeaponType.Projectile,
                WeaponTargetingMode.AutoNearest,
                7f,
                4.8f,
                0.55f,
                1,
                0f);
        }

        private static WeaponDefinition CreateStunPet()
        {
            return new WeaponDefinition(
                WeaponId.StunPet,
                "Stun Pet",
                "Pulse shots that briefly stagger enemy advance.",
                WeaponType.Projectile,
                WeaponTargetingMode.AutoNearest,
                7f,
                8f,
                0.5f,
                1,
                0f);
        }
    }
}
