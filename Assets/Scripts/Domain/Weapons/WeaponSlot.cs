namespace OneDayGame.Domain.Weapons
{
    public sealed class WeaponSlot
    {
        public WeaponSlot(int index, WeaponDefinition definition, int level, bool isLocked)
        {
            Index = index;
            Definition = definition;
            Level = level < 1 ? 1 : level;
            IsLocked = isLocked;
        }

        public int Index { get; }

        public WeaponDefinition Definition { get; private set; }

        public int Level { get; private set; }

        public bool IsLocked { get; private set; }

        public bool IsEmpty => Definition == null;

        public void SetWeapon(WeaponDefinition definition, int level)
        {
            Definition = definition;
            Level = level < 1 ? 1 : level;
            IsLocked = false;
        }

        public void UpgradeLevel(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Level += amount;
        }

        public void Unlock()
        {
            IsLocked = false;
        }
    }
}
