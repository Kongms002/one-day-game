using System;
using System.Collections.Generic;
using OneDayGame.Domain.Randomness;
using OneDayGame.Domain.Weapons;

namespace OneDayGame.Application
{
    public sealed class WeaponLoadoutService : IWeaponLoadoutReadModel
    {
        private readonly List<WeaponSlot> _slots;
        private readonly List<WeaponDefinition> _allDefinitions;
        private WeaponSlot _selectedSlot;

        public event Action Changed;

        public WeaponLoadoutService(List<WeaponSlot> slots, List<WeaponDefinition> allDefinitions)
        {
            _slots = slots ?? throw new ArgumentNullException(nameof(slots));
            _allDefinitions = allDefinitions ?? throw new ArgumentNullException(nameof(allDefinitions));

            _selectedSlot = ResolveDefaultSelection();
        }

        public IReadOnlyList<WeaponSlot> Slots => _slots;

        public IReadOnlyList<WeaponDefinition> Catalog => _allDefinitions;

        public WeaponSlot SelectedSlot => _selectedSlot;

        public bool TrySelectWeapon(WeaponId weaponId)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot == null || slot.IsLocked || slot.Definition == null)
                {
                    continue;
                }

                if (slot.Definition.Id != weaponId)
                {
                    continue;
                }

                _selectedSlot = slot;
                Changed?.Invoke();
                return true;
            }

            return false;
        }

        public WeaponDefinition GetSelectedWeapon()
        {
            return _selectedSlot != null ? _selectedSlot.Definition : null;
        }

        public WeaponStats GetSelectedStats(int stage)
        {
            var selected = GetSelectedWeapon();
            if (selected == null)
            {
                return new WeaponStats(0f, 0f, 1f, 1, 0f);
            }

            return selected.Evaluate(stage, _selectedSlot.Level);
        }

        public void ApplyGlobalLevelUp()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot == null || slot.IsLocked || slot.IsEmpty)
                {
                    continue;
                }

                slot.UpgradeLevel(1);
            }

            Changed?.Invoke();
        }

        public bool CanAddWeapon()
        {
            return FindFirstEmptySlot() != null && GetAvailableDefinitions().Count > 0;
        }

        public bool TryAddRandomWeapon(IRandomService randomService, out WeaponDefinition addedWeapon)
        {
            addedWeapon = null;
            if (randomService == null)
            {
                return false;
            }

            var empty = FindFirstEmptySlot();
            if (empty == null)
            {
                return false;
            }

            var pool = GetAvailableDefinitions();
            if (pool.Count == 0)
            {
                return false;
            }

            int index = randomService.Range(0, pool.Count);
            var selected = pool[index];
            empty.SetWeapon(selected, 1);
            if (_selectedSlot == null || _selectedSlot.IsEmpty)
            {
                _selectedSlot = empty;
            }

            addedWeapon = selected;
            Changed?.Invoke();
            return true;
        }

        private WeaponSlot ResolveDefaultSelection()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot != null && !slot.IsLocked && !slot.IsEmpty)
                {
                    return slot;
                }
            }

            return null;
        }

        private WeaponSlot FindFirstEmptySlot()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot != null && !slot.IsLocked && slot.IsEmpty)
                {
                    return slot;
                }
            }

            return null;
        }

        private List<WeaponDefinition> GetAvailableDefinitions()
        {
            var equipped = new HashSet<WeaponId>();
            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot != null && !slot.IsEmpty)
                {
                    equipped.Add(slot.Definition.Id);
                }
            }

            var result = new List<WeaponDefinition>();
            for (int i = 0; i < _allDefinitions.Count; i++)
            {
                var definition = _allDefinitions[i];
                if (definition != null && !equipped.Contains(definition.Id))
                {
                    result.Add(definition);
                }
            }

            return result;
        }

        public static WeaponLoadoutService CreateDefault()
        {
            var definitions = new List<WeaponDefinition>
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

            var slots = new List<WeaponSlot>(4)
            {
                new WeaponSlot(0, definitions[0], 1, false),
                new WeaponSlot(1, null, 1, false),
                new WeaponSlot(2, null, 1, false),
                new WeaponSlot(3, null, 1, false)
            };
            return new WeaponLoadoutService(slots, definitions);
        }

        private static WeaponDefinition CreateMelee()
        {
            return new WeaponDefinition(
                WeaponId.Melee,
                "Rotation Axe",
                "Circular close-range sweep with stable uptime.",
                WeaponType.Rotation,
                WeaponTargetingMode.FixedDirection,
                14f,
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
                13f,
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
                9f,
                7f,
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
                8f,
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
                10f,
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
                6f,
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
                9f,
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
                11f,
                7f,
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
