using System;
using OneDayGame.Domain;
using OneDayGame.Domain.Gameplay;
using OneDayGame.Domain.Repositories;
using OneDayGame.Domain.Policies;

namespace OneDayGame.Application
{
    public sealed class RunSessionService : IRunState
    {
        private readonly RunConfig _config;
        private readonly IRunRepository _repository;
        private readonly IDifficultyPolicy _difficultyPolicy;

        private int _score;
        private int _enemiesSpawned;
        private int _stage;
        private int _killsInStage;
        private float _hp;
        private float _maxHp;
        private float _ultimate;
        private int _experience;
        private int _level;
        private int _expInLevel;
        private int _expToNextLevel;
        private float _elapsed;
        private bool _isDead;
        private bool _hasStarted;
        private int _totalKills;
        private float _totalDamageTaken;

        public event Action<RunSnapshot> SnapshotChanged;
        public event Action<RunSnapshot> RunEnded;
        public event Action<RunSnapshot> Restarted;
        public event Action<bool> DeadStateChanged;
        public event Action<int> LevelUpTriggered;

        public RunSessionService(RunConfig config, IDifficultyPolicy difficultyPolicy, IRunRepository repository)
        {
            _config = config ?? new RunConfig();
            _repository = repository;
            _difficultyPolicy = difficultyPolicy;
            HighScore = _repository?.LoadHighScore() ?? 0;

            ResetInternal(true);
        }

        public int Score => _score;

        public int EnemiesSpawned => _enemiesSpawned;

        public int Stage => _stage;

        public float Hp => _hp;

        public float MaxHp => _maxHp;

        public float Ultimate => _ultimate;

        public int Experience => _experience;

        public int Level => _level;

        public int ExpInLevel => _expInLevel;

        public int ExpToNextLevel => _expToNextLevel;

        public float ElapsedTime => _elapsed;

        public bool IsDead => _isDead;

        public int HighScore { get; private set; }

        public int KillsInCurrentStage => _killsInStage;

        public int TotalKills => _totalKills;

        public float TotalDamageTaken => _totalDamageTaken;

        public RunSnapshot Snapshot => new RunSnapshot(
            _score,
            _enemiesSpawned,
            _stage,
            _hp,
            _maxHp,
            _ultimate,
            _experience,
            _level,
            _expInLevel,
            _expToNextLevel,
            _elapsed);

        public void StartRun()
        {
            _hasStarted = true;
        }

        public void Tick(float deltaTime)
        {
            if (!_hasStarted || _isDead)
            {
                return;
            }

            _elapsed += deltaTime;
            _ultimate = Math.Min(_config.UltimateMax, _ultimate + _config.UltimateRechargePerSecond * deltaTime);

            SnapshotChanged?.Invoke(Snapshot);
        }

        public void ApplyDamage(float damage)
        {
            if (_isDead)
            {
                return;
            }

            _hp = Math.Max(0f, _hp - damage);
            _totalDamageTaken += Math.Max(0f, damage);

            if (_hp <= 0f)
            {
                KillRun();
            }

            SnapshotChanged?.Invoke(Snapshot);
        }

        public bool TryUseUltimate(float cost)
        {
            if (_isDead || cost <= 0f)
            {
                return false;
            }

            if (_ultimate < cost)
            {
                return false;
            }

            _ultimate -= cost;
            SnapshotChanged?.Invoke(Snapshot);
            return true;
        }

        public void RegisterEnemyKill(int reward)
        {
            if (_isDead)
            {
                return;
            }

            _score += Math.Max(0, reward);
            _killsInStage++;
            _totalKills++;

            if (_killsInStage >= GetStageGoal(_stage))
            {
                _stage++;
                _killsInStage = 0;
            }

            if (_score > HighScore)
            {
                HighScore = _score;
                _repository?.SaveHighScore(HighScore);
            }

            SnapshotChanged?.Invoke(Snapshot);
        }

        public void RegisterEnemySpawn()
        {
            if (_isDead)
            {
                return;
            }

            _enemiesSpawned++;
            SnapshotChanged?.Invoke(Snapshot);
        }

        public void RegisterHeal(float healAmount)
        {
            if (_isDead)
            {
                return;
            }

            if (healAmount <= 0f)
            {
                return;
            }

            _hp = Math.Min(_maxHp, _hp + healAmount);
            SnapshotChanged?.Invoke(Snapshot);
        }

        public void RegisterExperience(int amount)
        {
            if (_isDead || amount <= 0)
            {
                return;
            }

            _experience += amount;
            _expInLevel += amount;
            while (_expInLevel >= _expToNextLevel)
            {
                _expInLevel -= _expToNextLevel;
                _level++;
                _expToNextLevel = Math.Min(999999, (int) Math.Ceiling(_expToNextLevel * 1.35f));
                LevelUpTriggered?.Invoke(_level);
            }

            SnapshotChanged?.Invoke(Snapshot);
        }

        public void ApplyMaxHpUpgrade(float amount)
        {
            if (_isDead || amount <= 0f)
            {
                return;
            }

            _maxHp += amount;
            _hp = Math.Min(_maxHp, _hp + amount);
            SnapshotChanged?.Invoke(Snapshot);
        }

        public void Restart()
        {
            ResetInternal(false);
            _hasStarted = true;
            DeadStateChanged?.Invoke(false);
            Restarted?.Invoke(Snapshot);
            SnapshotChanged?.Invoke(Snapshot);
        }

        private void KillRun()
        {
            _isDead = true;
            _hasStarted = false;
            HighScore = Math.Max(HighScore, _score);
            _repository?.SaveHighScore(HighScore);
            SnapshotChanged?.Invoke(Snapshot);
            DeadStateChanged?.Invoke(true);
            RunEnded?.Invoke(Snapshot);
        }

        private int GetStageGoal(int stage)
        {
            if (_difficultyPolicy == null)
            {
                return Math.Max(1, 8 + (stage - 1) * 3);
            }

            return Math.Max(1, _difficultyPolicy.GetKillsToAdvance(stage));
        }

        private void ResetInternal(bool firstBoot)
        {
            _score = 0;
            _enemiesSpawned = 0;
            _stage = _config.InitialStage;
            _killsInStage = 0;
            _maxHp = _config.StartMaxHp;
            _hp = _maxHp;
            _ultimate = _config.UltimateStart;
            _experience = 0;
            _level = 1;
            _expInLevel = 0;
            _expToNextLevel = 20;
            _elapsed = 0f;
            _isDead = false;
            _hasStarted = firstBoot ? false : true;
            _totalKills = 0;
            _totalDamageTaken = 0f;

            if (firstBoot)
            {
                DeadStateChanged?.Invoke(false);
            }
        }
    }
}
