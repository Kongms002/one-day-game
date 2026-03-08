using UnityEngine;

namespace OneDayGame.Domain.Weapons
{
    [CreateAssetMenu(fileName = "WeaponUpgradeRule", menuName = "OneDayGame/Weapon Upgrade Rule")]
    public sealed class WeaponUpgradeRuleSO : ScriptableObject
    {
        public string Id = "damage";
        public WeaponUpgradeEffectType EffectType = WeaponUpgradeEffectType.DamageMultiplier;
        public string Label = "Power +20%";
        public float Value = 1.2f;
        public bool Permanent = true;

        public WeaponUpgradeRule ToRule()
        {
            return new WeaponUpgradeRule(Id, EffectType, Label, Value, Permanent);
        }
    }
}
