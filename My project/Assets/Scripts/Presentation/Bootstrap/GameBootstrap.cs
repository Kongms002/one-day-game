using System.Collections.Generic;
using OneDayGame.Application;
using OneDayGame.Domain;
using OneDayGame.Domain.Gameplay;
using OneDayGame.Domain.Input;
using OneDayGame.Domain.Policies;
using OneDayGame.Domain.Repositories;
using OneDayGame.Infrastructure.Policies;
using OneDayGame.Infrastructure.Services;
using OneDayGame.Presentation.Gameplay;
using OneDayGame.Presentation.Input;
using OneDayGame.Presentation.Ui;
using UnityEngine;

namespace OneDayGame.Presentation.Bootstrap
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("Gameplay")]
        [SerializeField]
        private RuntimeInputPort _inputPort;

        [SerializeField]
        private PlayerView _playerPrefab;

        [SerializeField]
        private EnemyView _enemyPrefab;

        [SerializeField]
        private MedKitView _medKitPrefab;

        [SerializeField]
        private Transform _enemiesRoot;

        [SerializeField]
        private Transform _medKitRoot;

        [SerializeField]
        private GameHudPresenter _hudPresenter;

        [Header("Runtime")]
        [SerializeField]
        private RunConfig _runConfig = new RunConfig();

        private RunSessionService _runSession;
        private SpawnService _spawnService;
        private IRunRepository _repository;

        private IWeaponPolicy _weaponPolicy;
        private IMapPolicy _mapPolicy;
        private readonly List<EnemyView> _enemies = new List<EnemyView>();
        private readonly List<MedKitView> _medKits = new List<MedKitView>();
        private PlayerView _player;
        private bool _isInitialized;

        private void Awake()
        {
            SetupServices();
            SetupPlayer();

            if (_hudPresenter != null)
            {
                _hudPresenter.Bind(_runSession);
            }

            _runSession.StartRun();
            _isInitialized = true;
        }

        private void SetupServices()
        {
            _repository = new PlayerPrefsRunRepository();

            IDifficultyPolicy difficultyPolicy = new DefaultDifficultyPolicy();
            ISpawnPolicy spawnPolicy = new DefaultSpawnPolicy();
            IItemPolicy itemPolicy = new DefaultItemPolicy();
            var mapPolicy = new DefaultMapPolicy();
            _weaponPolicy = new DefaultWeaponPolicy();

            _runSession = new RunSessionService(_runConfig, difficultyPolicy, _repository);
            _spawnService = new SpawnService(
                spawnPolicy,
                difficultyPolicy,
                itemPolicy,
                mapPolicy,
                new UnityRandomService());
            _mapPolicy = mapPolicy;

            _runSession.RunEnded += OnRunEnded;
            _runSession.Restarted += OnRunRestarted;
            _spawnService.SpawnRequested += OnSpawnRequest;
            _spawnService.MedKitRequested += OnMedKitRequest;
        }

        private void SetupPlayer()
        {
            if (_playerPrefab == null)
            {
                return;
            }

            _player = Instantiate(_playerPrefab, Vector3.zero, Quaternion.identity);
            _player.Initialize(
                _inputPort,
                _runSession,
                _weaponPolicy,
                Mathf.Max(0.1f, _runConfig.PlayerClampSpeed),
                _runConfig.PlayerDamageOnTouchInterval);

            if (_mapPolicy != null)
            {
                _player.SetAreaBounds(_mapPolicy.PlayerMinX, _mapPolicy.PlayerMaxX, _mapPolicy.PlayerMinY, _mapPolicy.PlayerMaxY);
            }
            else
            {
                _player.SetAreaBounds(-7.8f, 7.8f, -4.8f, 4.8f);
            }

            if (_inputPort != null)
            {
                _inputPort.FrameTick += OnInputFrame;
            }

            _player.ResetPosition(new Vector3(_runConfig.PlayerStartX, _runConfig.PlayerStartY, 0f));
        }

        private void Update()
        {
            if (!_isInitialized || _runSession == null)
            {
                return;
            }

            _runSession.Tick(Time.deltaTime);

            if (_runSession.IsDead)
            {
                if (_inputPort != null && (_inputPort.AnyActionPressed || _inputPort.UltimatePressed))
                {
                    RestartRun();
                }

                return;
            }

            _spawnService.Tick(Time.deltaTime, _runSession, _enemies.Count);
        }

        private void OnInputFrame(VectorInputTick tick)
        {
            if (_runSession == null || _player == null || _weaponPolicy == null || _runSession.IsDead || !tick.AnyActionPressed)
            {
                return;
            }

            if (tick.UltimatePressed)
            {
                if (_runSession.TryUseUltimate(_weaponPolicy.GetUltimateCost()))
                {
                    _player.ApplyUltimate(_weaponPolicy.GetPlayerUltimateRadius(_runSession.Stage), _weaponPolicy.GetUltimateMultiplier(_runSession.Stage));
                }
            }
        }

        private void OnSpawnRequest(SpawnRequest request)
        {
            if (_enemyPrefab == null || _enemiesRoot == null)
            {
                return;
            }

            var position = new Vector3(request.X, request.Y, 0f);
            var enemy = Instantiate(_enemyPrefab, position, Quaternion.identity, _enemiesRoot);
            enemy.Initialize(request.EnemyData, _player != null ? _player.transform : transform);
            enemy.EnemyDied += OnEnemyDied;
            _enemies.Add(enemy);
        }

        private void OnMedKitRequest(SpawnRequest request)
        {
            if (_medKitPrefab == null || _medKitRoot == null)
            {
                return;
            }

            var position = new Vector3(request.X, request.Y, 0f);
            var medKit = Instantiate(_medKitPrefab, position, Quaternion.identity, _medKitRoot);
            medKit.Initialize(request.MedKitHealAmount, request.ScoreReward);
            medKit.MedKitCollected += OnMedKitCollected;
            _medKits.Add(medKit);
        }

        private void OnEnemyDied(EnemyView enemy)
        {
            enemy.EnemyDied -= OnEnemyDied;
            if (_enemies.Remove(enemy))
            {
                _runSession.RegisterEnemyKill(enemy.ScoreValue);
            }
        }

        private void OnMedKitCollected(MedKitView medKit)
        {
            if (_medKits.Remove(medKit))
            {
                _runSession.RegisterHeal(medKit.HealAmount);
                if (medKit.ScoreReward > 0)
                {
                    _runSession.RegisterEnemyKill(medKit.ScoreReward);
                }
            }

            medKit.MedKitCollected -= OnMedKitCollected;
        }

        private void OnRunEnded(RunSnapshot snapshot)
        {
            _spawnService.Reset();
        }

        private void OnRunRestarted(RunSnapshot snapshot)
        {
            foreach (var enemy in _enemies)
            {
                if (enemy != null)
                {
                    enemy.EnemyDied -= OnEnemyDied;
                    Destroy(enemy.gameObject);
                }
            }

            foreach (var medKit in _medKits)
            {
                if (medKit != null)
                {
                    medKit.MedKitCollected -= OnMedKitCollected;
                    Destroy(medKit.gameObject);
                }
            }

            _enemies.Clear();
            _medKits.Clear();

            if (_player != null)
            {
                _player.ResetPosition(new Vector3(_runConfig.PlayerStartX, _runConfig.PlayerStartY, 0f));
            }

            _spawnService.Reset();
        }

        private void RestartRun()
        {
            _runSession.Restart();
        }

        private void OnDestroy()
        {
            if (_runSession != null)
            {
                _runSession.RunEnded -= OnRunEnded;
                _runSession.Restarted -= OnRunRestarted;
            }

            if (_spawnService != null)
            {
                _spawnService.SpawnRequested -= OnSpawnRequest;
                _spawnService.MedKitRequested -= OnMedKitRequest;
            }

            if (_inputPort != null)
            {
                _inputPort.FrameTick -= OnInputFrame;
            }
        }
    }
}
