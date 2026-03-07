using OneDayGame.Domain.Policies;

namespace OneDayGame.Infrastructure.Policies
{
    public sealed class DefaultWeaponPolicy : IWeaponPolicy
    {
        public float GetPlayerAttackDamage(int stage)
        {
            return 20f + (stage - 1) * 1.5f;
        }

        public float GetPlayerAttackRange(int stage)
        {
            return 0.85f + (stage - 1) * 0.05f;
        }

        public float GetPlayerAttackCooldown(int stage)
        {
            return 0.25f;
        }

        public float GetPlayerUltimateRadius(int stage)
        {
            return 2.8f + (stage - 1) * 0.25f;
        }

        public float GetUltimateCost()
        {
            return 25f;
        }

        public float GetUltimateMultiplier(int stage)
        {
            return 1f + (stage - 1) * 0.35f;
        }
    }
}
