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
            var definitions = WeaponCatalog.CreateDefault();

            var slots = new List<WeaponSlot>(4)
            {
                new WeaponSlot(0, definitions[0], 1, false),
                new WeaponSlot(1, null, 1, false),
                new WeaponSlot(2, null, 1, false),
                new WeaponSlot(3, null, 1, false)
            };
            return new WeaponLoadoutService(slots, definitions);
        }
    }
}
