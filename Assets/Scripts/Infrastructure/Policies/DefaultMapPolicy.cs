using System;
using OneDayGame.Domain.Gameplay;
using OneDayGame.Domain.Policies;

namespace OneDayGame.Infrastructure.Policies
{
    public sealed class DefaultMapPolicy : IMapPolicy
    {
        private float _spawnXMin;
        private float _spawnXMax;
        private float _spawnYMin;
        private float _spawnYMax;
        private float _playerMinX;
        private float _playerMaxX;
        private float _playerMinY;
        private float _playerMaxY;

        private readonly IStageProfileProvider _stageProfileProvider;

        public DefaultMapPolicy()
        {
            ApplyProfile(new StageBandSettings().Resolve(1));
        }

        public DefaultMapPolicy(IStageProfileProvider stageProfileProvider, int initialStage)
        {
            _stageProfileProvider = stageProfileProvider ?? new DefaultStageProfileProvider();
            ApplyProfile(_stageProfileProvider.ResolveProfile(Math.Max(1, initialStage)));
        }

        public float SpawnXMin => _spawnXMin;

        public float SpawnXMax => _spawnXMax;

        public float SpawnYMin => _spawnYMin;

        public float SpawnYMax => _spawnYMax;

        public float PlayerMinX => _playerMinX;

        public float PlayerMaxX => _playerMaxX;

        public float PlayerMinY => _playerMinY;

        public float PlayerMaxY => _playerMaxY;

        public void ApplyProfile(StageProfile profile)
        {
            if (profile == null)
            {
                profile = new StageBandSettings().Resolve(1);
            }

            _spawnXMin = profile.SpawnXMin;
            _spawnXMax = profile.SpawnXMax;
            _spawnYMin = profile.SpawnYMin;
            _spawnYMax = profile.SpawnYMax;
            _playerMinX = profile.PlayerMinX;
            _playerMaxX = profile.PlayerMaxX;
            _playerMinY = profile.PlayerMinY;
            _playerMaxY = profile.PlayerMaxY;
        }
    }
}
