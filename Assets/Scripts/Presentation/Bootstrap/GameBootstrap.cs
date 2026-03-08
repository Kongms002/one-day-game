using System.Collections.Generic;
using OneDayGame.Application;
using OneDayGame.Domain;
using OneDayGame.Domain.Gameplay;
using OneDayGame.Domain.Input;
using OneDayGame.Domain.Policies;
using OneDayGame.Domain.Randomness;
using OneDayGame.Domain.Repositories;
using OneDayGame.Domain.Weapons;
using OneDayGame.Infrastructure.Policies;
using OneDayGame.Infrastructure.Services;
using OneDayGame.Presentation.Gameplay;
using OneDayGame.Presentation.Input;
using OneDayGame.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace OneDayGame.Presentation.Bootstrap
{
    [DefaultExecutionOrder(-500)]
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
        private Texture2D _enemyStateSheet;

        [SerializeField]
        private Sprite _enemyMoveFrameASprite;

        [SerializeField]
        private Sprite _enemyMoveFrameBSprite;

        [SerializeField]
        private Sprite _enemyHitFrameSprite;

        [SerializeField]
        private Sprite _enemyDeathFrameSprite;

        [SerializeField]
        [Min(0.04f)]
        private float _enemyMoveFrameInterval = 0.14f;

        [SerializeField]
        [Min(0.02f)]
        private float _enemyHitFrameDuration = 0.08f;

        [SerializeField]
        [Min(0.08f)]
        private float _enemyDeathFrameDuration = 0.35f;

        [SerializeField]
        private MedKitView _medKitPrefab;

        [SerializeField]
        private MagnetPickupView _magnetPickupPrefab;

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
        private bool _showEnemyHpBarsInDev = true;

        private RunSessionService _runSession;
        private SpawnService _spawnService;
        private IRunRepository _repository;

        private IWeaponPolicy _weaponPolicy;
        private WeaponLoadoutService _weaponLoadout;
        private IMapPolicy _mapPolicy;
        private IDifficultyPolicy _difficultyPolicy;
        private IRandomService _randomService;
        private readonly List<EnemyView> _enemies = new List<EnemyView>();
        private readonly List<MedKitView> _medKits = new List<MedKitView>();
        private readonly List<MagnetPickupView> _magnetItems = new List<MagnetPickupView>();
        private readonly List<ExpOrbView> _expOrbs = new List<ExpOrbView>();
        private EnemyFactory _enemyFactory;
        private ExpOrbFactory _expOrbFactory;
        private PlayerView _player;
        private Sprite _enemyMoveFrameA;
        private Sprite _enemyMoveFrameB;
        private Sprite _enemyHitFrame;
        private Sprite _enemyDeathFrame;
        private int _enemySpawnSerial;
        private bool _isInitialized;
        private float _deadElapsed;
        private int _lastAppliedStage;
        private int _pendingLevelUps;
        private bool _isLevelUpPaused;
        private IStageProfileProvider _stageProfileProvider;
        private WeaponUpgradeRuleService _upgradeRuleService;
        private WeaponUpgradeRule[] _upgradeChoices = new WeaponUpgradeRule[3];
        private Text _runtimeInputDebugText;
        private VectorInputTick _debugInputTick;
        private bool _debugHasInputTick;
        private float _nextDebugLogAt;
        private const float DebugLogInterval = 0.25f;

        private void Awake()
        {
            EnsureRuntimeReferences();
            EnsureRuntimeInputDebug();
            SetupServices();
            SetupFactories();
            SetupPlayer();
            EnemyView.SetHpBarVisible(_showEnemyHpBarsInDev);
            _enemySpawnSerial = 0;
            BuildEnemyStateFrames();

            if (_hudPresenter != null)
            {
                _hudPresenter.Bind(_runSession);
                _hudPresenter.BindWeapon(_weaponPolicy);
                _hudPresenter.BindWeaponLoadout(_weaponLoadout);
                _hudPresenter.RestartRequested += RestartRun;
            }

            _runSession.StartRun();
            Time.timeScale = 1f;
            _deadElapsed = 0f;
            _lastAppliedStage = _runSession.Stage;
            _enemySpawnSerial = 0;
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
            _weaponLoadout = WeaponLoadoutService.CreateDefault();
            _upgradeRuleService = new WeaponUpgradeRuleService();

            _runSession = new RunSessionService(_runConfig, _difficultyPolicy, _repository);
            _randomService = new UnityRandomService();
            _spawnService = new SpawnService(
                spawnPolicy,
                _difficultyPolicy,
                itemPolicy,
                mapPolicy,
                _randomService,
                _stageProfileProvider);
            _mapPolicy = mapPolicy;

            _runSession.RunEnded += OnRunEnded;
            _runSession.Restarted += OnRunRestarted;
            _runSession.SnapshotChanged += OnSnapshotChanged;
            _runSession.LevelUpTriggered += OnLevelUpTriggered;
            _spawnService.SpawnRequested += OnSpawnRequest;
            _spawnService.MedKitRequested += OnMedKitRequest;

            ApplyProfileToMapPolicy(startProfile.Stage);
        }

        private void EnsureRuntimeInputDebug()
        {
            if (!UnityEngine.Application.isPlaying)
            {
                return;
            }

            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasGo = new GameObject("RuntimeInputDebugCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.overrideSorting = true;
                canvas.sortingOrder = 99999;
                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
            }

            if (_runtimeInputDebugText != null)
            {
                return;
            }

            var overlay = canvas.transform.Find("RuntimeInputDebugOverlay");
            if (overlay != null && !overlay.gameObject.scene.IsValid())
            {
                overlay = null;
            }

            if (overlay == null)
            {
                var overlayGo = new GameObject("RuntimeInputDebugOverlay");
                overlay = overlayGo.transform;
                overlay.SetParent(canvas.transform, false);

                var overlayRect = overlayGo.AddComponent<RectTransform>();
                overlayRect.anchorMin = new Vector2(1f, 0f);
                overlayRect.anchorMax = new Vector2(1f, 0f);
                overlayRect.pivot = new Vector2(1f, 0f);
                overlayRect.anchoredPosition = new Vector2(-12f, 12f);
                overlayRect.sizeDelta = new Vector2(340f, 120f);

                var overlayBg = overlayGo.AddComponent<Image>();
                overlayBg.color = new Color(0f, 0f, 0f, 0.45f);
                overlayBg.raycastTarget = false;
            }

            if (overlay == null)
            {
                return;
            }

            var rootRect = overlay.GetComponent<RectTransform>();
            if (rootRect == null)
            {
                rootRect = overlay.gameObject.AddComponent<RectTransform>();
                rootRect.anchorMin = new Vector2(1f, 0f);
                rootRect.anchorMax = new Vector2(1f, 0f);
                rootRect.pivot = new Vector2(1f, 0f);
                rootRect.anchoredPosition = new Vector2(-12f, 12f);
                rootRect.sizeDelta = new Vector2(340f, 120f);
            }

            var label = overlay.Find("InputDebugLabel");
            if (label == null)
            {
                var labelObj = new GameObject("InputDebugLabel");
                labelObj.transform.SetParent(overlay, false);
                var labelRect = labelObj.AddComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(10f, 8f);
                labelRect.offsetMax = new Vector2(-10f, -8f);

                label = labelObj.transform;
                _runtimeInputDebugText = labelObj.AddComponent<Text>();
            }
            else
            {
                _runtimeInputDebugText = label.GetComponent<Text>();
                if (_runtimeInputDebugText == null)
                {
                    _runtimeInputDebugText = label.gameObject.AddComponent<Text>();
                }
            }

            _runtimeInputDebugText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _runtimeInputDebugText.fontSize = 14;
            _runtimeInputDebugText.alignment = TextAnchor.LowerLeft;
            _runtimeInputDebugText.color = Color.yellow;
            _runtimeInputDebugText.raycastTarget = false;
            _runtimeInputDebugText.text = "Input Debug: waiting...";
            _runtimeInputDebugText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _runtimeInputDebugText.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private void SetupFactories()
        {
            EnsureEntityRoots();
            EnsureFactoryPrefabs();
            _enemyFactory = new EnemyFactory(_enemyPrefab, _enemiesRoot);
            _expOrbFactory = new ExpOrbFactory(_expOrbPrefab, _expOrbRoot);
        }

        private void EnsureFactoryPrefabs()
        {
            if (_expOrbPrefab == null)
            {
                var runtimeOrbPrefab = new GameObject("ExpOrbPrefabRuntime");
                runtimeOrbPrefab.SetActive(false);
                runtimeOrbPrefab.transform.SetParent(_expOrbRoot, false);
                runtimeOrbPrefab.AddComponent<CircleCollider2D>().isTrigger = true;
                _expOrbPrefab = runtimeOrbPrefab.AddComponent<ExpOrbView>();
            }

            if (_enemyPrefab == null)
            {
                Debug.LogError("[OneDayGame] Enemy prefab is missing. Assign Assets/Prefabs/Enemy.prefab.");
            }
        }

        private void SetupPlayer()
        {
            float initialMoveSpeed = Mathf.Max(0.1f, _runConfig.PlayerClampSpeed);
            if (_difficultyPolicy != null && _runSession != null)
            {
                var enemyData = _difficultyPolicy.GetEnemyData(_runSession.Stage);
                initialMoveSpeed = Mathf.Max(0.1f, enemyData.MoveSpeed * 1.1f);
            }

            var existingPlayer = Object.FindFirstObjectByType<PlayerView>(FindObjectsInactive.Include);
            if (existingPlayer != null && existingPlayer.gameObject.scene.IsValid())
            {
                _player = existingPlayer;
            }

            global::PlayerController legacyPlayerToReuse = null;

            if (_player == null && _playerPrefab != null)
            {
                _player = Instantiate(_playerPrefab, Vector3.zero, Quaternion.identity);
                _player.name = "Player";
            }
            else if (_player == null)
            {
                var legacyPlayerControllers = Object.FindObjectsByType<global::PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                var legacyPlayer = legacyPlayerControllers.Length > 0 ? legacyPlayerControllers[0] : null;
                legacyPlayerToReuse = legacyPlayer;
                if (legacyPlayer != null)
                {
                    _player = legacyPlayer.GetComponent<PlayerView>();
                    if (_player == null)
                    {
                        _player = legacyPlayer.gameObject.AddComponent<PlayerView>();
                    }
                }
                else
                {
                    var runtimePlayer = new GameObject("PlayerRuntime");
                    _player = runtimePlayer.AddComponent<PlayerView>();
                }
            }

            var players = Object.FindObjectsByType<PlayerView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < players.Length; i++)
            {
                var candidate = players[i];
                if (candidate == null || candidate == _player)
                {
                    continue;
                }

                Destroy(candidate.gameObject);
            }

            var runtimeLegacyPlayers = Object.FindObjectsByType<global::PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < runtimeLegacyPlayers.Length; i++)
            {
                var legacy = runtimeLegacyPlayers[i];
                if (legacy != null)
                {
                    if (legacy == legacyPlayerToReuse)
                    {
                        legacy.enabled = false;
                        continue;
                    }

                    legacy.gameObject.SetActive(false);
                    legacy.enabled = false;
                }
            }

            _player.Initialize(
                _inputPort,
                _runSession,
                _weaponPolicy,
                initialMoveSpeed,
                _runConfig.PlayerDamageOnTouchInterval);
            _player.BindWeaponLoadout(_weaponLoadout);
            _player.BindWalkableResolver(IsWalkablePosition);

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

            _player.ResetPosition(GetMapCenterPosition());
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

            RefreshRuntimeInputDebug();
            if (_player != null && _inputPort != null)
            {
                _player.SetInputAxis(_inputPort.MoveAxis);
            }

            _runSession.Tick(Time.deltaTime);

            if (_isLevelUpPaused)
            {
                return;
            }

            if (_runSession.IsDead)
            {
                _deadElapsed += Time.deltaTime;
                return;
            }

            _deadElapsed = 0f;

            CleanupDestroyedViews();
            _spawnService.Tick(Time.deltaTime, _runSession, _enemies.Count);
        }

        private void OnInputFrame(VectorInputTick tick)
        {
            if (_player != null)
            {
                _player.SetInputAxis(tick.MoveAxis);
            }

            _debugHasInputTick = true;
            _debugInputTick = tick;
            if (Time.unscaledTime >= _nextDebugLogAt)
            {
                var runtimeInput = _inputPort as RuntimeInputPort;
                var joystickReady = runtimeInput != null && runtimeInput.HasActiveJoystick;
                Debug.Log($"[InputTick] move=({tick.MoveAxis.X:0.00},{tick.MoveAxis.Y:0.00}) ultimate={tick.UltimatePressed} action={tick.AnyActionPressed} joystickReady={joystickReady}");
                _nextDebugLogAt = Time.unscaledTime + DebugLogInterval;
            }

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

        private void RefreshRuntimeInputDebug()
        {
            if (_runtimeInputDebugText == null)
            {
                return;
            }

            VectorInputTick tick = _debugHasInputTick ? _debugInputTick : new VectorInputTick(_inputPort != null ? _inputPort.MoveAxis : InputAxis.Zero, false, false);
            var runtimeInput = _inputPort as RuntimeInputPort;
            string joystickReady = runtimeInput == null
                ? "n/a"
                : (runtimeInput.HasActiveJoystick ? "OK" : "NoJoystick");
            string moveSource = _debugHasInputTick ? "FrameTick" : "Port";
            string playerPos = _player != null ? _player.transform.position.ToString("F2") : "(none)";

            _runtimeInputDebugText.text =
                $"[Input Debug]\n" +
                $"move({moveSource}): X={tick.MoveAxis.X:0.00}, Y={tick.MoveAxis.Y:0.00}\n" +
                $"runStarted:{_runSession != null && !_runSession.IsDead} sessionAlive:{_runSession != null && _runSession.IsDead == false}\n" +
                $"joystick: {joystickReady}\n" +
                $"player: {(_player != null ? _player.name : "null")}\n" +
                $"playerPos: {playerPos}";
        }

        private void OnSpawnRequest(SpawnRequest request)
        {
            EnsureEntityRoots();
            if (_enemyFactory == null)
            {
                _enemyFactory = new EnemyFactory(_enemyPrefab, _enemiesRoot);
            }

            _runSession?.RegisterEnemySpawn();

            var position = new Vector3(request.X, request.Y, 0f);
            var enemy = _enemyFactory.Spawn(position);
            if (enemy == null)
            {
                return;
            }

            enemy.SetDestroyOnDeath(false);
            enemy.Initialize(request.EnemyData, _player != null ? _player.transform : transform);
            _enemySpawnSerial++;
            ApplyEnemyStateAnimation(enemy, _enemySpawnSerial);
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
            if (request.Kind == SpawnKind.MagnetPickup)
            {
                MagnetPickupView magnet;
                if (_magnetPickupPrefab != null)
                {
                    magnet = Instantiate(_magnetPickupPrefab, position, Quaternion.identity, _medKitRoot);
                }
                else
                {
                    var runtimeMagnet = new GameObject("MagnetPickupRuntime");
                    runtimeMagnet.transform.SetParent(_medKitRoot, false);
                    runtimeMagnet.transform.position = position;
                    var trigger = runtimeMagnet.AddComponent<CircleCollider2D>();
                    trigger.isTrigger = true;
                    magnet = runtimeMagnet.AddComponent<MagnetPickupView>();
                }

                magnet.Initialize(request.MagnetDuration, request.MagnetRadius);
                magnet.Collected += OnMagnetCollected;
                _magnetItems.Add(magnet);
                return;
            }

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

            EnsureEntityRoots();
            if (_expOrbFactory == null)
            {
                _expOrbFactory = new ExpOrbFactory(_expOrbPrefab, _expOrbRoot);
            }

            var expOrb = _expOrbFactory.Spawn(position);
            if (expOrb == null)
            {
                return;
            }

            expOrb.SetDestroyOnCollected(false);
            expOrb.Initialize(expValue, _player.transform);
            expOrb.Collected += OnExpOrbCollected;
            expOrb.Released += OnExpOrbReleased;
            _expOrbs.Add(expOrb);
        }

        private void OnEnemyDied(EnemyView enemy)
        {
            enemy.EnemyDied -= OnEnemyDied;
            if (_enemies.Remove(enemy))
            {
                _runSession.RegisterEnemyKill(enemy.ScoreValue);
                SpawnExperienceOrb(enemy.transform.position, Mathf.Max(1, enemy.ScoreValue / 3));

                if (enemy.Archetype == EnemyArchetype.Multiply)
                {
                    SpawnSplitEnemies(enemy.transform.position, enemy.Data);
                }

                _enemyFactory?.Release(enemy);
            }
        }

        private void SpawnSplitEnemies(Vector3 origin, EnemyData source)
        {
            if (_enemyFactory == null || _player == null)
            {
                return;
            }

            for (int i = 0; i < 2; i++)
            {
                var offset = new Vector3((i == 0 ? -1f : 1f) * 0.42f, 0.18f, 0f);
                var split = _enemyFactory.Spawn(origin + offset);
                if (split == null)
                {
                    continue;
                }

                var splitData = new EnemyData(
                    Mathf.Max(1f, source.MaxHp * 0.55f),
                    Mathf.Max(0.6f, source.MoveSpeed * 1.08f),
                    Mathf.Max(1f, source.ContactDamage * 0.9f),
                    Mathf.Max(1, source.ScoreValue / 2),
                    Mathf.Max(0.12f, source.ContactRadius * 0.82f),
                    EnemyArchetype.Swarm,
                    false);

                split.SetDestroyOnDeath(false);
                split.Initialize(splitData, _player.transform);
                ApplyEnemyStateAnimation(split, ++_enemySpawnSerial);
                split.EnemyDied += OnEnemyDied;
                _enemies.Add(split);
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

        private void OnMagnetCollected(MagnetPickupView magnet)
        {
            if (_magnetItems.Remove(magnet) && _player != null)
            {
                _player.ActivateExpMagnet(magnet.Duration, magnet.Radius);
            }

            if (magnet != null)
            {
                magnet.Collected -= OnMagnetCollected;
            }
        }

        private void OnExpOrbCollected(ExpOrbView expOrb, int expValue)
        {
            if (_expOrbs.Remove(expOrb))
            {
                _runSession.RegisterExperience(expValue);
            }

            expOrb.Collected -= OnExpOrbCollected;
            expOrb.Released -= OnExpOrbReleased;
            _expOrbFactory?.Release(expOrb);
        }

        private void OnExpOrbReleased(ExpOrbView expOrb)
        {
            if (_expOrbs.Remove(expOrb))
            {
                expOrb.Collected -= OnExpOrbCollected;
                expOrb.Released -= OnExpOrbReleased;
                _expOrbFactory?.Release(expOrb);
            }
        }

        private void OnRunEnded(RunSnapshot snapshot)
        {
            _spawnService.Reset();
            _isLevelUpPaused = false;
            _pendingLevelUps = 0;
            Time.timeScale = 1f;
            _hudPresenter?.HideLevelUpChoices();
            if (_runSession != null)
            {
                Debug.Log($"[OneDayGame][RunSummary] score={snapshot.Score}, stage={snapshot.Stage}, kills={_runSession.TotalKills}, damageTaken={_runSession.TotalDamageTaken:F0}, survival={snapshot.ElapsedTime:F1}s");
            }
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
            BuildUpgradeChoices();
            bool opened = _hudPresenter.ShowLevelUpChoices(
                level,
                GetUpgradeChoiceLabel(_upgradeChoices[0]),
                GetUpgradeChoiceLabel(_upgradeChoices[1]),
                GetUpgradeChoiceLabel(_upgradeChoices[2]),
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
                    ApplyUpgradeByChoice(_upgradeChoices[0]);
                    break;
                case 1:
                    ApplyUpgradeByChoice(_upgradeChoices[1]);
                    break;
                case 2:
                    ApplyUpgradeByChoice(_upgradeChoices[2]);
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

            var randomMapGen = FindObjectOfType<RandomMapGenerator>();
            if (randomMapGen != null)
            {
                randomMapGen.UpdateStage(snapshot.Stage);
            }
        }

        private void OnRunRestarted(RunSnapshot snapshot)
        {
            ApplyProfileToMapPolicy(snapshot.Stage);
            _deadElapsed = 0f;
            _lastAppliedStage = snapshot.Stage;
            _enemySpawnSerial = 0;
            _pendingLevelUps = 0;
            _isLevelUpPaused = false;
            Time.timeScale = 1f;
            _hudPresenter?.HideLevelUpChoices();

            foreach (var enemy in _enemies)
            {
                if (enemy != null)
                {
                    enemy.EnemyDied -= OnEnemyDied;
                    _enemyFactory?.Release(enemy);
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

            foreach (var magnet in _magnetItems)
            {
                if (magnet != null)
                {
                    magnet.Collected -= OnMagnetCollected;
                    Destroy(magnet.gameObject);
                }
            }

            foreach (var expOrb in _expOrbs)
            {
                if (expOrb != null)
                {
                    expOrb.Collected -= OnExpOrbCollected;
                    expOrb.Released -= OnExpOrbReleased;
                    _expOrbFactory?.Release(expOrb);
                }
            }

            _enemies.Clear();
            _medKits.Clear();
            _magnetItems.Clear();
            _expOrbs.Clear();

            if (_player != null)
            {
                _player.ResetPosition(GetMapCenterPosition());
            }

            _spawnService.Reset();
            EnsureRoundMap();
            _roundMapView?.ResetToStage(snapshot.Stage);

            var randomMapGen = FindObjectOfType<RandomMapGenerator>();
            if (randomMapGen != null)
            {
                randomMapGen.UpdateStage(snapshot.Stage);
            }
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
                    expOrb.Released -= OnExpOrbReleased;
                }
            }

            foreach (var magnet in _magnetItems)
            {
                if (magnet != null)
                {
                    magnet.Collected -= OnMagnetCollected;
                }
            }

            if (_hudPresenter != null)
            {
                _hudPresenter.RestartRequested -= RestartRun;
            }

            _enemyFactory?.ClearPool();
            _expOrbFactory?.ClearPool();
        }

        private void CleanupDestroyedViews()
        {
            _enemies.RemoveAll(enemy => enemy == null);
            _medKits.RemoveAll(medKit => medKit == null);
            _magnetItems.RemoveAll(magnet => magnet == null);
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

        private void EnsureEntityRoots()
        {
            if (_enemiesRoot == null)
            {
                var root = GameObject.Find("EnemiesRoot") ?? new GameObject("EnemiesRoot");
                _enemiesRoot = root.transform;
            }

            if (_medKitRoot == null)
            {
                var root = GameObject.Find("MedKitRoot") ?? new GameObject("MedKitRoot");
                _medKitRoot = root.transform;
            }

            if (_expOrbRoot == null)
            {
                var root = GameObject.Find("ExpOrbRoot") ?? new GameObject("ExpOrbRoot");
                _expOrbRoot = root.transform;
            }
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

        private bool IsWalkablePosition(Vector2 worldPosition)
        {
            if (_roundMapView == null)
            {
                return true;
            }

            return _roundMapView.IsWalkable(worldPosition);
        }

        private Vector3 GetMapCenterPosition()
        {
            if (_mapPolicy == null)
            {
                return new Vector3(_runConfig.PlayerStartX, _runConfig.PlayerStartY, 0f);
            }

            float x = (_mapPolicy.PlayerMinX + _mapPolicy.PlayerMaxX) * 0.5f;
            float y = (_mapPolicy.PlayerMinY + _mapPolicy.PlayerMaxY) * 0.5f;
            return new Vector3(x, y, 0f);
        }

        private void EnsureRuntimeReferences()
        {
            HideLegacyStartOverlay();

            if (_inputPort == null)
            {
                _inputPort = Object.FindFirstObjectByType<RuntimeInputPort>();
            }

            if (_inputPort == null)
            {
                var inputObject = new GameObject("RuntimeInputPort");
                _inputPort = inputObject.AddComponent<RuntimeInputPort>();
            }
            else
            {
                if (!_inputPort.isActiveAndEnabled)
                {
                    _inputPort.enabled = true;
                }

                if (_inputPort.gameObject != null && !_inputPort.gameObject.activeInHierarchy)
                {
                    _inputPort.gameObject.SetActive(true);
                }
            }

            _inputPort.AutoBindFallbackJoysticks();

            if (!_enableUltimate)
            {
                var ultimateButtons = Object.FindObjectsByType<UltimatePressButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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

        private void BuildUpgradeChoices()
        {
            if (_upgradeRuleService == null)
            {
                _upgradeChoices = new[]
                {
                    new WeaponUpgradeRule("damage", WeaponUpgradeEffectType.DamageMultiplier, "Power +20%", 1.2f, true),
                    new WeaponUpgradeRule("attack-speed", WeaponUpgradeEffectType.AttackSpeedMultiplier, "Attack Speed +15%", 1.15f, true),
                    new WeaponUpgradeRule("max-hp", WeaponUpgradeEffectType.MaxHpFlat, "Max HP +20", 20f, true)
                };
                return;
            }

            _upgradeChoices = _upgradeRuleService.BuildChoices(_weaponLoadout, _randomService);
        }

        private string GetUpgradeChoiceLabel(WeaponUpgradeRule choice)
        {
            return string.IsNullOrEmpty(choice.Label) ? "Power +20%" : choice.Label;
        }

        private void ApplyUpgradeByChoice(WeaponUpgradeRule choice)
        {
            switch (choice.EffectType)
            {
                case WeaponUpgradeEffectType.DamageMultiplier:
                    _player.ApplyDamageMultiplier(choice.Value > 0f ? choice.Value : 1.2f);
                    _weaponLoadout?.ApplyGlobalLevelUp();
                    break;
                case WeaponUpgradeEffectType.AttackSpeedMultiplier:
                    _player.ApplyAttackSpeedMultiplier(choice.Value > 0f ? choice.Value : 1.15f);
                    break;
                case WeaponUpgradeEffectType.MaxHpFlat:
                    _runSession.ApplyMaxHpUpgrade(choice.Value > 0f ? choice.Value : 20f);
                    break;
                case WeaponUpgradeEffectType.AddRandomWeapon:
                    if (_weaponLoadout == null || _randomService == null)
                    {
                        _runSession.ApplyMaxHpUpgrade(20f);
                        break;
                    }

                    if (!_weaponLoadout.TryAddRandomWeapon(_randomService, out _))
                    {
                        _runSession.ApplyMaxHpUpgrade(20f);
                    }

                    break;
            }

            _upgradeRuleService?.MarkApplied(choice);
        }

        private static void HideLegacyStartOverlay()
        {
            var tap = GameObject.Find("TapToStart");
            if (tap != null)
            {
                tap.SetActive(false);
            }

            var title = GameObject.Find("Title");
            if (title != null)
            {
                title.SetActive(false);
            }
        }

        private void ApplyEnemyStateAnimation(EnemyView enemy, int enemySerial)
        {
            if (enemy == null)
            {
                return;
            }

            if (_enemyMoveFrameA == null || _enemyMoveFrameB == null)
            {
                enemy.ConfigureFallbackVisual(enemySerial);
                return;
            }

            enemy.ClearFallbackVisual();

            enemy.ConfigureStateAnimation(
                _enemyMoveFrameA,
                _enemyMoveFrameB,
                _enemyHitFrame,
                _enemyDeathFrame,
                _enemyMoveFrameInterval,
                _enemyHitFrameDuration,
                _enemyDeathFrameDuration);
        }

        private void BuildEnemyStateFrames()
        {
            if (_enemyStateSheet != null)
            {
                int frameCount = 4;
                int frameWidth = _enemyStateSheet.width / frameCount;
                int frameHeight = _enemyStateSheet.height;
                if (frameWidth > 0 && frameHeight > 0)
                {
                    float ppu = Mathf.Max(1f, frameWidth);
                    _enemyMoveFrameA = CreateSheetFrame(_enemyStateSheet, frameWidth, frameHeight, 0, ppu);
                    _enemyMoveFrameB = CreateSheetFrame(_enemyStateSheet, frameWidth, frameHeight, 1, ppu);
                    _enemyHitFrame = CreateSheetFrame(_enemyStateSheet, frameWidth, frameHeight, 2, ppu);
                    _enemyDeathFrame = CreateSheetFrame(_enemyStateSheet, frameWidth, frameHeight, 3, ppu);
                    return;
                }
            }

            _enemyMoveFrameA = _enemyMoveFrameASprite;
            _enemyMoveFrameB = _enemyMoveFrameBSprite;
            _enemyHitFrame = _enemyHitFrameSprite;
            _enemyDeathFrame = _enemyDeathFrameSprite;
        }

        private static Sprite CreateSheetFrame(Texture2D sheet, int frameWidth, int frameHeight, int index, float ppu)
        {
            return Sprite.Create(
                sheet,
                new Rect(index * frameWidth, 0f, frameWidth, frameHeight),
                new Vector2(0.5f, 0.5f),
                ppu);
        }
    }
}
