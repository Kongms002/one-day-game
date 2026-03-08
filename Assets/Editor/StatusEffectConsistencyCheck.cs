using OneDayGame.Application;
using OneDayGame.Domain.Weapons;
using UnityEditor;
using UnityEngine;

public static class StatusEffectConsistencyCheck
{
    [MenuItem("Tools/OneDayGame/Run Status Effect Consistency Check")]
    public static void Run()
    {
        var loadout = WeaponLoadoutService.CreateDefault();
        var catalog = loadout.Catalog;

        bool hasPoisonCloud = false;
        bool hasLaserPet = false;
        bool hasRagePet = false;
        bool hasStunPet = false;

        for (int i = 0; i < catalog.Count; i++)
        {
            var weapon = catalog[i];
            if (weapon == null)
            {
                continue;
            }

            if (weapon.Id == WeaponId.PoisonCloud)
            {
                hasPoisonCloud = weapon.DotPerSecond > 0f && weapon.Type == WeaponType.Area;
            }
            else if (weapon.Id == WeaponId.LaserPet)
            {
                hasLaserPet = weapon.Type == WeaponType.Projectile;
            }
            else if (weapon.Id == WeaponId.RagePet)
            {
                hasRagePet = weapon.Type == WeaponType.Projectile;
            }
            else if (weapon.Id == WeaponId.StunPet)
            {
                hasStunPet = weapon.Type == WeaponType.Projectile;
            }
        }

        bool passed = hasPoisonCloud && hasLaserPet && hasRagePet && hasStunPet;
        if (passed)
        {
            Debug.Log("[OneDayGame] Status effect consistency check PASSED.");
        }
        else
        {
            Debug.LogError("[OneDayGame] Status effect consistency check FAILED. Verify poison/laser/rage/stun weapon configs.");
        }
    }
}
