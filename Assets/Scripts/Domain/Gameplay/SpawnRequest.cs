namespace OneDayGame.Domain.Gameplay
{
    public enum SpawnKind
    {
        Enemy,
        MedKit,
        MagnetPickup,
    }

    public readonly struct SpawnRequest
    {
        public SpawnKind Kind { get; }

        public float X { get; }

        public float Y { get; }

        public EnemyData EnemyData { get; }

        public float MedKitHealAmount { get; }

        public int ScoreReward { get; }

        public float MagnetDuration { get; }

        public float MagnetRadius { get; }

        public SpawnRequest(SpawnKind kind, float x, float y, EnemyData enemyData, float medKitHealAmount, int scoreReward, float magnetDuration, float magnetRadius)
        {
            Kind = kind;
            X = x;
            Y = y;
            EnemyData = enemyData;
            MedKitHealAmount = medKitHealAmount;
            ScoreReward = scoreReward;
            MagnetDuration = magnetDuration;
            MagnetRadius = magnetRadius;
        }

        public static SpawnRequest Enemy(float x, float y, EnemyData enemyData)
        {
            return new SpawnRequest(SpawnKind.Enemy, x, y, enemyData, 0f, 0, 0f, 0f);
        }

        public static SpawnRequest MedKit(float x, float y, float healAmount, int scoreReward)
        {
            return new SpawnRequest(SpawnKind.MedKit, x, y, new EnemyData(0f, 0f, 0f, 0, 0f, EnemyArchetype.Normal), healAmount, scoreReward, 0f, 0f);
        }

        public static SpawnRequest Magnet(float x, float y, float duration, float radius)
        {
            return new SpawnRequest(SpawnKind.MagnetPickup, x, y, new EnemyData(0f, 0f, 0f, 0, 0f, EnemyArchetype.Normal), 0f, 0, duration, radius);
        }
    }
}
