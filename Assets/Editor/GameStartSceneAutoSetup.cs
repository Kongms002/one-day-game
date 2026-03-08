using OneDayGame.Presentation.Bootstrap;
using OneDayGame.Presentation.Gameplay;
using OneDayGame.Presentation.Input;
using OneDayGame.Presentation.Ui;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GameStartSceneAutoSetup
{
    [MenuItem("Tools/OneDayGame/Setup Active Scene")]
    public static void SetupActiveScene()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("No active loaded scene. Open a scene first.");
            return;
        }

        RemoveMissingScripts(scene);

        var canvas = EnsureCanvas();
        EnsureEventSystem();

        var runtimeInput = EnsureRuntimeInput(canvas.transform);
        var hud = EnsureHud(canvas.transform);
        var bootstrap = EnsureBootstrap(runtimeInput, hud);
        DisableLegacyFlowComponents();

        Selection.activeObject = bootstrap.gameObject;
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[OneDayGame] Active scene setup completed. Review Inspector references and press Play.");
    }

    private static void DisableLegacyFlowComponents()
    {
        var oldGameManager = FindFirstByType<global::GameManager>();
        if (oldGameManager != null)
        {
            oldGameManager.enabled = false;
            EditorUtility.SetDirty(oldGameManager);
        }

        var oldPlayerController = FindFirstByType<global::PlayerController>();
        if (oldPlayerController != null)
        {
            oldPlayerController.gameObject.SetActive(false);
            EditorUtility.SetDirty(oldPlayerController.gameObject);
        }
    }

    private static Canvas EnsureCanvas()
    {
        var existing = Object.FindFirstObjectByType<Canvas>();
        if (existing != null)
        {
            return existing;
        }

        var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        return canvas;
    }

    private static void EnsureEventSystem()
    {
        var existing = Object.FindFirstObjectByType<EventSystem>();
        if (existing != null)
        {
            if (existing.GetComponent<InputSystemUIInputModule>() == null)
            {
                existing.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            var oldModule = existing.GetComponent<StandaloneInputModule>();
            if (oldModule != null)
            {
                Object.DestroyImmediate(oldModule);
            }

            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private static RuntimeInputPort EnsureRuntimeInput(Transform canvasTransform)
    {
        var runtimeInputGO = FindOrCreateRoot("RuntimeInput");
        var runtimeInput = GetOrAdd<RuntimeInputPort>(runtimeInputGO);

        var joystick = CreateJoystick(canvasTransform);

        var buttons = Object.FindObjectsByType<UltimatePressButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var ultimateButton = FindByName(buttons, "UltimateButton");
        if (ultimateButton != null)
        {
            ultimateButton.gameObject.SetActive(false);
        }

        var actionAsset = LoadInputActions();
        var serialized = new SerializedObject(runtimeInput);
        serialized.FindProperty("_actionAsset").objectReferenceValue = actionAsset;
        serialized.FindProperty("_actionMapName").stringValue = "Player";
        serialized.FindProperty("_moveActionName").stringValue = "Move";
        serialized.FindProperty("_ultimateActionName").stringValue = string.Empty;
        serialized.FindProperty("_joystick").objectReferenceValue = joystick;
        serialized.FindProperty("_ultimateButton").objectReferenceValue = ultimateButton;
        serialized.FindProperty("_useUltimateInput").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return runtimeInput;
    }

    private static void RemoveMissingScripts(Scene scene)
    {
        var roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            RemoveMissingScriptsRecursive(roots[i].transform);
        }
    }

    private static void RemoveMissingScriptsRecursive(Transform transform)
    {
        if (transform == null)
        {
            return;
        }

        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transform.gameObject);
        for (int i = 0; i < transform.childCount; i++)
        {
            RemoveMissingScriptsRecursive(transform.GetChild(i));
        }
    }

    private static GameHudPresenter EnsureHud(Transform canvasTransform)
    {
        var hudRootGO = FindOrCreateChild(canvasTransform, "HudRoot");
        var hud = GetOrAdd<GameHudPresenter>(hudRootGO);

        var hudRootRect = GetOrAdd<RectTransform>(hudRootGO);
        hudRootRect.anchorMin = Vector2.zero;
        hudRootRect.anchorMax = Vector2.one;
        hudRootRect.offsetMin = Vector2.zero;
        hudRootRect.offsetMax = Vector2.zero;
        hudRootRect.pivot = new Vector2(0.5f, 0.5f);

        var profileIcon = EnsureHudIcon(hudRootGO.transform, "ProfileIcon", new Vector2(16f, -16f), new Vector2(72f, 72f));
        profileIcon.sprite = RuntimeSpriteLibrary.GetCircle();
        profileIcon.color = new Color(0.15f, 0.95f, 1f, 0.95f);
        var profileNameText = EnsureHudText(hudRootGO.transform, "ProfileNameText", new Vector2(100f, -18f));
        profileNameText.text = "PLAYER";

        var hpText = EnsureHudText(hudRootGO.transform, "HpText", new Vector2(100f, -58f));
        var levelText = EnsureHudText(hudRootGO.transform, "LevelText", new Vector2(100f, -92f));
        var expText = EnsureHudText(hudRootGO.transform, "ExpText", new Vector2(100f, -124f));
        var scoreText = EnsureHudText(hudRootGO.transform, "ScoreText", new Vector2(470f, -22f));
        var stageText = EnsureHudText(hudRootGO.transform, "StageText", new Vector2(470f, -56f));
        var timeText = EnsureHudText(hudRootGO.transform, "TimeText", new Vector2(470f, -90f));
        var highScoreText = EnsureHudText(hudRootGO.transform, "HighScoreText", new Vector2(470f, -124f));
        var enemiesSpawnedText = EnsureHudText(hudRootGO.transform, "EnemiesSpawnedText", new Vector2(470f, -158f));
        var statusText = EnsureHudText(hudRootGO.transform, "StatusText", new Vector2(0f, -56f), TextAnchor.MiddleCenter);
        var ultimateText = EnsureHudText(hudRootGO.transform, "UltimateText", new Vector2(72f, -236f));
        var weaponText = EnsureHudText(hudRootGO.transform, "WeaponText", new Vector2(72f, -196f));
        var weaponIcon = EnsureHudIcon(hudRootGO.transform, "WeaponIcon", new Vector2(20f, -194f), new Vector2(40f, 40f));
        var expBarBg = EnsureHudPanel(hudRootGO.transform, "ExpBarBg", new Vector2(100f, -154f), new Vector2(340f, 18f), new Color(0f, 0f, 0f, 0.45f));
        var expBarFill = EnsureHudPanel(expBarBg.transform, "ExpBarFill", Vector2.zero, new Vector2(316f, 14f), new Color(0.24f, 0.82f, 1f, 0.9f));

        var expBarBgRect = expBarBg.GetComponent<RectTransform>();
        expBarBgRect.anchorMin = new Vector2(0f, 1f);
        expBarBgRect.anchorMax = new Vector2(0f, 1f);
        expBarBgRect.pivot = new Vector2(0f, 1f);
        expBarBgRect.anchoredPosition = new Vector2(100f, -154f);
        expBarBgRect.sizeDelta = new Vector2(340f, 18f);

        var expBarFillImageRect = expBarFill.GetComponent<RectTransform>();
        expBarFillImageRect.anchorMin = new Vector2(0f, 0f);
        expBarFillImageRect.anchorMax = new Vector2(1f, 1f);
        expBarFillImageRect.offsetMin = new Vector2(2f, 2f);
        expBarFillImageRect.offsetMax = new Vector2(-2f, -2f);
        var expBarFillImage = expBarFill.GetComponent<Image>();
        expBarFillImage.type = Image.Type.Filled;
        expBarFillImage.fillMethod = Image.FillMethod.Horizontal;
        expBarFillImage.fillOrigin = 0;
        expBarFillImage.fillAmount = 0f;
        weaponIcon.sprite = RuntimeSpriteLibrary.GetDiamond();
        weaponIcon.color = new Color(1f, 0.9f, 0.25f, 1f);
        var weaponButton = GetOrAdd<Button>(weaponIcon.gameObject);
        weaponButton.targetGraphic = weaponIcon;

        var weaponDetailPanel = EnsureHudPanel(hudRootGO.transform, "WeaponDetailPanel", new Vector2(0f, 0f), new Vector2(460f, 240f), new Color(0.08f, 0.09f, 0.12f, 0.94f));
        var weaponDetailRect = weaponDetailPanel.GetComponent<RectTransform>();
        weaponDetailRect.anchorMin = new Vector2(0.5f, 0.5f);
        weaponDetailRect.anchorMax = new Vector2(0.5f, 0.5f);
        weaponDetailRect.pivot = new Vector2(0.5f, 0.5f);
        weaponDetailRect.anchoredPosition = new Vector2(-120f, -30f);
        var weaponDetailTitle = EnsurePanelText(weaponDetailPanel.transform, "WeaponDetailTitle", new Vector2(16f, -14f), 25, TextAnchor.UpperLeft);
        var weaponDetailStats = EnsurePanelText(weaponDetailPanel.transform, "WeaponDetailStats", new Vector2(16f, -52f), 20, TextAnchor.UpperLeft);
        var weaponDetailDescription = EnsurePanelText(weaponDetailPanel.transform, "WeaponDetailDescription", new Vector2(16f, -132f), 18, TextAnchor.UpperLeft);
        var weaponDetailConfirm = EnsurePanelButton(weaponDetailPanel.transform, "WeaponDetailConfirm", new Vector2(300f, -188f), new Vector2(140f, 40f), "Confirm");
        weaponDetailPanel.SetActive(false);

        var levelUpPanel = EnsureHudPanel(hudRootGO.transform, "LevelUpPanel", Vector2.zero, new Vector2(560f, 380f), new Color(0.06f, 0.07f, 0.1f, 0.94f));
        var levelUpRect = levelUpPanel.GetComponent<RectTransform>();
        levelUpRect.anchorMin = new Vector2(0.5f, 0.5f);
        levelUpRect.anchorMax = new Vector2(0.5f, 0.5f);
        levelUpRect.pivot = new Vector2(0.5f, 0.5f);
        levelUpRect.anchoredPosition = Vector2.zero;
        var levelUpTitle = EnsurePanelText(levelUpPanel.transform, "LevelUpTitle", new Vector2(20f, -20f), 30, TextAnchor.UpperLeft);
        var upgradeA = EnsurePanelButton(levelUpPanel.transform, "UpgradeA", new Vector2(28f, -90f), new Vector2(504f, 78f), "Power +20%");
        var upgradeB = EnsurePanelButton(levelUpPanel.transform, "UpgradeB", new Vector2(28f, -182f), new Vector2(504f, 78f), "Attack Speed +15%");
        var upgradeC = EnsurePanelButton(levelUpPanel.transform, "UpgradeC", new Vector2(28f, -274f), new Vector2(504f, 78f), "Max HP +20");
        levelUpPanel.SetActive(false);

        var resultPanel = EnsureHudPanel(hudRootGO.transform, "ResultPanel", Vector2.zero, new Vector2(520f, 320f), new Color(0.05f, 0.05f, 0.07f, 0.94f));
        var resultRect = resultPanel.GetComponent<RectTransform>();
        resultRect.anchorMin = new Vector2(0.5f, 0.5f);
        resultRect.anchorMax = new Vector2(0.5f, 0.5f);
        resultRect.pivot = new Vector2(0.5f, 0.5f);
        resultRect.anchoredPosition = Vector2.zero;
        var resultTitle = EnsurePanelText(resultPanel.transform, "ResultTitle", new Vector2(24f, -24f), 30, TextAnchor.UpperLeft);
        resultTitle.text = "Run Result";
        var resultDamage = EnsurePanelText(resultPanel.transform, "ResultDamage", new Vector2(24f, -88f), 24, TextAnchor.UpperLeft);
        var resultTime = EnsurePanelText(resultPanel.transform, "ResultTime", new Vector2(24f, -132f), 24, TextAnchor.UpperLeft);
        var resultKills = EnsurePanelText(resultPanel.transform, "ResultKills", new Vector2(24f, -176f), 24, TextAnchor.UpperLeft);
        var restartButton = EnsurePanelButton(resultPanel.transform, "RestartButton", new Vector2(170f, -246f), new Vector2(180f, 52f), "Restart");
        resultPanel.SetActive(false);

        var serialized = new SerializedObject(hud);
        serialized.FindProperty("_scoreText").objectReferenceValue = scoreText;
        serialized.FindProperty("_stageText").objectReferenceValue = stageText;
        serialized.FindProperty("_hpText").objectReferenceValue = hpText;
        serialized.FindProperty("_ultimateText").objectReferenceValue = ultimateText;
        serialized.FindProperty("_levelText").objectReferenceValue = levelText;
        serialized.FindProperty("_expText").objectReferenceValue = expText;
        serialized.FindProperty("_expBarFill").objectReferenceValue = expBarFillImage;
        serialized.FindProperty("_timeText").objectReferenceValue = timeText;
        serialized.FindProperty("_highScoreText").objectReferenceValue = highScoreText;
        serialized.FindProperty("_enemiesSpawnedText").objectReferenceValue = enemiesSpawnedText;
        serialized.FindProperty("_statusText").objectReferenceValue = statusText;
        serialized.FindProperty("_weaponText").objectReferenceValue = weaponText;
        serialized.FindProperty("_weaponIcon").objectReferenceValue = weaponIcon;
        serialized.FindProperty("_weaponButton").objectReferenceValue = weaponButton;
        serialized.FindProperty("_weaponDetailPanel").objectReferenceValue = weaponDetailPanel;
        serialized.FindProperty("_weaponDetailTitle").objectReferenceValue = weaponDetailTitle;
        serialized.FindProperty("_weaponDetailStats").objectReferenceValue = weaponDetailStats;
        serialized.FindProperty("_weaponDetailDescription").objectReferenceValue = weaponDetailDescription;
        serialized.FindProperty("_weaponDetailConfirmButton").objectReferenceValue = weaponDetailConfirm.GetComponent<Button>();
        serialized.FindProperty("_levelUpPanel").objectReferenceValue = levelUpPanel;
        serialized.FindProperty("_levelUpTitleText").objectReferenceValue = levelUpTitle;
        serialized.FindProperty("_upgradeButtonA").objectReferenceValue = upgradeA.GetComponent<Button>();
        serialized.FindProperty("_upgradeButtonB").objectReferenceValue = upgradeB.GetComponent<Button>();
        serialized.FindProperty("_upgradeButtonC").objectReferenceValue = upgradeC.GetComponent<Button>();
        serialized.FindProperty("_upgradeButtonAText").objectReferenceValue = upgradeA.transform.Find("Text").GetComponent<Text>();
        serialized.FindProperty("_upgradeButtonBText").objectReferenceValue = upgradeB.transform.Find("Text").GetComponent<Text>();
        serialized.FindProperty("_upgradeButtonCText").objectReferenceValue = upgradeC.transform.Find("Text").GetComponent<Text>();
        serialized.FindProperty("_resultPanel").objectReferenceValue = resultPanel;
        serialized.FindProperty("_resultDamageText").objectReferenceValue = resultDamage;
        serialized.FindProperty("_resultTimeText").objectReferenceValue = resultTime;
        serialized.FindProperty("_resultKillsText").objectReferenceValue = resultKills;
        serialized.FindProperty("_restartButton").objectReferenceValue = restartButton.GetComponent<Button>();
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return hud;
    }

    private static GameBootstrap EnsureBootstrap(RuntimeInputPort runtimeInput, GameHudPresenter hud)
    {
        var bootstrapGO = FindOrCreateRoot("GameBootstrap");
        var bootstrap = GetOrAdd<GameBootstrap>(bootstrapGO);

        var enemiesRoot = FindOrCreateRoot("EnemiesRoot").transform;
        var medKitRoot = FindOrCreateRoot("MedKitRoot").transform;
        var expOrbRoot = FindOrCreateRoot("ExpOrbRoot").transform;
        var roundMap = GetOrAdd<RoundMapView>(FindOrCreateRoot("RoundMap"));

        var playerPrefab = LoadAsset<PlayerView>("Assets/Prefabs/Player.prefab");
        var enemyPrefab = LoadAsset<EnemyView>("Assets/Prefabs/Enemy.prefab");
        var medKitPrefab = LoadAsset<MedKitView>("Assets/Prefabs/MedKit.prefab");
        var expOrbPrefab = LoadAsset<ExpOrbView>("Assets/Prefabs/ExpOrb.prefab");
        var enemyStateSheet = LoadAsset<Texture2D>("Assets/Image/Enemy/enemy_1.png");

        var serialized = new SerializedObject(bootstrap);
        serialized.FindProperty("_inputPort").objectReferenceValue = runtimeInput;
        serialized.FindProperty("_playerPrefab").objectReferenceValue = playerPrefab;
        serialized.FindProperty("_enemyPrefab").objectReferenceValue = enemyPrefab;
        serialized.FindProperty("_medKitPrefab").objectReferenceValue = medKitPrefab;
        serialized.FindProperty("_expOrbPrefab").objectReferenceValue = expOrbPrefab;
        serialized.FindProperty("_enemiesRoot").objectReferenceValue = enemiesRoot;
        serialized.FindProperty("_medKitRoot").objectReferenceValue = medKitRoot;
        serialized.FindProperty("_expOrbRoot").objectReferenceValue = expOrbRoot;
        serialized.FindProperty("_hudPresenter").objectReferenceValue = hud;
        serialized.FindProperty("_roundMapView").objectReferenceValue = roundMap;
        serialized.FindProperty("_enemyStateSheet").objectReferenceValue = enemyStateSheet;
        serialized.FindProperty("_enemyMoveFrameASprite").objectReferenceValue = null;
        serialized.FindProperty("_enemyMoveFrameBSprite").objectReferenceValue = null;
        serialized.FindProperty("_enemyHitFrameSprite").objectReferenceValue = null;
        serialized.FindProperty("_enemyDeathFrameSprite").objectReferenceValue = null;
        serialized.FindProperty("_enemyMoveFrameInterval").floatValue = 0.14f;
        serialized.FindProperty("_enemyHitFrameDuration").floatValue = 0.08f;
        serialized.FindProperty("_enemyDeathFrameDuration").floatValue = 0.35f;
        serialized.FindProperty("_showEnemyHpBarsInDev").boolValue = true;
        serialized.FindProperty("_enableUltimate").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return bootstrap;
    }

    private static FloatingJoystick CreateJoystick(Transform canvasTransform)
    {
        var joystickGO = FindOrCreateChild(canvasTransform, "JoyStick");
        var joystickRect = GetOrAdd<RectTransform>(joystickGO);
        joystickRect.anchorMin = Vector2.zero;
        joystickRect.anchorMax = Vector2.one;
        joystickRect.pivot = new Vector2(0.5f, 0.5f);
        joystickRect.offsetMin = Vector2.zero;
        joystickRect.offsetMax = Vector2.zero;

        var touchAreaImage = GetOrAdd<Image>(joystickGO);
        touchAreaImage.color = new Color(0f, 0f, 0f, 0.001f);
        touchAreaImage.raycastTarget = true;

        var bgGO = FindOrCreateChild(joystickRect, "Background");
        var bgRect = GetOrAdd<RectTransform>(bgGO);
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.sizeDelta = new Vector2(200f, 200f);
        var bgImage = GetOrAdd<Image>(bgGO);
        bgImage.sprite = RuntimeSpriteLibrary.GetCircle();
        bgImage.preserveAspect = true;
        bgImage.color = new Color(1f, 1f, 1f, 0.25f);
        bgImage.raycastTarget = false;

        var handleGO = FindOrCreateChild(bgRect, "Handle");
        var handleRect = GetOrAdd<RectTransform>(handleGO);
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.anchoredPosition = Vector2.zero;
        handleRect.sizeDelta = new Vector2(90f, 90f);
        var handleImage = GetOrAdd<Image>(handleGO);
        handleImage.sprite = RuntimeSpriteLibrary.GetCircle();
        handleImage.preserveAspect = true;
        handleImage.color = new Color(1f, 1f, 1f, 0.6f);
        handleImage.raycastTarget = false;

        var joystick = GetOrAdd<FloatingJoystick>(joystickGO);
        var serialized = new SerializedObject(joystick);
        serialized.FindProperty("_background").objectReferenceValue = bgRect;
        serialized.FindProperty("_handle").objectReferenceValue = handleRect;
        serialized.FindProperty("_floatingMode").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return joystick;
    }

    private static UltimatePressButton CreateActionButton(Transform canvasTransform, string name, Vector2 anchoredPosition)
    {
        var buttonGO = FindOrCreateChild(canvasTransform, name);
        var rect = GetOrAdd<RectTransform>(buttonGO);
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(120f, 120f);

        var image = GetOrAdd<Image>(buttonGO);
        image.color = new Color(1f, 1f, 1f, 0.35f);
        GetOrAdd<Button>(buttonGO);
        return GetOrAdd<UltimatePressButton>(buttonGO);
    }

    private static Text EnsureHudText(Transform parent, string name, Vector2 anchoredPosition)
    {
        return EnsureHudText(parent, name, anchoredPosition, TextAnchor.MiddleLeft);
    }

    private static Text EnsureHudText(Transform parent, string name, Vector2 anchoredPosition, TextAnchor alignment)
    {
        var go = FindOrCreateChild(parent, name);
        var rect = GetOrAdd<RectTransform>(go);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(560f, 40f);

        var text = GetOrAdd<Text>(go);
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 28;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = Color.white;
        return text;
    }

    private static Image EnsureHudIcon(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
    {
        var go = FindOrCreateChild(parent, name);
        var rect = GetOrAdd<RectTransform>(go);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var image = GetOrAdd<Image>(go);
        image.preserveAspect = true;
        return image;
    }

    private static GameObject EnsureHudPanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color backgroundColor)
    {
        var go = FindOrCreateChild(parent, name);
        var rect = GetOrAdd<RectTransform>(go);
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var image = GetOrAdd<Image>(go);
        image.color = backgroundColor;
        return go;
    }

    private static Text EnsurePanelText(Transform parent, string name, Vector2 anchoredPosition, int fontSize, TextAnchor alignment)
    {
        var go = FindOrCreateChild(parent, name);
        var rect = GetOrAdd<RectTransform>(go);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(-24f, 54f);

        var text = GetOrAdd<Text>(go);
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = Color.white;
        return text;
    }

    private static GameObject EnsurePanelButton(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, string label)
    {
        var go = FindOrCreateChild(parent, name);
        var rect = GetOrAdd<RectTransform>(go);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var image = GetOrAdd<Image>(go);
        image.color = new Color(0.18f, 0.22f, 0.3f, 0.95f);
        var button = GetOrAdd<Button>(go);
        button.targetGraphic = image;

        var textGo = FindOrCreateChild(go.transform, "Text");
        var textRect = GetOrAdd<RectTransform>(textGo);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 10f);
        textRect.offsetMax = new Vector2(-12f, -10f);

        var text = GetOrAdd<Text>(textGo);
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 22;
        text.alignment = TextAnchor.MiddleLeft;
        text.text = label;
        text.color = Color.white;
        return go;
    }

    private static T FindFirstByType<T>() where T : Component
    {
        var all = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (all == null || all.Length == 0)
        {
            return null;
        }

        return all[0];
    }

    private static InputActionAsset LoadInputActions()
    {
        var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        if (asset != null)
        {
            return asset;
        }

        var guids = AssetDatabase.FindAssets("t:InputActionAsset");
        if (guids.Length == 0)
        {
            return null;
        }

        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
    }

    private static T LoadAsset<T>(string preferredPath) where T : Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(preferredPath);
        if (asset != null)
        {
            return asset;
        }

        var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
        if (guids.Length == 0)
        {
            return null;
        }

        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }

    private static GameObject FindOrCreateRoot(string name)
    {
        var found = GameObject.Find(name);
        if (found != null)
        {
            return found;
        }

        return new GameObject(name);
    }

    private static GameObject FindOrCreateChild(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child != null)
        {
            return child.gameObject;
        }

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var existing = go.GetComponent<T>();
        if (existing != null)
        {
            return existing;
        }

        return go.AddComponent<T>();
    }

    private static T FindByName<T>(T[] items, string name) where T : Component
    {
        for (var i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].name == name)
            {
                return items[i];
            }
        }

        return null;
    }
}
