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

            if (_spawnXMin > _spawnXMax)
            {
                (_spawnXMin, _spawnXMax) = (_spawnXMax, _spawnXMin);
            }

            if (_spawnYMin > _spawnYMax)
            {
                (_spawnYMin, _spawnYMax) = (_spawnYMax, _spawnYMin);
            }

            if (_playerMinX > _playerMaxX)
            {
                (_playerMinX, _playerMaxX) = (_playerMaxX, _playerMinX);
            }

            if (_playerMinY > _playerMaxY)
            {
                (_playerMinY, _playerMaxY) = (_playerMaxY, _playerMinY);
            }

            if (_playerMaxX - _playerMinX < 0.5f)
            {
                float centerX = (_playerMinX + _playerMaxX) * 0.5f;
                _playerMinX = centerX - 0.25f;
                _playerMaxX = centerX + 0.25f;
            }

            if (_playerMaxY - _playerMinY < 0.5f)
            {
                float centerY = (_playerMinY + _playerMaxY) * 0.5f;
                _playerMinY = centerY - 0.25f;
                _playerMaxY = centerY + 0.25f;
            }
        }
    }
}
