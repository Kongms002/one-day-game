using System;
using System.Collections.Generic;

namespace OneDayGame.Domain.Weapons
{
    public interface IWeaponLoadoutReadModel
    {
        IReadOnlyList<WeaponSlot> Slots { get; }

        WeaponSlot SelectedSlot { get; }

        event Action Changed;

        bool TrySelectWeapon(WeaponId weaponId);

        WeaponDefinition GetSelectedWeapon();

        WeaponStats GetSelectedStats(int stage);
    }
}
