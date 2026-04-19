using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public static class SceneSetup
{
    static Sprite _placeholder;

    static GameObject _prefabPlayer;
    static GameObject _prefabProjectile;
    static GameObject _prefabSpawnPoint;
    static GameObject _prefabExitPoint;
    static GameObject _prefabSpike;
    static GameObject _prefabButton;
    static GameObject _prefabDoor;
    static GameObject _prefabMonster;
    static GameObject _prefabWall;

    [MenuItem("Tools/生成游戏场景")]
    public static void SetupScenes()
    {
        if (!EditorUtility.DisplayDialog(
            "生成游戏场景",
            "将创建 Prefabs 和 MainMenu、Level_01、Level_02、Test 场景。\n继续？",
            "确定", "取消"))
            return;

        EnsurePlaceholderSprite();
        CreateAllPrefabs();

        CreateMainMenuScene();
        CreateLevelScene("Level_01");
        CreateLevelScene("Level_02");
        CreateTestScene();
        SyncBuildSettings();

        EditorSceneManager.OpenScene("Assets/Scenes/Test.unity");
        Debug.Log("生成完毕！Player/GameCamera 在 Assets/Resources/Prefabs/，其余在 Assets/Prefabs/。已打开 Test 场景。");
    }

    // ═══════════════════════════ Sprite creation ═══════════════════════════

    static void EnsurePlaceholderSprite()
    {
        const string path = "Assets/Sprites/Placeholder.png";
        _placeholder = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (_placeholder != null) return;

        if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
            AssetDatabase.CreateFolder("Assets", "Sprites");

        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var px = tex.GetPixels();
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        System.IO.File.WriteAllBytes(System.IO.Path.GetFullPath(path), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        SetSpriteImport(path, 4);
        _placeholder = AssetDatabase.LoadAssetAtPath<Sprite>(path);

        Debug.Log(_placeholder != null ? "占位 Sprite 创建成功" : "占位 Sprite 创建失败！");
    }

    static void SetSpriteImport(string path, int ppu)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;
        imp.textureType = TextureImporterType.Sprite;
        imp.spritePixelsPerUnit = ppu;
        imp.filterMode = FilterMode.Point;
        imp.SaveAndReimport();
    }

    // ═══════════════════════════ Prefab creation ═══════════════════════════

    static void CreateAllPrefabs()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        _prefabProjectile = MakePrefab("Projectile", go =>
        {
            Sprite(go, Color.yellow, sortOrder: 5);
            go.transform.localScale = Vector3.one * 0.5f;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;
            go.AddComponent<Projectile>();
        });

        _prefabPlayer = MakePrefab("Player", go =>
        {
            go.tag = "Player";
            Sprite(go, Color.cyan, sortOrder: 10);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            go.AddComponent<BoxCollider2D>();
            go.AddComponent<PlayerController>();
            SetRef(go.GetComponent<PlayerController>(), "projectilePrefab", _prefabProjectile);
        });

        _prefabSpawnPoint = MakePrefab("SpawnPoint", go =>
        {
            Sprite(go, Color.green, sortOrder: -1);
            go.AddComponent<SpawnPoint>();
            go.AddComponent<DarkPhaseHideable>();
        });

        _prefabExitPoint = MakePrefab("ExitPoint", go =>
        {
            Sprite(go, Color.yellow);
            go.transform.localScale = Vector3.one * 1.2f;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            go.AddComponent<ExitPoint>();
            go.AddComponent<DarkPhaseHideable>();
        });

        _prefabSpike = MakePrefab("Spike", go =>
        {
            Sprite(go, new Color(1f, 0.2f, 0.2f));
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            go.AddComponent<Spike>();
            go.AddComponent<DarkPhaseHideable>();
        });

        _prefabButton = MakePrefab("PressureButton", go =>
        {
            Sprite(go, Color.red);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            go.AddComponent<PressureButton>();
            go.AddComponent<DarkPhaseHideable>();
        });

        _prefabDoor = MakePrefab("Door", go =>
        {
            Sprite(go, new Color(0.6f, 0.4f, 0.2f));
            go.AddComponent<BoxCollider2D>();
            go.AddComponent<Door>();
            go.AddComponent<DarkPhaseHideable>();
        });

        _prefabMonster = MakePrefab("Monster", go =>
        {
            Sprite(go, Color.magenta);
            go.transform.localScale = Vector3.one * 1.2f;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            go.AddComponent<Monster>();
        });

        _prefabWall = MakePrefab("Wall", go =>
        {
            Sprite(go, new Color(0.5f, 0.5f, 0.5f));
            go.AddComponent<BoxCollider2D>();
            go.AddComponent<DarkPhaseHideable>();
        });

        EnsureGameCameraPrefab();

        Debug.Log("已创建 Prefab：Player、GameCamera → Assets/Resources/Prefabs/；其余 → Assets/Prefabs/");
    }

    static void EnsureResourcesPrefabFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs"))
            AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");
    }

    static void EnsureGameCameraPrefab()
    {
        const string path = "Assets/Resources/Prefabs/GameCamera.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            return;

        var camGO = new GameObject("GameCamera");
        camGO.tag = "MainCamera";
        camGO.transform.position = new Vector3(0f, 0f, -10f);
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.15f, 0.18f, 0.15f);
        cam.orthographic = true;
        camGO.AddComponent<AudioListener>();
        camGO.AddComponent<TopDownCamera>();
        PrefabUtility.SaveAsPrefabAsset(camGO, path);
        Object.DestroyImmediate(camGO);
    }

    static GameObject MakePrefab(string name, System.Action<GameObject> setup)
    {
        EnsureResourcesPrefabFolder();
        string path = name == "Player"
            ? $"Assets/Resources/Prefabs/{name}.prefab"
            : $"Assets/Prefabs/{name}.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) AssetDatabase.DeleteAsset(path);

        var go = new GameObject(name);
        setup(go);
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static void Sprite(GameObject go, Color color, int sortOrder = 0)
    {
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _placeholder;
        sr.color = color;
        sr.sortingOrder = sortOrder;
    }

    static void SetRef(Component comp, string fieldName, Object value)
    {
        var so = new SerializedObject(comp);
        so.FindProperty(fieldName).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ═══════════════════════════ Scene: MainMenu ═══════════════════════════

    static void CreateMainMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        new GameObject("EventSystem").AddComponent<EventSystem>()
            .gameObject.AddComponent<StandaloneInputModule>();

        var canvasGO = CreateUICanvas("Canvas");

        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.2f, 1f);
        StretchFill(bgGO);

        CreateUIText("Title", canvasGO.transform, "西格纳路",
            60, new Vector2(0, 100), new Vector2(600, 100));

        var startBtnGO = CreateUIButton("StartButton", canvasGO.transform, "开始游戏",
            new Vector2(0, -50), new Vector2(240, 64));

        var menuUI = canvasGO.AddComponent<MainMenuUI>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            startBtnGO.GetComponent<Button>().onClick, menuUI.OnStartGame);
        SetRef(menuUI, "background", bgImage);

        var managerGO = new GameObject("LevelManager");
        var lm = managerGO.AddComponent<LevelManager>();
        var so = new SerializedObject(lm);
        var lp = so.FindProperty("levelScenes");
        lp.arraySize = 2;
        lp.GetArrayElementAtIndex(0).stringValue = "Level_01";
        lp.GetArrayElementAtIndex(1).stringValue = "Level_02";
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
    }

    // ═══════════════════════════ Scene: Level template ═══════════════════════════

    static void CreateLevelScene(string sceneName)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        new GameObject("EventSystem").AddComponent<EventSystem>()
            .gameObject.AddComponent<StandaloneInputModule>();

        var canvasGO = CreateUICanvas("Canvas");
        CreateUIText("LevelTitle", canvasGO.transform, sceneName,
            48, new Vector2(0, 200), new Vector2(400, 80));

        var uiGO = new GameObject("LevelUI");
        uiGO.transform.SetParent(canvasGO.transform, false);
        uiGO.AddComponent<RectTransform>().anchoredPosition = new Vector2(0, -100);
        var levelUI = uiGO.AddComponent<LevelUI>();

        var nextBtn = CreateUIButton("NextLevelButton", uiGO.transform, "下一关",
            Vector2.zero, new Vector2(200, 60)).GetComponent<Button>();
        var finishBtn = CreateUIButton("FinishButton", uiGO.transform, "完成",
            Vector2.zero, new Vector2(200, 60)).GetComponent<Button>();

        UnityEditor.Events.UnityEventTools.AddPersistentListener(nextBtn.onClick, levelUI.OnNextLevel);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(finishBtn.onClick, levelUI.OnFinish);

        var uiSO = new SerializedObject(levelUI);
        uiSO.FindProperty("nextLevelButton").objectReferenceValue = nextBtn;
        uiSO.FindProperty("finishButton").objectReferenceValue = finishBtn;
        uiSO.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, $"Assets/Scenes/{sceneName}.unity");
    }

    // ═══════════════════════════ Scene: Test (full gameplay) ═══════════════════════════

    static void CreateTestScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        // EventSystem
        new GameObject("EventSystem").AddComponent<EventSystem>()
            .gameObject.AddComponent<StandaloneInputModule>();

        new GameObject("GameplayBootstrap").AddComponent<GameplayBootstrap>();

        // ── Game objects (all from prefabs) ──
        // Player / GameCamera：运行时由 GameplayBootstrap 从 Resources 加载
        Spawn(_prefabSpawnPoint, new Vector2(2, 2));
        Spawn(_prefabExitPoint,  new Vector2(9, 9));

        SpawnScaled(_prefabWall, "Wall_Bottom", new Vector2(5, -0.5f),   new Vector3(12, 1, 1));
        SpawnScaled(_prefabWall, "Wall_Top",    new Vector2(5, 10.5f),   new Vector3(12, 1, 1));
        SpawnScaled(_prefabWall, "Wall_Left",   new Vector2(-0.5f, 5),   new Vector3(1, 12, 1));
        SpawnScaled(_prefabWall, "Wall_Right",  new Vector2(10.5f, 5),   new Vector3(1, 12, 1));
        SpawnScaled(_prefabWall, "Wall_Inner",  new Vector2(5, 4f),      new Vector3(6, 0.5f, 1));

        SpawnScaled(_prefabSpike, "Spike", new Vector2(5, 2), new Vector3(3, 0.6f, 1));

        var door = SpawnScaled(_prefabDoor, "Door", new Vector2(7, 6.5f), new Vector3(0.6f, 3f, 1));
        var btn  = Spawn(_prefabButton, new Vector2(3, 6));
        SetRef(btn.GetComponent<PressureButton>(), "linkedDoor", door.GetComponent<Door>());

        Spawn(_prefabMonster, new Vector2(8, 4));

        // LevelPhaseManager（相机引用由 GameplayBootstrap 注入）
        var pmGO = new GameObject("LevelPhaseManager");
        pmGO.AddComponent<LevelPhaseManager>();

        // LevelConfig
        var configGO = new GameObject("LevelConfig");
        configGO.AddComponent<LevelConfig>();

        // UI
        BuildGameUI();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Test.unity");
    }

    static GameObject Spawn(GameObject prefab, Vector2 pos)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.position = new Vector3(pos.x, pos.y, 0);
        return go;
    }

    static GameObject SpawnScaled(GameObject prefab, string name, Vector2 pos, Vector3 scale)
    {
        var go = Spawn(prefab, pos);
        go.name = name;
        go.transform.localScale = scale;
        return go;
    }

    // ═══════════════════════════ Game UI (for level scenes) ═══════════════════════════

    static void BuildGameUI()
    {
        var canvasGO = new GameObject("UICanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Left panel — Drawing Canvas
        var leftPanel = CreatePanel("LeftPanel", canvasGO.transform,
            new Vector2(0, 0), new Vector2(0.5f, 1), new Color(0.12f, 0.12f, 0.15f, 1f));

        var toolbarGO = new GameObject("IconToolbar");
        toolbarGO.transform.SetParent(leftPanel.transform, false);
        var toolbarRT = toolbarGO.AddComponent<RectTransform>();
        toolbarRT.anchorMin = new Vector2(0, 0.9f);
        toolbarRT.anchorMax = Vector2.one;
        toolbarRT.offsetMin = new Vector2(8, 0);
        toolbarRT.offsetMax = new Vector2(-8, -4);
        toolbarGO.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 1f);
        var hlg = toolbarGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.padding = new RectOffset(8, 8, 8, 8);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        toolbarGO.AddComponent<IconToolbar>();

        var canvasAreaGO = new GameObject("CanvasArea");
        canvasAreaGO.transform.SetParent(leftPanel.transform, false);
        var areaRT = canvasAreaGO.AddComponent<RectTransform>();
        areaRT.anchorMin = Vector2.zero;
        areaRT.anchorMax = new Vector2(1, 0.9f);
        areaRT.offsetMin = new Vector2(4, 4);
        areaRT.offsetMax = new Vector2(-4, -4);
        canvasAreaGO.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.22f, 0.9f);

        var dcGO = new GameObject("DrawingCanvasManager");
        dcGO.transform.SetParent(canvasGO.transform, false);
        var dc = dcGO.AddComponent<DrawingCanvas>();
        var dcSO = new SerializedObject(dc);
        dcSO.FindProperty("toolbar").objectReferenceValue = toolbarGO.GetComponent<IconToolbar>();
        dcSO.FindProperty("canvasArea").objectReferenceValue = areaRT;
        dcSO.ApplyModifiedPropertiesWithoutUndo();

        // Right panel — transparent HUD
        var rightPanel = CreatePanel("RightPanel", canvasGO.transform,
            new Vector2(0.5f, 0), new Vector2(1, 1), new Color(0, 0, 0, 0));
        rightPanel.GetComponent<Image>().raycastTarget = false;

        var phaseText = CreateUIText("PhaseText", rightPanel.transform, "阶段一：亮灯探索",
            28, Vector2.zero, Vector2.zero);
        var ptRT = phaseText.GetComponent<RectTransform>();
        ptRT.anchorMin = new Vector2(0, 0.93f);
        ptRT.anchorMax = new Vector2(1, 1);
        ptRT.offsetMin = Vector2.zero;
        ptRT.offsetMax = Vector2.zero;

        // Level Complete panel
        var completePanel = new GameObject("LevelCompletePanel");
        completePanel.transform.SetParent(canvasGO.transform, false);
        var cpRT = completePanel.AddComponent<RectTransform>();
        cpRT.anchorMin = new Vector2(0.3f, 0.35f);
        cpRT.anchorMax = new Vector2(0.7f, 0.65f);
        cpRT.offsetMin = Vector2.zero;
        cpRT.offsetMax = Vector2.zero;
        completePanel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        CreateUIText("CompleteTitle", completePanel.transform, "关卡通关！",
            36, new Vector2(0, 40), new Vector2(300, 60));

        var nextBtn = CreateUIButton("NextLevelButton", completePanel.transform, "下一关",
            new Vector2(-80, -40), new Vector2(140, 50)).GetComponent<Button>();
        var finishBtn = CreateUIButton("FinishButton", completePanel.transform, "完成",
            new Vector2(80, -40), new Vector2(140, 50)).GetComponent<Button>();

        var levelUI = canvasGO.AddComponent<LevelUI>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(nextBtn.onClick, levelUI.OnNextLevel);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(finishBtn.onClick, levelUI.OnFinish);

        var uiSO = new SerializedObject(levelUI);
        uiSO.FindProperty("levelCompletePanel").objectReferenceValue = completePanel;
        uiSO.FindProperty("nextLevelButton").objectReferenceValue = nextBtn;
        uiSO.FindProperty("finishButton").objectReferenceValue = finishBtn;
        uiSO.FindProperty("phaseText").objectReferenceValue = phaseText.GetComponent<Text>();
        uiSO.ApplyModifiedPropertiesWithoutUndo();
    }

    // ═══════════════════════════ UI helpers ═══════════════════════════

    static GameObject CreateUICanvas(string name)
    {
        var go = new GameObject(name);
        go.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    static GameObject CreatePanel(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = color;
        return go;
    }

    static Font GetFont()
    {
        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return f != null ? f : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    static GameObject CreateUIText(string name, Transform parent, string content,
        int fontSize, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = content;
        t.fontSize = fontSize;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.font = GetFont();
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return go;
    }

    static GameObject CreateUIButton(string name, Transform parent, string label,
        Vector2 position, Vector2 size)
    {
        var go = DefaultControls.CreateButton(new DefaultControls.Resources());
        go.name = name;
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().anchoredPosition = position;
        go.GetComponent<RectTransform>().sizeDelta = size;
        var t = go.GetComponentInChildren<Text>();
        t.text = label;
        t.fontSize = 24;
        t.font = GetFont();
        return go;
    }

    static void StretchFill(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ═══════════════════════════ Build Settings ═══════════════════════════

    [MenuItem("Tools/同步关卡到 Build Settings")]
    public static void SyncBuildSettings()
    {
        var guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
        var scenes = new List<EditorBuildSettingsScene>();

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("MainMenu"))
            { scenes.Insert(0, new EditorBuildSettingsScene(path, true)); continue; }
            scenes.Add(new EditorBuildSettingsScene(path, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log($"Build Settings 已更新：共 {scenes.Count} 个场景");
    }
}
