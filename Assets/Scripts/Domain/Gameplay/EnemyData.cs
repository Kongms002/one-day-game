namespace OneDayGame.Domain.Gameplay
{
    public readonly struct EnemyData
    {
        public float MaxHp { get; }

        public float MoveSpeed { get; }

        public float ContactDamage { get; }

        public int ScoreValue { get; }

        public float ContactRadius { get; }

        public EnemyArchetype Archetype { get; }

        public bool IsBoss { get; }

        public EnemyData(float maxHp, float moveSpeed, float contactDamage, int scoreValue, float contactRadius, EnemyArchetype archetype, bool isBoss = false)
        {
            MaxHp = maxHp;
            MoveSpeed = moveSpeed;
            ContactDamage = contactDamage;
            ScoreValue = scoreValue;
            ContactRadius = contactRadius;
            Archetype = archetype;
            IsBoss = isBoss;
        }
    }
}
