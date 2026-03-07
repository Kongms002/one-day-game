using System;
using OneDayGame.Domain;

namespace OneDayGame.Domain.Gameplay
{
    public interface IRunState
    {
        int Score { get; }

        int Stage { get; }

        float Hp { get; }

        float MaxHp { get; }

        float Ultimate { get; }

        float ElapsedTime { get; }

        bool IsDead { get; }

        int HighScore { get; }

        int KillsInCurrentStage { get; }

        RunSnapshot Snapshot { get; }

        event Action<RunSnapshot> SnapshotChanged;

        event Action<RunSnapshot> RunEnded;

        event Action<RunSnapshot> Restarted;

        event Action<bool> DeadStateChanged;
    }
}
