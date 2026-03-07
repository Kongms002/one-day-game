namespace OneDayGame.Domain.Policies
{
    public interface IWeaponPolicy
    {
        float GetPlayerAttackDamage(int stage);

        float GetPlayerAttackRange(int stage);

        float GetPlayerAttackCooldown(int stage);

        float GetPlayerUltimateRadius(int stage);

        float GetUltimateCost();

        float GetUltimateMultiplier(int stage);
    }
}
