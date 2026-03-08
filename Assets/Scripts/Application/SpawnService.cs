using System;
using OneDayGame.Domain.Gameplay;
using OneDayGame.Domain.Policies;
using OneDayGame.Domain.Randomness;
using UnityEngine;

namespace OneDayGame.Application
{
    public sealed class SpawnService
    {
        private readonly ISpawnPolicy _spawnPolicy;
        private readonly IDifficultyPolicy _difficultyPolicy;
        private readonly IItemPolicy _itemPolicy;
        private readonly IMapPolicy _mapPolicy;
        private readonly IRandomService _randomService;
        private readonly IStageProfileProvider _stageProfileProvider;

        private float _enemySpawnElapsed;
        private float _medKitSpawnElapsed;
        private int _lastStage = -1;
        private bool _bossSpawnedForStage;

        public SpawnService(
            ISpawnPolicy spawnPolicy,
            IDifficultyPolicy difficultyPolicy,
            IItemPolicy itemPolicy,
            IMapPolicy mapPolicy,
            IRandomService randomService,
            IStageProfileProvider stageProfileProvider)
        {
            _spawnPolicy = spawnPolicy;
            _difficultyPolicy = difficultyPolicy;
            _itemPolicy = itemPolicy;
            _mapPolicy = mapPolicy;
            _randomService = randomService;
            _stageProfileProvider = stageProfileProvider;
        }

        public event Action<SpawnRequest> SpawnRequested;
        public event Action<SpawnRequest> MedKitRequested;

        public void Tick(float deltaTime, IRunState runState, int activeEnemyCount)
        {
            if (runState == null || runState.IsDead || _spawnPolicy == null || _difficultyPolicy == null || _itemPolicy == null || _mapPolicy == null || _randomService == null)
            {
                return;
            }

            _enemySpawnElapsed += deltaTime;
            _medKitSpawnElapsed += deltaTime;

            if (_lastStage != runState.Stage)
            {
                _lastStage = runState.Stage;
                _bossSpawnedForStage = false;
            }

            if (!_bossSpawnedForStage && TrySpawnBoss(runState))
            {
                _bossSpawnedForStage = true;
            }

            if (_enemySpawnElapsed >= _spawnPolicy.GetSpawnInterval(runState.Stage, activeEnemyCount))
            {
                var enemyData = _difficultyPolicy.GetEnemyData(runState.Stage);
                var request = _spawnPolicy.CreateEnemyRequest(runState.Stage, _randomService, _mapPolicy, enemyData);
                SpawnRequested?.Invoke(request);

                if (enemyData.Archetype == EnemyArchetype.Swarm)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        var extraX = request.X + _randomService.Range(-0.8f, 0.8f);
                        var extraY = request.Y + _randomService.Range(-0.8f, 0.8f);
                        var swarmData = new EnemyData(
                            enemyData.MaxHp * 0.8f,
                            enemyData.MoveSpeed * 1.08f,
                            enemyData.ContactDamage * 0.92f,
                            enemyData.ScoreValue,
                            enemyData.ContactRadius * 0.9f,
                            EnemyArchetype.Swarm,
                            false);
                        SpawnRequested?.Invoke(SpawnRequest.Enemy(extraX, extraY, swarmData));
                    }
                }

                _enemySpawnElapsed = 0f;
            }

            var medkitInterval = _itemPolicy.GetMedKitSpawnInterval(runState.Stage);
            if (medkitInterval < 0.3f)
            {
                medkitInterval = 0.3f;
            }
            if (_medKitSpawnElapsed >= medkitInterval &&
                _itemPolicy.ShouldSpawnMedKit(runState.Stage, _medKitSpawnElapsed, _randomService))
            {
                var x = _randomService.Range(_mapPolicy.SpawnXMin, _mapPolicy.SpawnXMax);
                var y = _randomService.Range(_mapPolicy.SpawnYMin, _mapPolicy.SpawnYMax);
                SpawnRequest request;
                if (_randomService.Value() < _itemPolicy.GetMagnetSpawnChance(runState.Stage))
                {
                    request = SpawnRequest.Magnet(
                        x,
                        y,
                        _itemPolicy.GetMagnetDuration(runState.Stage),
                        _itemPolicy.GetMagnetRadius(runState.Stage));
                }
                else
                {
                    request = SpawnRequest.MedKit(
                        x,
                        y,
                        _itemPolicy.GetMedKitHealAmount(runState.Stage),
                        _itemPolicy.GetMedKitScore(runState.Stage));
                }

                MedKitRequested?.Invoke(request);
                _medKitSpawnElapsed = 0f;
            }
        }

        public void Reset()
        {
            _enemySpawnElapsed = 0f;
            _medKitSpawnElapsed = 0f;
            _lastStage = -1;
            _bossSpawnedForStage = false;
        }

        private bool TrySpawnBoss(IRunState runState)
        {
            if (_stageProfileProvider == null || runState == null)
            {
                return false;
            }

            int stage = runState.Stage;
            if (stage <= 0 || stage % 10 != 0)
            {
                return false;
            }

            var profile = _stageProfileProvider.ResolveProfile(stage);
            if (profile == null)
            {
                return false;
            }

            var baseData = _difficultyPolicy.GetEnemyData(stage);
            var bossData = new EnemyData(
                baseData.MaxHp * profile.BossHpMultiplier,
                baseData.MoveSpeed * profile.BossSpeedMultiplier,
                baseData.ContactDamage * profile.BossDamageMultiplier,
                Mathf.Max(1, baseData.ScoreValue * profile.BossScoreMultiplier),
                baseData.ContactRadius * profile.BossContactRadiusMultiplier,
                EnemyArchetype.Tank,
                true);

            float x = (_mapPolicy.PlayerMinX + _mapPolicy.PlayerMaxX) * 0.5f;
            float y = _mapPolicy.PlayerMaxY - 0.8f;
            SpawnRequested?.Invoke(SpawnRequest.Enemy(x, y, bossData));
            return true;
        }
    }
}
