using System.Collections.Generic;
using OneDayGame.Domain.Randomness;
using OneDayGame.Domain.Weapons;

namespace OneDayGame.Application
{
    public sealed class WeaponUpgradeRuleService
    {
        private readonly List<WeaponUpgradeRule> _catalog = new List<WeaponUpgradeRule>
        {
            new WeaponUpgradeRule("damage", WeaponUpgradeEffectType.DamageMultiplier, "Power +20%", 1.2f, true),
            new WeaponUpgradeRule("attack-speed", WeaponUpgradeEffectType.AttackSpeedMultiplier, "Attack Speed +15%", 1.15f, true),
            new WeaponUpgradeRule("max-hp", WeaponUpgradeEffectType.MaxHpFlat, "Max HP +20", 20f, true),
            new WeaponUpgradeRule("add-weapon", WeaponUpgradeEffectType.AddRandomWeapon, "Add Random Weapon", 0f, true)
        };

        private readonly HashSet<string> _appliedPermanentRuleIds = new HashSet<string>();

        public WeaponUpgradeRule[] BuildChoices(WeaponLoadoutService loadout, IRandomService random)
        {
            var choices = new[]
            {
                _catalog[0],
                _catalog[1],
                _catalog[2]
            };

            if (loadout != null && random != null && loadout.CanAddWeapon() && random.Value() <= 0.42f)
            {
                int replaceIndex = random.Range(0, choices.Length);
                choices[replaceIndex] = _catalog[3];
            }

            return choices;
        }

        public void MarkApplied(WeaponUpgradeRule rule)
        {
            if (rule.Permanent && !string.IsNullOrEmpty(rule.Id))
            {
                _appliedPermanentRuleIds.Add(rule.Id);
            }
        }

        public bool IsAppliedPermanent(string ruleId)
        {
            return !string.IsNullOrEmpty(ruleId) && _appliedPermanentRuleIds.Contains(ruleId);
        }
    }
}
