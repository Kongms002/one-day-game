using System;
using System.Collections.Generic;
using OneDayGame.Domain.Weapons;
using UnityEngine;

namespace OneDayGame.Application
{
    public sealed class WeaponOrchestrator
    {
        private readonly Dictionary<WeaponId, float> _cooldowns = new Dictionary<WeaponId, float>();

        public void Reset()
        {
            _cooldowns.Clear();
        }

        public void Tick(
            IReadOnlyList<WeaponSlot> slots,
            int stage,
            float attackSpeedMultiplier,
            float deltaTime,
            Action<WeaponSlot, WeaponStats> onAttack)
        {
            if (slots == null || onAttack == null)
            {
                return;
            }

            float safeAttackSpeed = Mathf.Max(0.01f, attackSpeedMultiplier);
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.IsLocked || slot.IsEmpty)
                {
                    continue;
                }

                var weaponId = slot.Definition.Id;
                if (!_cooldowns.ContainsKey(weaponId))
                {
                    _cooldowns[weaponId] = 0f;
                }

                float nextCooldown = _cooldowns[weaponId] - deltaTime;
                if (nextCooldown > 0f)
                {
                    _cooldowns[weaponId] = nextCooldown;
                    continue;
                }

                var stats = slot.Definition.Evaluate(stage, slot.Level);
                onAttack(slot, stats);
                _cooldowns[weaponId] = Mathf.Max(0.05f, stats.Cooldown / safeAttackSpeed);
            }
        }
    }
}
