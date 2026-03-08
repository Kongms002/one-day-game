namespace OneDayGame.Domain.Policies
{
    public interface IWeaponPolicy
    {
        string GetWeaponDisplayName(int stage);

        string GetWeaponDescription(int stage);

        bool HasDamageOverTime(int stage);

        float GetDamageOverTimePerSecond(int stage);

        int GetProjectileCount(int stage);

        float GetPlayerAttackDamage(int stage);

        float GetPlayerAttackRange(int stage);

        float GetPlayerAttackCooldown(int stage);

        float GetPlayerUltimateRadius(int stage);

        float GetUltimateCost(int stage);

        float GetUltimateMultiplier(int stage);
    }
}
