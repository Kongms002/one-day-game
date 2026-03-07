using System;
using OneDayGame.Domain.Gameplay;
using OneDayGame.Domain.Policies;
using OneDayGame.Domain.Randomness;

namespace OneDayGame.Application
{
    public sealed class SpawnService
    {
        private readonly ISpawnPolicy _spawnPolicy;
        private readonly IDifficultyPolicy _difficultyPolicy;
        private readonly IItemPolicy _itemPolicy;
        private readonly IMapPolicy _mapPolicy;
        private readonly IRandomService _randomService;

        private float _enemySpawnElapsed;
        private float _medKitSpawnElapsed;

        public SpawnService(
            ISpawnPolicy spawnPolicy,
            IDifficultyPolicy difficultyPolicy,
            IItemPolicy itemPolicy,
            IMapPolicy mapPolicy,
            IRandomService randomService)
        {
            _spawnPolicy = spawnPolicy;
            _difficultyPolicy = difficultyPolicy;
            _itemPolicy = itemPolicy;
            _mapPolicy = mapPolicy;
            _randomService = randomService;
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

            if (_enemySpawnElapsed >= _spawnPolicy.GetSpawnInterval(runState.Stage, activeEnemyCount))
            {
                var enemyData = _difficultyPolicy.GetEnemyData(runState.Stage);
                var request = _spawnPolicy.CreateEnemyRequest(runState.Stage, _randomService, _mapPolicy, enemyData);
                SpawnRequested?.Invoke(request);
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
                SpawnRequest request = SpawnRequest.MedKit(
                    x,
                    y,
                    _itemPolicy.GetMedKitHealAmount(runState.Stage),
                    _itemPolicy.GetMedKitScore(runState.Stage));

                MedKitRequested?.Invoke(request);
                _medKitSpawnElapsed = 0f;
            }
        }

        public void Reset()
        {
            _enemySpawnElapsed = 0f;
            _medKitSpawnElapsed = 0f;
        }
    }
}
