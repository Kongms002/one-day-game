namespace OneDayGame.Domain.Weapons
{
    public readonly struct WeaponUpgradeRule
    {
        public WeaponUpgradeRule(string id, WeaponUpgradeEffectType effectType, string label, float value, bool permanent)
        {
            Id = id;
            EffectType = effectType;
            Label = label;
            Value = value;
            Permanent = permanent;
        }

        public string Id { get; }

        public WeaponUpgradeEffectType EffectType { get; }

        public string Label { get; }

        public float Value { get; }

        public bool Permanent { get; }
    }
}
