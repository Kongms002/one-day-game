namespace OneDayGame.Domain
{
    public readonly struct RunSnapshot
    {
        public int Score { get; }
        public int EnemiesSpawned { get; }
        public int Stage { get; }
        public float Hp { get; }
        public float MaxHp { get; }
        public float Ultimate { get; }
        public int Experience { get; }
        public int Level { get; }
        public int ExpInLevel { get; }
        public int ExpToNextLevel { get; }
        public float ElapsedTime { get; }

        public RunSnapshot(
            int score,
            int enemiesSpawned,
            int stage,
            float hp,
            float maxHp,
            float ultimate,
            int experience,
            int level,
            int expInLevel,
            int expToNextLevel,
            float elapsedTime)
        {
            Score = score;
            EnemiesSpawned = enemiesSpawned;
            Stage = stage;
            Hp = hp;
            MaxHp = maxHp;
            Ultimate = ultimate;
            Experience = experience;
            Level = level;
            ExpInLevel = expInLevel;
            ExpToNextLevel = expToNextLevel;
            ElapsedTime = elapsedTime;
        }
    }
}
