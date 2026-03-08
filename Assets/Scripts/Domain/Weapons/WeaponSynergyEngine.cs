using System.Collections.Generic;

namespace OneDayGame.Domain.Weapons
{
    public static class WeaponSynergyEngine
    {
        public static float EvaluateDamageMultiplier(IReadOnlyList<WeaponSlot> slots)
        {
            if (slots == null)
            {
                return 1f;
            }

            bool hasArrow = false;
            bool hasBoomerang = false;
            bool hasBlackHole = false;
            bool hasPoisonCloud = false;
            bool hasLaserPet = false;
            bool hasStunPet = false;

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.IsLocked || slot.IsEmpty)
                {
                    continue;
                }

                switch (slot.Definition.Id)
                {
                    case WeaponId.Arrow:
                        hasArrow = true;
                        break;
                    case WeaponId.Boomerang:
                        hasBoomerang = true;
                        break;
                    case WeaponId.BlackHole:
                        hasBlackHole = true;
                        break;
                    case WeaponId.PoisonCloud:
                        hasPoisonCloud = true;
                        break;
                    case WeaponId.LaserPet:
                        hasLaserPet = true;
                        break;
                    case WeaponId.StunPet:
                        hasStunPet = true;
                        break;
                }
            }

            float multiplier = 1f;
            if (hasArrow && hasBoomerang)
            {
                multiplier *= 1.12f;
            }

            if (hasBlackHole && hasPoisonCloud)
            {
                multiplier *= 1.18f;
            }

            if (hasLaserPet && hasStunPet)
            {
                multiplier *= 1.1f;
            }

            return multiplier;
        }
    }
}
