namespace OneDayGame.Domain.Weapons
{
    public readonly struct WeaponStats
    {
        public WeaponStats(float damage, float range, float cooldown, int projectileCount, float dotPerSecond)
        {
            Damage = damage;
            Range = range;
            Cooldown = cooldown;
            ProjectileCount = projectileCount;
            DotPerSecond = dotPerSecond;
        }

        public float Damage { get; }

        public float Range { get; }

        public float Cooldown { get; }

        public int ProjectileCount { get; }

        public float DotPerSecond { get; }
    }
}
