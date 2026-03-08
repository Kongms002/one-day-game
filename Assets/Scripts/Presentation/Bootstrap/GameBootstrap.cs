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
        private ExpOrbView _expOrbPrefab;

        [SerializeField]
        private Transform _enemiesRoot;

        [SerializeField]
        private Transform _medKitRoot;

        [SerializeField]
        private Transform _expOrbRoot;

        [SerializeField]
        private GameHudPresenter _hudPresenter;

        [SerializeField]
        private RoundMapView _roundMapView;

        [Header("Runtime")]
        [SerializeField]
        private RunConfig _runConfig = new RunConfig();

        [SerializeField]
        private StageConfig _stageProfileConfig;

        [SerializeField]
        private bool _enableUltimate;

        [SerializeField]
        [Min(0f)]
        private float _autoRestartDelay = 0f;

        [SerializeField]
        private bool _showEnemyHpBarsInDev = true;

        private RunSessionService _runSession;
        private SpawnService _spawnService;
        private IRunRepository _repository;

        private IWeaponPolicy _weaponPolicy;
        private IMapPolicy _mapPolicy;
        private IDifficultyPolicy _difficultyPolicy;
        private readonly List<EnemyView> _enemies = new List<EnemyView>();
        private readonly List<MedKitView> _medKits = new List<MedKitView>();
        private readonly List<ExpOrbView> _expOrbs = new List<ExpOrbView>();
        private PlayerView _player;
        private bool _isInitialized;
        private float _deadElapsed;
        private bool _restartQueued;
        private int _lastAppliedStage;
        private int _pendingLevelUps;
        private bool _isLevelUpPaused;
        private IStageProfileProvider _stageProfileProvider;

        private void Awake()
        {
            EnsureRuntimeReferences();
            SetupServices();
            SetupPlayer();
            EnemyView.SetHpBarVisible(_showEnemyHpBarsInDev);

            if (_hudPresenter != null)
            {
                _hudPresenter.Bind(_runSession);
                _hudPresenter.BindWeapon(_weaponPolicy);
            }

            _runSession.StartRun();
            Time.timeScale = 1f;
            _deadElapsed = 0f;
            _restartQueued = false;
            _lastAppliedStage = _runSession.Stage;
            EnsureRoundMap();
            _roundMapView?.ResetToStage(_runSession.Stage);
            _isInitialized = true;
        }

        private void SetupServices()
        {
            _repository = new PlayerPrefsRunRepository();

            _stageProfileProvider = _stageProfileConfig != null
                ? (IStageProfileProvider) _stageProfileConfig
                : new DefaultStageProfileProvider();
            int startStage = Mathf.Max(1, _runConfig.InitialStage);
            var startProfile = GetProfile(startStage);

            _difficultyPolicy = new DefaultDifficultyPolicy(_stageProfileProvider);
            ISpawnPolicy spawnPolicy = new DefaultSpawnPolicy(_stageProfileProvider);
            IItemPolicy itemPolicy = new DefaultItemPolicy();
            var mapPolicy = new DefaultMapPolicy(_stageProfileProvider, startProfile.Stage);
            _weaponPolicy = new DefaultWeaponPolicy(_stageProfileProvider);

            _runSession = new RunSessionService(_runConfig, _difficultyPolicy, _repository);
            _spawnService = new SpawnService(
                spawnPolicy,
                _difficultyPolicy,
                itemPolicy,
                mapPolicy,
                new UnityRandomService());
            _mapPolicy = mapPolicy;

            _runSession.RunEnded += OnRunEnded;
            _runSession.Restarted += OnRunRestarted;
            _runSession.SnapshotChanged += OnSnapshotChanged;
            _runSession.LevelUpTriggered += OnLevelUpTriggered;
            _spawnService.SpawnRequested += OnSpawnRequest;
            _spawnService.MedKitRequested += OnMedKitRequest;

            ApplyProfileToMapPolicy(startProfile.Stage);
        }

        private void SetupPlayer()
        {
            float initialMoveSpeed = Mathf.Max(0.1f, _runConfig.PlayerClampSpeed);
            if (_difficultyPolicy != null && _runSession != null)
            {
                var enemyData = _difficultyPolicy.GetEnemyData(_runSession.Stage);
                initialMoveSpeed = Mathf.Max(0.1f, enemyData.MoveSpeed * 1.1f);
            }

            if (_playerPrefab != null)
            {
                _player = Instantiate(_playerPrefab, Vector3.zero, Quaternion.identity);
            }
            else
            {
                var runtimePlayer = new GameObject("PlayerRuntime");
                _player = runtimePlayer.AddComponent<PlayerView>();
            }

            _player.Initialize(
                _inputPort,
                _runSession,
                _weaponPolicy,
                initialMoveSpeed,
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
            SetupCameraFollow();
        }

        private void SetupCameraFollow()
        {
            if (_player == null)
            {
                return;
            }

            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            var follow = mainCamera.GetComponent<CameraFollow2D>();
            if (follow == null)
            {
                follow = mainCamera.gameObject.AddComponent<CameraFollow2D>();
            }

            follow.SetTarget(_player.transform);
        }

        private void Update()
        {
            if (!_isInitialized || _runSession == null)
            {
                return;
            }

            _runSession.Tick(Time.deltaTime);

            if (_isLevelUpPaused)
            {
                return;
            }

            if (_runSession.IsDead)
            {
                _deadElapsed += Time.deltaTime;
                if (!_restartQueued && _deadElapsed >= _autoRestartDelay)
                {
                    _restartQueued = true;
                    RestartRun();
                }

                return;
            }

            _deadElapsed = 0f;
            _restartQueued = false;

            CleanupDestroyedViews();
            _spawnService.Tick(Time.deltaTime, _runSession, _enemies.Count);
        }

        private void OnInputFrame(VectorInputTick tick)
        {
            if (!_enableUltimate || _runSession == null || _player == null || _weaponPolicy == null || _runSession.IsDead)
            {
                return;
            }

            if (tick.UltimatePressed)
            {
                if (_runSession.TryUseUltimate(_weaponPolicy.GetUltimateCost(_runSession.Stage)))
                {
                    _player.ApplyUltimate(_weaponPolicy.GetPlayerUltimateRadius(_runSession.Stage), _weaponPolicy.GetUltimateMultiplier(_runSession.Stage));
                }
            }
        }

        private void OnSpawnRequest(SpawnRequest request)
        {
            if (_enemiesRoot == null)
            {
                var root = GameObject.Find("EnemiesRoot") ?? new GameObject("EnemiesRoot");
                _enemiesRoot = root.transform;
            }

            _runSession?.RegisterEnemySpawn();

            var position = new Vector3(request.X, request.Y, 0f);
            EnemyView enemy;
            if (_enemyPrefab != null)
            {
                enemy = Instantiate(_enemyPrefab, position, Quaternion.identity, _enemiesRoot);
            }
            else
            {
                var runtimeEnemy = new GameObject("EnemyRuntime");
                runtimeEnemy.transform.SetParent(_enemiesRoot, false);
                runtimeEnemy.transform.position = position;
                enemy = runtimeEnemy.AddComponent<EnemyView>();
            }

            enemy.Initialize(request.EnemyData, _player != null ? _player.transform : transform);
            enemy.EnemyDied += OnEnemyDied;
            _enemies.Add(enemy);
        }

        private void OnMedKitRequest(SpawnRequest request)
        {
            if (_medKitRoot == null)
            {
                var root = GameObject.Find("MedKitRoot") ?? new GameObject("MedKitRoot");
                _medKitRoot = root.transform;
            }

            var position = new Vector3(request.X, request.Y, 0f);
            MedKitView medKit;
            if (_medKitPrefab != null)
            {
                medKit = Instantiate(_medKitPrefab, position, Quaternion.identity, _medKitRoot);
            }
            else
            {
                var runtimeMedKit = new GameObject("MedKitRuntime");
                runtimeMedKit.transform.SetParent(_medKitRoot, false);
                runtimeMedKit.transform.position = position;
                var trigger = runtimeMedKit.AddComponent<CircleCollider2D>();
                trigger.isTrigger = true;
                medKit = runtimeMedKit.AddComponent<MedKitView>();
            }

            medKit.Initialize(request.MedKitHealAmount, request.ScoreReward);
            medKit.MedKitCollected += OnMedKitCollected;
            _medKits.Add(medKit);
        }

        private void SpawnExperienceOrb(Vector3 position, int expValue)
        {
            if (_player == null)
            {
                return;
            }

            if (_expOrbPrefab == null)
            {
                var runtimeOrb = new GameObject("ExpOrb").AddComponent<ExpOrbView>();
                runtimeOrb.transform.position = position;
                runtimeOrb.Initialize(expValue, _player.transform);
                runtimeOrb.Collected += OnExpOrbCollected;
                _expOrbs.Add(runtimeOrb);
                return;
            }

            if (_expOrbRoot == null)
            {
                var root = GameObject.Find("ExpOrbRoot") ?? new GameObject("ExpOrbRoot");
                _expOrbRoot = root.transform;
            }

            var expOrb = Instantiate(_expOrbPrefab, position, Quaternion.identity, _expOrbRoot);
            expOrb.Initialize(expValue, _player.transform);
            expOrb.Collected += OnExpOrbCollected;
            _expOrbs.Add(expOrb);
        }

        private void OnEnemyDied(EnemyView enemy)
        {
            enemy.EnemyDied -= OnEnemyDied;
            if (_enemies.Remove(enemy))
            {
                _runSession.RegisterEnemyKill(enemy.ScoreValue);
                SpawnExperienceOrb(enemy.transform.position, Mathf.Max(1, enemy.ScoreValue / 3));
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

        private void OnExpOrbCollected(ExpOrbView expOrb, int expValue)
        {
            if (_expOrbs.Remove(expOrb))
            {
                _runSession.RegisterExperience(expValue);
            }

            expOrb.Collected -= OnExpOrbCollected;
        }

        private void OnRunEnded(RunSnapshot snapshot)
        {
            _spawnService.Reset();
            _isLevelUpPaused = false;
            _pendingLevelUps = 0;
            Time.timeScale = 1f;
            _hudPresenter?.HideLevelUpChoices();
        }

        private void OnLevelUpTriggered(int level)
        {
            _pendingLevelUps++;
            TryOpenLevelUpPanel(level);
        }

        private void TryOpenLevelUpPanel(int level)
        {
            if (_isLevelUpPaused)
            {
                return;
            }

            if (_hudPresenter == null)
            {
                _pendingLevelUps = Mathf.Max(0, _pendingLevelUps - 1);
                return;
            }

            if (_runSession == null)
            {
                _pendingLevelUps = Mathf.Max(0, _pendingLevelUps - 1);
                return;
            }

            _isLevelUpPaused = true;
            Time.timeScale = 0f;
            bool opened = _hudPresenter.ShowLevelUpChoices(
                level,
                "Power +20%",
                "Attack Speed +15%",
                "Max HP +20",
                ApplyUpgradeChoice);

            if (!opened)
            {
                _isLevelUpPaused = false;
                Time.timeScale = 1f;
                ApplyUpgradeChoice(0);
            }
        }

        private void ApplyUpgradeChoice(int choiceIndex)
        {
            if (_player == null || _runSession == null)
            {
                _isLevelUpPaused = false;
                Time.timeScale = 1f;
                return;
            }

            switch (choiceIndex)
            {
                case 0:
                    _player.ApplyDamageMultiplier(1.2f);
                    break;
                case 1:
                    _player.ApplyAttackSpeedMultiplier(1.15f);
                    break;
                case 2:
                    _runSession.ApplyMaxHpUpgrade(20f);
                    break;
            }

            _pendingLevelUps = Mathf.Max(0, _pendingLevelUps - 1);
            _isLevelUpPaused = false;
            Time.timeScale = 1f;

            if (_pendingLevelUps > 0)
            {
                TryOpenLevelUpPanel(_runSession.Level);
            }
        }

        private void OnSnapshotChanged(RunSnapshot snapshot)
        {
            ApplyProfileToMapPolicy(snapshot.Stage);

            if (snapshot.Stage == _lastAppliedStage)
            {
                return;
            }

            _lastAppliedStage = snapshot.Stage;
            EnsureRoundMap();
            _roundMapView?.ApplyForStage(snapshot.Stage);
        }

        private void OnRunRestarted(RunSnapshot snapshot)
        {
            ApplyProfileToMapPolicy(snapshot.Stage);
            _deadElapsed = 0f;
            _restartQueued = false;
            _lastAppliedStage = snapshot.Stage;
            _pendingLevelUps = 0;
            _isLevelUpPaused = false;
            Time.timeScale = 1f;
            _hudPresenter?.HideLevelUpChoices();

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

            foreach (var expOrb in _expOrbs)
            {
                if (expOrb != null)
                {
                    expOrb.Collected -= OnExpOrbCollected;
                    Destroy(expOrb.gameObject);
                }
            }

            _enemies.Clear();
            _medKits.Clear();
            _expOrbs.Clear();

            if (_player != null)
            {
                _player.ResetPosition(new Vector3(_runConfig.PlayerStartX, _runConfig.PlayerStartY, 0f));
            }

            _spawnService.Reset();
            EnsureRoundMap();
            _roundMapView?.ResetToStage(snapshot.Stage);
        }

        private void RestartRun()
        {
            _runSession.Restart();
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;

            if (_runSession != null)
            {
                _runSession.RunEnded -= OnRunEnded;
                _runSession.Restarted -= OnRunRestarted;
                _runSession.SnapshotChanged -= OnSnapshotChanged;
                _runSession.LevelUpTriggered -= OnLevelUpTriggered;
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

            foreach (var expOrb in _expOrbs)
            {
                if (expOrb != null)
                {
                    expOrb.Collected -= OnExpOrbCollected;
                }
            }
        }

        private void CleanupDestroyedViews()
        {
            _enemies.RemoveAll(enemy => enemy == null);
            _medKits.RemoveAll(medKit => medKit == null);
            _expOrbs.RemoveAll(expOrb => expOrb == null);
        }

        private void EnsureRoundMap()
        {
            if (_roundMapView == null)
            {
                var mapRoot = GameObject.Find("RoundMap") ?? new GameObject("RoundMap");
                _roundMapView = mapRoot.GetComponent<RoundMapView>();
                if (_roundMapView == null)
                {
                    _roundMapView = mapRoot.AddComponent<RoundMapView>();
                }
            }

            _roundMapView.Initialize(_mapPolicy);
        }

        private void ApplyProfileToMapPolicy(int stage)
        {
            if (_mapPolicy == null)
            {
                return;
            }

            if (_mapPolicy is DefaultMapPolicy mapPolicy)
            {
                mapPolicy.ApplyProfile(GetProfile(stage));
                if (_player != null)
                {
                    _player.SetAreaBounds(
                        mapPolicy.PlayerMinX,
                        mapPolicy.PlayerMaxX,
                        mapPolicy.PlayerMinY,
                        mapPolicy.PlayerMaxY);
                }

                _roundMapView?.Initialize(_mapPolicy);
            }
        }

        private StageProfile GetProfile(int stage)
        {
            if (_stageProfileProvider == null)
            {
                return new DefaultStageProfileProvider().ResolveProfile(Mathf.Max(1, stage));
            }

            return _stageProfileProvider.ResolveProfile(Mathf.Max(1, stage));
        }

        private void EnsureRuntimeReferences()
        {
            if (_inputPort == null)
            {
                _inputPort = FindObjectOfType<RuntimeInputPort>();
            }

            if (_inputPort == null)
            {
                var inputObject = new GameObject("RuntimeInputPort");
                _inputPort = inputObject.AddComponent<RuntimeInputPort>();
            }

            _inputPort.AutoBindFallbackJoysticks();

            if (!_enableUltimate)
            {
                var ultimateButtons = FindObjectsOfType<UltimatePressButton>(includeInactive: true);
                for (int i = 0; i < ultimateButtons.Length; i++)
                {
                    var button = ultimateButtons[i];
                    if (button != null)
                    {
                        button.gameObject.SetActive(false);
                    }
                }
            }
        }
    }
}
