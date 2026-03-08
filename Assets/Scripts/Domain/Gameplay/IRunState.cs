using System;
using OneDayGame.Domain;

namespace OneDayGame.Domain.Gameplay
{
    public interface IRunState
    {
        int Score { get; }
        int EnemiesSpawned { get; }

        int Stage { get; }

        float Hp { get; }

        float MaxHp { get; }

        float Ultimate { get; }

        int Experience { get; }

        int Level { get; }

        int ExpInLevel { get; }

        int ExpToNextLevel { get; }

        float ElapsedTime { get; }

        bool IsDead { get; }

        int HighScore { get; }

        int KillsInCurrentStage { get; }

        int TotalKills { get; }

        float TotalDamageTaken { get; }

        RunSnapshot Snapshot { get; }

        event Action<RunSnapshot> SnapshotChanged;

        event Action<RunSnapshot> RunEnded;

        event Action<RunSnapshot> Restarted;

        event Action<bool> DeadStateChanged;

        event Action<int> LevelUpTriggered;
    }
}
