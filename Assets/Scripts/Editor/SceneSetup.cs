using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public static class SceneSetup
{
    static Sprite _placeholder;

    [MenuItem("Tools/生成游戏场景")]
    public static void SetupScenes()
    {
        if (!EditorUtility.DisplayDialog(
            "生成游戏场景",
            "将创建 MainMenu、Level_01、Level_02、Test 四个场景，并配置 Build Settings。\n继续？",
            "确定", "取消"))
            return;

        EnsurePlaceholderSprite();
        EnsureProjectilePrefab();

        CreateMainMenuScene();
        CreateLevelScene("Level_01");
        CreateLevelScene("Level_02");
        CreateTestScene();
        SyncBuildSettings();

        EditorSceneManager.OpenScene("Assets/Scenes/Test.unity");
        Debug.Log("游戏场景生成完毕！已打开 Test 场景，可直接 Play 试玩。");
    }

    // ─────────────────────────── Placeholder assets ───────────────────────────

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

        System.IO.File.WriteAllBytes(
            System.IO.Path.GetFullPath(path), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spritePixelsPerUnit = 4;
            imp.filterMode = FilterMode.Point;
            imp.SaveAndReimport();
        }

        _placeholder = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (_placeholder == null)
            Debug.LogError("占位 Sprite 创建失败！请重新运行 Tools > 生成游戏场景");
        else
            Debug.Log("占位 Sprite 创建成功");
    }

    static void EnsureProjectilePrefab()
    {
        const string path = "Assets/Prefabs/Projectile.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            AssetDatabase.DeleteAsset(path);

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        var go = new GameObject("Projectile");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _placeholder;
        sr.color = Color.yellow;
        go.transform.localScale = Vector3.one * 0.5f;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.4f;
        go.AddComponent<Projectile>();

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    static Sprite[] CreateTestIconSprites()
    {
        string[] names = { "Icon_Trap", "Icon_Door", "Icon_Monster", "Icon_Exit" };
        Color[] colors = { Color.red, new Color(0.6f, 0.4f, 0.2f), Color.magenta, Color.cyan };
        var sprites = new Sprite[names.Length];

        for (int i = 0; i < names.Length; i++)
        {
            string path = $"Assets/Sprites/{names[i]}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) { sprites[i] = existing; continue; }

            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = tex.GetPixels();
            for (int j = 0; j < px.Length; j++) px[j] = colors[i];
            tex.SetPixels(px);
            tex.Apply();

            System.IO.File.WriteAllBytes(
                System.IO.Path.GetFullPath(path), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spritePixelsPerUnit = 4;
                imp.filterMode = FilterMode.Point;
                imp.SaveAndReimport();
            }
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return sprites;
    }

    // ─────────────────────────── MainMenu scene ───────────────────────────

    static void CreateMainMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        var canvasGO = CreateUICanvas("Canvas");

        // Background
        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.2f, 1f);
        StretchFill(bgGO);

        // Title
        CreateUIText("Title", canvasGO.transform, "西格纳路",
            60, new Vector2(0, 100), new Vector2(600, 100));

        // Start Button
        var startBtnGO = CreateUIButton("StartButton", canvasGO.transform, "开始游戏",
            new Vector2(0, -50), new Vector2(240, 64));

        // MainMenuUI
        var menuUI = canvasGO.AddComponent<MainMenuUI>();
        var startBtn = startBtnGO.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(startBtn.onClick, menuUI.OnStartGame);

        var menuSO = new SerializedObject(menuUI);
        menuSO.FindProperty("background").objectReferenceValue = bgImage;
        menuSO.ApplyModifiedPropertiesWithoutUndo();

        // LevelManager
        var managerGO = new GameObject("LevelManager");
        var lm = managerGO.AddComponent<LevelManager>();
        var so = new SerializedObject(lm);
        var levelsProp = so.FindProperty("levelScenes");
        levelsProp.arraySize = 2;
        levelsProp.GetArrayElementAtIndex(0).stringValue = "Level_01";
        levelsProp.GetArrayElementAtIndex(1).stringValue = "Level_02";
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
    }

    // ─────────────────────────── Level scene (template) ───────────────────────────

    static void CreateLevelScene(string sceneName)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        var canvasGO = CreateUICanvas("Canvas");

        CreateUIText("LevelTitle", canvasGO.transform, sceneName,
            48, new Vector2(0, 200), new Vector2(400, 80));

        var levelUIGO = new GameObject("LevelUI");
        levelUIGO.transform.SetParent(canvasGO.transform, false);
        levelUIGO.AddComponent<RectTransform>().anchoredPosition = new Vector2(0, -100);
        var levelUI = levelUIGO.AddComponent<LevelUI>();

        var nextBtnGO = CreateUIButton("NextLevelButton", levelUIGO.transform, "下一关",
            Vector2.zero, new Vector2(200, 60));
        var nextBtn = nextBtnGO.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(nextBtn.onClick, levelUI.OnNextLevel);

        var finishBtnGO = CreateUIButton("FinishButton", levelUIGO.transform, "完成",
            Vector2.zero, new Vector2(200, 60));
        var finishBtn = finishBtnGO.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(finishBtn.onClick, levelUI.OnFinish);

        var uiSO = new SerializedObject(levelUI);
        uiSO.FindProperty("nextLevelButton").objectReferenceValue = nextBtn;
        uiSO.FindProperty("finishButton").objectReferenceValue = finishBtn;
        uiSO.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, $"Assets/Scenes/{sceneName}.unity");
    }

    // ─────────────────────────── TEST scene (full gameplay) ───────────────────────────

    static void CreateTestScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var iconSprites = CreateTestIconSprites();

        // ── Camera ──
        var camGO = new GameObject("GameCamera");
        camGO.transform.position = new Vector3(5, 5, -10);
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.15f, 0.18f, 0.15f);
        camGO.AddComponent<AudioListener>();
        var topDown = camGO.AddComponent<TopDownCamera>();

        // ── EventSystem ──
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        // ── Player ──
        var playerGO = CreateSpriteObject("Player", new Vector2(2, 2), Color.cyan);
        playerGO.tag = "Player";
        playerGO.GetComponent<SpriteRenderer>().sortingOrder = 10;
        var rb = playerGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var playerCol = playerGO.AddComponent<BoxCollider2D>();
        var pc = playerGO.AddComponent<PlayerController>();

        var projPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectile.prefab");
        var pcSO = new SerializedObject(pc);
        pcSO.FindProperty("projectilePrefab").objectReferenceValue = projPrefab;
        pcSO.ApplyModifiedPropertiesWithoutUndo();

        // ── SpawnPoint ──
        var spawnGO = CreateSpriteObject("SpawnPoint", new Vector2(2, 2), Color.green);
        spawnGO.GetComponent<SpriteRenderer>().sortingOrder = -1;
        spawnGO.AddComponent<SpawnPoint>();
        spawnGO.AddComponent<DarkPhaseHideable>();

        // ── ExitPoint ──
        var exitGO = CreateSpriteObject("ExitPoint", new Vector2(9, 9), Color.yellow);
        exitGO.transform.localScale = Vector3.one * 1.2f;
        var exitCol = exitGO.AddComponent<BoxCollider2D>();
        exitCol.isTrigger = true;
        exitGO.AddComponent<ExitPoint>();
        exitGO.AddComponent<DarkPhaseHideable>();

        // ── Walls (boundary) ──
        CreateWall("Wall_Bottom", new Vector2(5, -0.5f),  new Vector2(12, 1));
        CreateWall("Wall_Top",    new Vector2(5, 10.5f),   new Vector2(12, 1));
        CreateWall("Wall_Left",   new Vector2(-0.5f, 5),   new Vector2(1, 12));
        CreateWall("Wall_Right",  new Vector2(10.5f, 5),   new Vector2(1, 12));
        CreateWall("Wall_Inner1", new Vector2(5, 4f),      new Vector2(6, 0.5f));

        // ── Spike ──
        var spikeGO = CreateSpriteObject("Spike", new Vector2(5, 2), new Color(1f, 0.2f, 0.2f));
        spikeGO.transform.localScale = new Vector3(3, 0.6f, 1);
        var spikeCol = spikeGO.AddComponent<BoxCollider2D>();
        spikeCol.isTrigger = true;
        spikeGO.AddComponent<Spike>();
        spikeGO.AddComponent<DarkPhaseHideable>();

        // ── Door ──
        var doorGO = CreateSpriteObject("Door", new Vector2(7, 6.5f), new Color(0.6f, 0.4f, 0.2f));
        doorGO.transform.localScale = new Vector3(0.6f, 3f, 1);
        var doorCol = doorGO.AddComponent<BoxCollider2D>();
        doorGO.AddComponent<Door>();
        doorGO.AddComponent<DarkPhaseHideable>();

        // ── Button ──
        var btnGO = CreateSpriteObject("PressureButton", new Vector2(3, 6), Color.red);
        var btnCol = btnGO.AddComponent<BoxCollider2D>();
        btnCol.isTrigger = true;
        var pressBtn = btnGO.AddComponent<PressureButton>();
        btnGO.AddComponent<DarkPhaseHideable>();

        var pbSO = new SerializedObject(pressBtn);
        pbSO.FindProperty("linkedDoor").objectReferenceValue = doorGO.GetComponent<Door>();
        pbSO.ApplyModifiedPropertiesWithoutUndo();

        // ── Monster (placeholder) ──
        var monsterGO = CreateSpriteObject("Monster", new Vector2(8, 4), Color.magenta);
        monsterGO.transform.localScale = Vector3.one * 1.2f;
        monsterGO.AddComponent<BoxCollider2D>().isTrigger = true;
        monsterGO.AddComponent<Monster>();

        // ── LevelPhaseManager ──
        var pmGO = new GameObject("LevelPhaseManager");
        var pm = pmGO.AddComponent<LevelPhaseManager>();
        var pmSO = new SerializedObject(pm);
        pmSO.FindProperty("topDownCamera").objectReferenceValue = topDown;
        pmSO.ApplyModifiedPropertiesWithoutUndo();

        // ── LevelConfig ──
        var configGO = new GameObject("LevelConfig");
        var config = configGO.AddComponent<LevelConfig>();
        var cfgSO = new SerializedObject(config);
        var iconsProp = cfgSO.FindProperty("availableIcons");
        iconsProp.arraySize = iconSprites.Length;
        for (int i = 0; i < iconSprites.Length; i++)
            iconsProp.GetArrayElementAtIndex(i).objectReferenceValue = iconSprites[i];
        cfgSO.ApplyModifiedPropertiesWithoutUndo();

        // ── UI Canvas (full screen overlay) ──
        BuildGameUI(camGO, pm);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Test.unity");
    }

    // ─────────────────────────── UI builder for gameplay scenes ───────────────────────────

    static void BuildGameUI(GameObject cameraGO, LevelPhaseManager pm)
    {
        var canvasGO = new GameObject("UICanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Left panel (Drawing Canvas) ──
        var leftPanel = CreatePanel("LeftPanel", canvasGO.transform,
            new Vector2(0, 0), new Vector2(0.5f, 1), new Color(0.12f, 0.12f, 0.15f, 1f));

        // Toolbar at top
        var toolbarGO = new GameObject("IconToolbar");
        toolbarGO.transform.SetParent(leftPanel.transform, false);
        var toolbarRT = toolbarGO.AddComponent<RectTransform>();
        toolbarRT.anchorMin = new Vector2(0, 0.9f);
        toolbarRT.anchorMax = Vector2.one;
        toolbarRT.offsetMin = new Vector2(8, 0);
        toolbarRT.offsetMax = new Vector2(-8, -4);
        var toolbarBg = toolbarGO.AddComponent<Image>();
        toolbarBg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        var hlg = toolbarGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.padding = new RectOffset(8, 8, 8, 8);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        toolbarGO.AddComponent<IconToolbar>();

        // Canvas drawing area below toolbar
        var canvasAreaGO = new GameObject("CanvasArea");
        canvasAreaGO.transform.SetParent(leftPanel.transform, false);
        var areaRT = canvasAreaGO.AddComponent<RectTransform>();
        areaRT.anchorMin = Vector2.zero;
        areaRT.anchorMax = new Vector2(1, 0.9f);
        areaRT.offsetMin = new Vector2(4, 4);
        areaRT.offsetMax = new Vector2(-4, -4);
        var areaBg = canvasAreaGO.AddComponent<Image>();
        areaBg.color = new Color(0.18f, 0.18f, 0.22f, 0.9f);

        // DrawingCanvas component
        var dcGO = new GameObject("DrawingCanvasManager");
        dcGO.transform.SetParent(canvasGO.transform, false);
        var dc = dcGO.AddComponent<DrawingCanvas>();
        var dcSO = new SerializedObject(dc);
        dcSO.FindProperty("toolbar").objectReferenceValue = toolbarGO.GetComponent<IconToolbar>();
        dcSO.FindProperty("canvasArea").objectReferenceValue = areaRT;
        dcSO.ApplyModifiedPropertiesWithoutUndo();

        // ── Right panel (transparent game HUD) ──
        var rightPanel = CreatePanel("RightPanel", canvasGO.transform,
            new Vector2(0.5f, 0), new Vector2(1, 1), new Color(0, 0, 0, 0));
        rightPanel.GetComponent<Image>().raycastTarget = false;

        // Phase indicator text (top of right panel)
        var phaseText = CreateUIText("PhaseText", rightPanel.transform, "阶段一：亮灯探索",
            28, Vector2.zero, Vector2.zero);
        var ptRT = phaseText.GetComponent<RectTransform>();
        ptRT.anchorMin = new Vector2(0, 0.93f);
        ptRT.anchorMax = new Vector2(1, 1);
        ptRT.offsetMin = Vector2.zero;
        ptRT.offsetMax = Vector2.zero;

        // ── Level Complete Panel (centered, hidden) ──
        var completePanel = new GameObject("LevelCompletePanel");
        completePanel.transform.SetParent(canvasGO.transform, false);
        var cpRT = completePanel.AddComponent<RectTransform>();
        cpRT.anchorMin = new Vector2(0.3f, 0.35f);
        cpRT.anchorMax = new Vector2(0.7f, 0.65f);
        cpRT.offsetMin = Vector2.zero;
        cpRT.offsetMax = Vector2.zero;
        var cpBg = completePanel.AddComponent<Image>();
        cpBg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        CreateUIText("CompleteTitle", completePanel.transform, "关卡通关！",
            36, new Vector2(0, 40), new Vector2(300, 60));

        var nextBtnGO = CreateUIButton("NextLevelButton", completePanel.transform, "下一关",
            new Vector2(-80, -40), new Vector2(140, 50));
        var finishBtnGO = CreateUIButton("FinishButton", completePanel.transform, "完成",
            new Vector2(80, -40), new Vector2(140, 50));

        // LevelUI
        var levelUI = canvasGO.AddComponent<LevelUI>();
        var nextBtn = nextBtnGO.GetComponent<Button>();
        var finishBtn = finishBtnGO.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(nextBtn.onClick, levelUI.OnNextLevel);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(finishBtn.onClick, levelUI.OnFinish);

        var uiSO = new SerializedObject(levelUI);
        uiSO.FindProperty("levelCompletePanel").objectReferenceValue = completePanel;
        uiSO.FindProperty("nextLevelButton").objectReferenceValue = nextBtn;
        uiSO.FindProperty("finishButton").objectReferenceValue = finishBtn;
        uiSO.FindProperty("phaseText").objectReferenceValue = phaseText.GetComponent<Text>();
        uiSO.ApplyModifiedPropertiesWithoutUndo();
    }

    // ─────────────────────────── Helper: game world objects ───────────────────────────

    static GameObject CreateSpriteObject(string name, Vector2 pos, Color color)
    {
        var go = new GameObject(name);
        go.transform.position = new Vector3(pos.x, pos.y, 0);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _placeholder;
        sr.color = color;
        if (_placeholder == null)
            Debug.LogWarning($"[SceneSetup] {name}: 占位 sprite 为 null，物体将不可见！");
        return go;
    }

    static void CreateWall(string name, Vector2 pos, Vector2 size)
    {
        var go = CreateSpriteObject(name, pos, new Color(0.5f, 0.5f, 0.5f));
        go.transform.localScale = new Vector3(size.x, size.y, 1);
        go.AddComponent<BoxCollider2D>();
        go.AddComponent<DarkPhaseHideable>();
    }

    // ─────────────────────────── Helper: UI elements ───────────────────────────

    static GameObject CreateUICanvas(string name)
    {
        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    static Font GetFont()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }

    static GameObject CreateUIText(string name, Transform parent, string content,
        int fontSize, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.font = GetFont();
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
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        var text = go.GetComponentInChildren<Text>();
        text.text = label;
        text.fontSize = 24;
        text.font = GetFont();
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

    // ─────────────────────────── Build Settings ───────────────────────────

    [MenuItem("Tools/同步关卡到 Build Settings")]
    public static void SyncBuildSettings()
    {
        var guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
        var scenes = new List<EditorBuildSettingsScene>();

        // MainMenu first
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("MainMenu"))
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
                break;
            }
        }

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.Contains("MainMenu"))
                scenes.Add(new EditorBuildSettingsScene(path, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log($"Build Settings 已更新：共 {scenes.Count} 个场景");
    }
}
