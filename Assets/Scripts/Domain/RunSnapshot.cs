namespace OneDayGame.Domain
{
    public readonly struct RunSnapshot
    {
        public int Score { get; }
        public int Stage { get; }
        public float Hp { get; }
        public float MaxHp { get; }
        public float Ultimate { get; }
        public float ElapsedTime { get; }

        public RunSnapshot(int score, int stage, float hp, float maxHp, float ultimate, float elapsedTime)
        {
            Score = score;
            Stage = stage;
            Hp = hp;
            MaxHp = maxHp;
            Ultimate = ultimate;
            ElapsedTime = elapsedTime;
        }
    }
}
