using OneDayGame.Application;
using OneDayGame.Domain.Gameplay;
using OneDayGame.Presentation.Bootstrap;
using UnityEditor;
using UnityEngine;

public sealed class GameplayConfigOverviewWindow : EditorWindow
{
    private Vector2 _scroll;
    private StageConfig _stageConfig;
    private GameBootstrap _bootstrap;

    [MenuItem("Tools/OneDayGame/Gameplay Config Overview")]
    private static void Open()
    {
        var window = GetWindow<GameplayConfigOverviewWindow>("Gameplay Config Overview");
        window.minSize = new Vector2(760f, 500f);
        window.Show();
    }

    private void OnEnable()
    {
        AutoResolveStageConfig();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Developer One-Glance Config", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Enemy bands come from StageConfig. Weapon list comes from WeaponLoadoutService default catalog.", MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            _stageConfig = (StageConfig) EditorGUILayout.ObjectField("Stage Config", _stageConfig, typeof(StageConfig), false);
            if (GUILayout.Button("Auto", GUILayout.Width(60f)))
            {
                AutoResolveStageConfig();
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            _bootstrap = (GameBootstrap) EditorGUILayout.ObjectField("Bootstrap", _bootstrap, typeof(GameBootstrap), true);
            if (GUILayout.Button("Assign StageConfig", GUILayout.Width(150f)))
            {
                AssignStageConfigToBootstrap();
            }

            if (GUILayout.Button("Create StageConfig", GUILayout.Width(150f)))
            {
                CreateAndAssignStageConfig();
            }
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        DrawEnemyBandSection();
        EditorGUILayout.Space(10f);
        DrawStageLoopPreview();
        EditorGUILayout.Space(12f);
        DrawWeaponSection();
        EditorGUILayout.EndScrollView();
    }

    private void DrawEnemyBandSection()
    {
        EditorGUILayout.LabelField("Enemy Config Bands", EditorStyles.boldLabel);
        if (_stageConfig == null)
        {
            EditorGUILayout.HelpBox("Assign StageConfig to inspect enemy band settings.", MessageType.Warning);
            return;
        }

        var serialized = new SerializedObject(_stageConfig);
        var bands = serialized.FindProperty("_bandProfiles");
        if (bands == null || !bands.isArray || bands.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No band profiles found.", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginVertical("box");
        for (int i = 0; i < bands.arraySize; i++)
        {
            var band = bands.GetArrayElementAtIndex(i);
            if (band == null)
            {
                continue;
            }

            int start = band.FindPropertyRelative("StageStart")?.intValue ?? 1;
            int end = band.FindPropertyRelative("StageEnd")?.intValue ?? start;
            string enemyType = band.FindPropertyRelative("EnemyType")?.stringValue ?? "Normal";
            float hp = band.FindPropertyRelative("EnemyBaseMaxHp")?.floatValue ?? 0f;
            float speed = band.FindPropertyRelative("EnemyBaseMoveSpeed")?.floatValue ?? 0f;
            float damage = band.FindPropertyRelative("EnemyBaseContactDamage")?.floatValue ?? 0f;
            float spawnBase = band.FindPropertyRelative("SpawnIntervalBase")?.floatValue ?? 0f;

            EditorGUILayout.LabelField($"Band {i + 1}: Stage {start}-{end} | Type: {enemyType}", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"HP {hp:F1} | Speed {speed:F2} | Contact {damage:F1} | SpawnBase {spawnBase:F2}", EditorStyles.miniLabel);
            EditorGUILayout.Space(4f);
        }

        EditorGUILayout.EndVertical();
    }

    private static void DrawWeaponSection()
    {
        EditorGUILayout.LabelField("Weapon Catalog (Default)", EditorStyles.boldLabel);
        var loadout = WeaponLoadoutService.CreateDefault();
        if (loadout == null)
        {
            EditorGUILayout.HelpBox("Cannot resolve weapon loadout.", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginVertical("box");
        var catalog = loadout.Catalog;
        for (int i = 0; i < catalog.Count; i++)
        {
            var weapon = catalog[i];
            if (weapon == null)
            {
                continue;
            }

            EditorGUILayout.LabelField($"{i + 1}. {weapon.DisplayName} ({weapon.Type})", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"ID {weapon.Id} | Target {weapon.TargetingMode} | BaseDmg {weapon.BaseDamage:F1} | Cooldown {weapon.BaseCooldown:F2} | Projectiles {weapon.ProjectileCount}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField(weapon.Description, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4f);
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Current Slots", EditorStyles.miniBoldLabel);
        var slots = loadout.Slots;
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            string slotLabel = slot == null || slot.IsEmpty ? "Empty" : slot.Definition.DisplayName;
            EditorGUILayout.LabelField($"Slot {i + 1}: {slotLabel}", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawStageLoopPreview()
    {
        EditorGUILayout.LabelField("Stage Loop Preview", EditorStyles.boldLabel);
        if (_stageConfig == null)
        {
            EditorGUILayout.HelpBox("Assign StageConfig to preview per-stage enemy info.", MessageType.Warning);
            return;
        }

        int firstBossStage = FindFirstBossStage(200);
        int maxStage = firstBossStage > 0 ? firstBossStage : 10;

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"Preview Range: Stage 1 ~ {maxStage}", EditorStyles.miniBoldLabel);
        for (int stage = 1; stage <= maxStage; stage++)
        {
            var profile = _stageConfig.ResolveProfile(stage);
            bool isBoss = profile.IsBossStage(stage);
            string bossTag = isBoss ? " [BOSS]" : string.Empty;
            EditorGUILayout.LabelField(
                $"S{stage}{bossTag} | Enemy {profile.EnemyType} | HP {profile.EnemyMaxHp:F1} | Spd {profile.EnemyMoveSpeed:F2} | Dmg {profile.EnemyContactDamage:F1} | SpawnBase {profile.SpawnIntervalBase:F2}",
                EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    private int FindFirstBossStage(int searchLimit)
    {
        int limit = Mathf.Max(1, searchLimit);
        for (int stage = 1; stage <= limit; stage++)
        {
            var profile = _stageConfig.ResolveProfile(stage);
            if (profile.IsBossStage(stage))
            {
                return stage;
            }
        }

        return -1;
    }

    private void AutoResolveStageConfig()
    {
        _bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
        if (_bootstrap == null)
        {
            return;
        }

        var serialized = new SerializedObject(_bootstrap);
        var prop = serialized.FindProperty("_stageProfileConfig");
        if (prop != null && prop.objectReferenceValue is StageConfig config)
        {
            _stageConfig = config;
        }
    }

    private void AssignStageConfigToBootstrap()
    {
        if (_bootstrap == null || _stageConfig == null)
        {
            return;
        }

        var serialized = new SerializedObject(_bootstrap);
        var prop = serialized.FindProperty("_stageProfileConfig");
        if (prop == null)
        {
            return;
        }

        prop.objectReferenceValue = _stageConfig;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(_bootstrap);
    }

    private void CreateAndAssignStageConfig()
    {
        var asset = CreateInstance<StageConfig>();
        const string basePath = "Assets/Configs/StageConfig.asset";
        string path = AssetDatabase.GenerateUniqueAssetPath(basePath);
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        _stageConfig = asset;
        AssignStageConfigToBootstrap();
        Selection.activeObject = asset;
    }
}
