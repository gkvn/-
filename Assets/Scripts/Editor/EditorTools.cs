using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using NavMeshPlus.Components;
using NavMeshPlus.Extensions;

public static class EditorTools
{
    [MenuItem("Tools/切换场景中 UI 显示 %h")]
    public static void ToggleUISceneVisibility()
    {
        var canvases = Object.FindObjectsOfType<Canvas>(true);
        if (canvases.Length == 0)
        {
            Debug.Log("场景中没有 Canvas");
            return;
        }

        var svm = SceneVisibilityManager.instance;
        bool anyVisible = false;
        foreach (var c in canvases)
        {
            if (!svm.IsHidden(c.gameObject))
            {
                anyVisible = true;
                break;
            }
        }

        foreach (var c in canvases)
        {
            if (anyVisible)
                svm.Hide(c.gameObject, true);
            else
                svm.Show(c.gameObject, true);
        }

        Debug.Log(anyVisible ? "UI 已在 Scene 视图中隐藏" : "UI 已在 Scene 视图中显示");
    }

    [MenuItem("Tools/创建 SignPost Prefab")]
    public static void CreateSignPostPrefab()
    {
        string folder = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        string path = folder + "/SignPost.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            Debug.Log("SignPost.prefab 已存在：" + path);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return;
        }

        var go = new GameObject("SignPost");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = RuntimeSprite.Get();
        sr.color = new Color(0.6f, 0.45f, 0.2f);

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = false;

        go.AddComponent<SignPost>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        Selection.activeObject = prefab;
        Debug.Log("已创建 SignPost Prefab：" + path);
    }

    [MenuItem("Tools/创建 GlowPoint Prefab")]
    public static void CreateGlowPointPrefab()
    {
        string folder = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        string path = folder + "/GlowPoint.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            Debug.Log("GlowPoint.prefab 已存在：" + path);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return;
        }

        var go = new GameObject("GlowPoint");
        go.AddComponent<GlowPoint>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        Selection.activeObject = prefab;
        Debug.Log("已创建 GlowPoint Prefab：" + path);
    }

    [MenuItem("Tools/配置帧动画到 Player 和 Monster Prefab")]
    public static void AssignFrameAnimations()
    {
        string[] animFolders = new[]
        {
            "Assets/Art/animation/角色1",
            "Assets/Art/animation/角色2",
            "Assets/Art/animation/猫",
            "Assets/Art/animation/狗",
            "Assets/Art/animation/鸟"
        };
        EnsureSpritesImported(animFolders);

        int changes = 0;

        // Player prefab
        string playerPath = "Assets/Resources/Prefabs/Player.prefab";
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPath);
        if (playerPrefab != null)
        {
            var pc = playerPrefab.GetComponent<PlayerController>();
            if (pc != null)
            {
                var so = new SerializedObject(pc);

                var darkFramesProp = so.FindProperty("darkFrames");
                var lightFramesProp = so.FindProperty("lightFrames");
                var animFpsProp = so.FindProperty("animFps");
                var lightSpriteProp = so.FindProperty("lightSprite");
                var darkSpriteProp = so.FindProperty("darkSprite");

                var dark = LoadSortedSprites("Assets/Art/animation/角色1");
                var light = LoadSortedSprites("Assets/Art/animation/角色2");

                if (dark.Length > 0)
                {
                    darkFramesProp.arraySize = dark.Length;
                    for (int i = 0; i < dark.Length; i++)
                        darkFramesProp.GetArrayElementAtIndex(i).objectReferenceValue = dark[i];
                    darkSpriteProp.objectReferenceValue = null;
                }

                if (light.Length > 0)
                {
                    lightFramesProp.arraySize = light.Length;
                    for (int i = 0; i < light.Length; i++)
                        lightFramesProp.GetArrayElementAtIndex(i).objectReferenceValue = light[i];
                    lightSpriteProp.objectReferenceValue = null;
                }

                animFpsProp.floatValue = 3f;
                so.ApplyModifiedProperties();

                var sr = playerPrefab.GetComponent<SpriteRenderer>();
                if (sr != null && light.Length > 0)
                {
                    sr.sprite = light[0];
                    sr.color = Color.white;
                    EditorUtility.SetDirty(sr);
                }

                EditorUtility.SetDirty(playerPrefab);
                changes++;
                Debug.Log($"[Player] darkFrames={dark.Length}, lightFrames={light.Length}, fps=3, 旧单张贴图已清除");
            }
        }
        else
        {
            Debug.LogWarning("找不到 Player Prefab: " + playerPath);
        }

        // Monster prefab
        string monsterPath = "Assets/Prefabs/Monster.prefab";
        var monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(monsterPath);
        if (monsterPrefab != null)
        {
            var monster = monsterPrefab.GetComponent<Monster>();
            if (monster != null)
            {
                var so = new SerializedObject(monster);
                var catFramesProp = so.FindProperty("catFrames");
                var dogFramesProp = so.FindProperty("dogFrames");
                var birdFramesProp = so.FindProperty("birdFrames");
                var fpsProp = so.FindProperty("animFps");

                var catSprites = LoadSortedSprites("Assets/Art/animation/猫");
                var dogSprites = LoadSortedSprites("Assets/Art/animation/狗");
                var birdSprites = LoadSortedSprites("Assets/Art/animation/鸟");

                if (catSprites.Length > 0)
                {
                    catFramesProp.arraySize = catSprites.Length;
                    for (int i = 0; i < catSprites.Length; i++)
                        catFramesProp.GetArrayElementAtIndex(i).objectReferenceValue = catSprites[i];
                }

                if (dogSprites.Length > 0)
                {
                    dogFramesProp.arraySize = dogSprites.Length;
                    for (int i = 0; i < dogSprites.Length; i++)
                        dogFramesProp.GetArrayElementAtIndex(i).objectReferenceValue = dogSprites[i];
                }

                if (birdSprites.Length > 0)
                {
                    birdFramesProp.arraySize = birdSprites.Length;
                    for (int i = 0; i < birdSprites.Length; i++)
                        birdFramesProp.GetArrayElementAtIndex(i).objectReferenceValue = birdSprites[i];
                }

                fpsProp.floatValue = 3f;
                so.ApplyModifiedProperties();

                var sr = monsterPrefab.GetComponent<SpriteRenderer>();
                if (sr != null && catSprites.Length > 0)
                {
                    sr.sprite = catSprites[0];
                    sr.color = Color.white;
                    EditorUtility.SetDirty(sr);
                }

                EditorUtility.SetDirty(monsterPrefab);
                changes++;
                Debug.Log($"[Monster] catFrames={catSprites.Length}, dogFrames={dogSprites.Length}, birdFrames={birdSprites.Length}, fps=3");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"帧动画配置完成，修改了 {changes} 个 Prefab。\n" +
                  "Monster 已挂好猫和狗两组帧动画，在 Inspector 中切换 Monster Type 即可。");
    }

    private static void EnsureSpritesImported(string[] folders)
    {
        bool reimport = false;
        foreach (var folder in folders)
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                if (importer.textureType != TextureImporterType.Sprite
                    || importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                    reimport = true;
                }
            }
        }
        if (reimport)
        {
            AssetDatabase.Refresh();
            Debug.Log("已将动画图片全部设为 Sprite 格式并重新导入");
        }
    }

    [MenuItem("Tools/设置动画图片 Pixels Per Unit")]
    public static void SetAnimationPPU()
    {
        PixelsPerUnitWindow.Open();
    }

    private static Sprite[] LoadSortedSprites(string folderPath)
    {
        var guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
        var sprites = new System.Collections.Generic.List<Sprite>();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (spr != null) sprites.Add(spr);
        }

        sprites.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
        return sprites.ToArray();
    }

    [MenuItem("Tools/在 MainMenu 添加语言切换按钮")]
    public static void AddLanguageButton()
    {
        var menuUI = Object.FindObjectOfType<MainMenuUI>();
        if (menuUI == null)
        {
            Debug.LogError("当前场景没有 MainMenuUI，请先打开 MainMenu 场景");
            return;
        }

        if (Object.FindObjectOfType<LanguageManager>() == null)
        {
            var lmGo = new GameObject("LanguageManager");
            lmGo.AddComponent<LanguageManager>();
            Undo.RegisterCreatedObjectUndo(lmGo, "Create LanguageManager");
            Debug.Log("已创建 LanguageManager 物体");
        }

        var canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("场景中没有 Canvas");
            return;
        }

        var btnGo = new GameObject("LanguageButton");
        Undo.RegisterCreatedObjectUndo(btnGo, "Create Language Button");
        btnGo.transform.SetParent(canvas.transform, false);

        var rt = btnGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-20, -20);
        rt.sizeDelta = new Vector2(200, 50);

        var img = btnGo.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        var btn = btnGo.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f, 0.9f);
        btn.colors = colors;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(btnGo.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        var text = textGo.AddComponent<Text>();
        text.text = "中文 / English";
        text.font = Resources.Load<Font>("Fonts/NotoSansSC-Regular");
        if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 22;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        UnityEventTools.AddPersistentListener(btn.onClick,
            new UnityEngine.Events.UnityAction(menuUI.OnToggleLanguage));

        var so = new SerializedObject(menuUI);
        var prop = so.FindProperty("languageButtonText");
        if (prop != null)
        {
            prop.objectReferenceValue = text;
            so.ApplyModifiedProperties();
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = btnGo;
        Debug.Log("已添加语言切换按钮并自动关联 MainMenuUI");
    }

    [MenuItem("Tools/创建并烘焙 NavMesh 2D")]
    public static void CreateAndBakeNavMesh2D()
    {
        PrepareNavMeshScene();

        foreach (var wallCol in Object.FindObjectsOfType<WallSpriteCollider2D>(true))
            wallCol.SyncColliderNow();

        var surface = Object.FindObjectOfType<NavMeshSurface>();
        if (surface == null)
        {
            var go = new GameObject("NavMesh2D");
            go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

            surface = go.AddComponent<NavMeshSurface>();
            surface.collectObjects = NavMeshPlus.Components.CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.defaultArea = 0;

            go.AddComponent<CollectSources2d>();
        }

        surface.agentTypeID = 0;
        surface.BuildNavMesh();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        var settings = NavMesh.GetSettingsByID(0);
        Debug.Log($"NavMesh 2D 烘焙完成！agentRadius={settings.agentRadius}  按 Ctrl+S 保存场景。");
    }

    [MenuItem("Tools/设置 NavMesh Agent 半径")]
    public static void SetNavMeshAgentRadius()
    {
        NavAgentRadiusWindow.Open();
    }

    static void PrepareNavMeshScene()
    {
        EnsureNavFloor();
        MarkWallsNotWalkable();
    }

    static void EnsureNavFloor()
    {
        const string floorName = "NavFloor";
        var existing = GameObject.Find(floorName);
        if (existing == null)
        {
            existing = new GameObject(floorName);
            existing.AddComponent<BoxCollider2D>();
        }

        var allColliders = Object.FindObjectsOfType<Collider2D>();
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool first = true;
        foreach (var c in allColliders)
        {
            if (c.gameObject == existing) continue;
            if (first) { bounds = c.bounds; first = false; }
            else bounds.Encapsulate(c.bounds);
        }

        if (first)
        {
            Debug.LogWarning("场景中没有找到任何碰撞体，无法计算地面范围。");
            return;
        }

        float padding = 3f;
        existing.transform.position = new Vector3(bounds.center.x, bounds.center.y, 0f);
        var box = existing.GetComponent<BoxCollider2D>();
        box.size = new Vector2(bounds.size.x + padding * 2, bounds.size.y + padding * 2);
        box.isTrigger = true;

        var mod = existing.GetComponent<NavMeshModifier>();
        if (mod == null) mod = existing.AddComponent<NavMeshModifier>();
        mod.overrideArea = true;
        mod.area = 0;

        Debug.Log($"NavFloor 地面：center={bounds.center}, size={box.size}");
    }

    static void MarkWallsNotWalkable()
    {
        int wallCount = 0;

        var wallRoot = GameObject.Find("Wall");
        if (wallRoot != null)
        {
            wallCount += MarkChildrenNotWalkable(wallRoot.transform);
        }

        var allColliders = Object.FindObjectsOfType<Collider2D>();
        foreach (var col in allColliders)
        {
            if (col.isTrigger) continue;
            if (col.GetComponent<NavMeshModifier>() != null) continue;

            bool isWall = col.gameObject.name.Contains("Wall") || col.gameObject.name.Contains("wall");
            if (!isWall && wallRoot != null && col.transform.IsChildOf(wallRoot.transform))
                isWall = true;

            if (isWall)
            {
                var mod = col.gameObject.AddComponent<NavMeshModifier>();
                mod.overrideArea = true;
                mod.area = 1;
                wallCount++;
            }
        }

        Debug.Log($"已标记 {wallCount} 个墙壁为 Not Walkable");
    }

    static int MarkChildrenNotWalkable(Transform parent)
    {
        int count = 0;
        var colliders = parent.GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders)
        {
            if (col.isTrigger) continue;
            if (col.GetComponent<NavMeshModifier>() != null) continue;

            var mod = col.gameObject.AddComponent<NavMeshModifier>();
            mod.overrideArea = true;
            mod.area = 1;
            count++;
        }
        return count;
    }

    // ── LevelGlobals Prefab ──

    private const string LevelGlobalsPrefabPath = "Assets/Prefabs/LevelGlobals.prefab";

    [MenuItem("Tools/创建 LevelGlobals Prefab")]
    public static void CreateLevelGlobalsPrefab()
    {
        string folder = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(LevelGlobalsPrefabPath);
        if (existing != null)
        {
            Debug.Log("LevelGlobals.prefab 已存在：" + LevelGlobalsPrefabPath);
            Selection.activeObject = existing;
            return;
        }

        var go = new GameObject("LevelGlobals");
        go.AddComponent<LevelPhaseManager>();
        go.AddComponent<CursorManager>();
        go.AddComponent<DarkPhaseParticles>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, LevelGlobalsPrefabPath);
        Object.DestroyImmediate(go);

        Selection.activeObject = prefab;
        Debug.Log("已创建 LevelGlobals Prefab，包含 LevelPhaseManager + CursorManager + DarkPhaseParticles");
    }

    [MenuItem("Tools/同步 LevelGlobals 到所有关卡场景")]
    public static void SyncLevelGlobalsToAllScenes()
    {
        const string sourceScenePath = "Assets/Scenes/level_xm_1.unity";
        if (!System.IO.File.Exists(sourceScenePath))
        {
            Debug.LogError("找不到源场景 level_xm_1，请确认 Assets/Scenes/level_xm_1.unity 存在");
            return;
        }

        var currentScene = EditorSceneManager.GetActiveScene().path;

        var srcScene = EditorSceneManager.OpenScene(sourceScenePath, OpenSceneMode.Single);
        GameObject srcGlobals = null;
        foreach (var root in srcScene.GetRootGameObjects())
        {
            if (root.GetComponent<LevelPhaseManager>() != null)
            {
                srcGlobals = root;
                break;
            }
        }

        if (srcGlobals == null)
        {
            Debug.LogError("level_xm_1 场景中没有找到 LevelGlobals（含 LevelPhaseManager 的根物体）");
            return;
        }

        var tempPrefabPath = "Assets/Prefabs/_TempLevelGlobalsSync.prefab";
        PrefabUtility.SaveAsPrefabAsset(srcGlobals, tempPrefabPath);
        var tempPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(tempPrefabPath);

        var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
        int updated = 0;

        foreach (var guid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            if (sceneName == "MainMenu" || sceneName == "SampleScene" || scenePath == sourceScenePath)
                continue;

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            GameObject oldGlobals = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponent<LevelPhaseManager>() != null)
                {
                    oldGlobals = root;
                    break;
                }
            }

            if (oldGlobals != null)
                Object.DestroyImmediate(oldGlobals);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(tempPrefab, scene);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = "LevelGlobals";

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            updated++;
            Debug.Log($"  [{sceneName}] 已从 level_xm_1 同步 LevelGlobals");
        }

        AssetDatabase.DeleteAsset(tempPrefabPath);
        var metaPath = tempPrefabPath + ".meta";
        if (System.IO.File.Exists(metaPath))
            AssetDatabase.DeleteAsset(metaPath);

        if (!string.IsNullOrEmpty(currentScene))
            EditorSceneManager.OpenScene(currentScene, OpenSceneMode.Single);

        Debug.Log($"已将 level_xm_1 的 LevelGlobals 同步到 {updated} 个关卡场景。");
    }

    [MenuItem("Tools/批量设置 Monster Combo Sequence")]
    public static void OpenMonsterComboEditor()
    {
        MonsterComboWindow.Open();
    }

    [MenuItem("Tools/创建 ImagePanel Prefab")]
    public static void CreateImagePanelPrefab()
    {
        string folder = "Assets/Resources/Prefabs";
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");

        string path = folder + "/ImagePanel.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            Debug.Log("ImagePanel.prefab 已存在：" + path);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return;
        }

        var root = new GameObject("ImagePanel");
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        // ── ShowButton: 打开面板的按钮 ──
        var showBtnGo = new GameObject("ShowButton");
        showBtnGo.transform.SetParent(root.transform, false);
        var showRect = showBtnGo.AddComponent<RectTransform>();
        showRect.anchorMin = new Vector2(0.5f, 0f);
        showRect.anchorMax = new Vector2(0.5f, 0f);
        showRect.pivot = new Vector2(0.5f, 0f);
        showRect.anchoredPosition = new Vector2(0f, 60f);
        showRect.sizeDelta = new Vector2(300f, 80f);

        var showImg = showBtnGo.AddComponent<Image>();
        showImg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        showBtnGo.AddComponent<Button>();

        var showTextGo = new GameObject("Text");
        showTextGo.transform.SetParent(showBtnGo.transform, false);
        var showTxtRect = showTextGo.AddComponent<RectTransform>();
        showTxtRect.anchorMin = Vector2.zero;
        showTxtRect.anchorMax = Vector2.one;
        showTxtRect.offsetMin = Vector2.zero;
        showTxtRect.offsetMax = Vector2.zero;
        var showText = showTextGo.AddComponent<Text>();
        showText.text = "查看图片";
        showText.font = LoadFont();
        showText.fontSize = 36;
        showText.alignment = TextAnchor.MiddleCenter;
        showText.color = Color.white;

        // ── Panel: 全屏图片面板（默认隐藏） ──
        var panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(root.transform, false);
        var panelRect = panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImg = panelGo.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.85f);

        // ── DisplayImage: 中央展示的图片 ──
        var displayGo = new GameObject("DisplayImage");
        displayGo.transform.SetParent(panelGo.transform, false);
        var displayRect = displayGo.AddComponent<RectTransform>();
        displayRect.anchorMin = new Vector2(0.1f, 0.1f);
        displayRect.anchorMax = new Vector2(0.9f, 0.9f);
        displayRect.offsetMin = Vector2.zero;
        displayRect.offsetMax = Vector2.zero;
        var displayImg = displayGo.AddComponent<Image>();
        displayImg.color = new Color(1f, 1f, 1f, 0.9f);
        displayImg.preserveAspect = true;

        // ── BackButton: 返回按钮 ──
        var backBtnGo = new GameObject("BackButton");
        backBtnGo.transform.SetParent(panelGo.transform, false);
        var backRect = backBtnGo.AddComponent<RectTransform>();
        backRect.anchorMin = new Vector2(1f, 1f);
        backRect.anchorMax = new Vector2(1f, 1f);
        backRect.pivot = new Vector2(1f, 1f);
        backRect.anchoredPosition = new Vector2(-30f, -30f);
        backRect.sizeDelta = new Vector2(120f, 60f);

        var backImg = backBtnGo.AddComponent<Image>();
        backImg.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);
        backBtnGo.AddComponent<Button>();

        var backTextGo = new GameObject("Text");
        backTextGo.transform.SetParent(backBtnGo.transform, false);
        var backTxtRect = backTextGo.AddComponent<RectTransform>();
        backTxtRect.anchorMin = Vector2.zero;
        backTxtRect.anchorMax = Vector2.one;
        backTxtRect.offsetMin = Vector2.zero;
        backTxtRect.offsetMax = Vector2.zero;
        var backText = backTextGo.AddComponent<Text>();
        backText.text = "返回";
        backText.font = LoadFont();
        backText.fontSize = 30;
        backText.alignment = TextAnchor.MiddleCenter;
        backText.color = Color.white;

        panelGo.SetActive(false);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        Selection.activeObject = prefab;
        Debug.Log("已创建 ImagePanel Prefab：" + path +
                  "\n结构：ImagePanel / ShowButton（查看图片按钮）+ Panel / DisplayImage（图片）+ BackButton（返回）" +
                  "\n双击 Prefab 即可修改美术、调整布局");
    }

    private static Font LoadFont()
    {
        var font = Resources.Load<Font>("Fonts/NotoSansSC-Regular");
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }

    [MenuItem("Tools/添加 BgmManager 到 MainMenu 场景")]
    public static void AddBgmManager()
    {
        if (Object.FindObjectOfType<BgmManager>() != null)
        {
            Debug.Log("场景中已存在 BgmManager");
            Selection.activeGameObject = Object.FindObjectOfType<BgmManager>().gameObject;
            return;
        }

        var go = new GameObject("BgmManager");
        go.AddComponent<BgmManager>();
        Undo.RegisterCreatedObjectUndo(go, "Create BgmManager");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = go;
        Debug.Log("已创建 BgmManager，请在 Inspector 中拖入 BGM AudioClip（主菜单、白天、黑夜）");
    }

    public static void SetHumanoidAgentRadius(float radius)
    {
        const string path = "ProjectSettings/NavMeshAreas.asset";
        var text = System.IO.File.ReadAllText(path);
        text = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"(agentTypeID: 0\s+agentRadius: )\S+",
            "${1}" + radius.ToString("F2"));
        System.IO.File.WriteAllText(path, text);
    }
}

public class NavAgentRadiusWindow : EditorWindow
{
    private float radius;

    public static void Open()
    {
        var win = GetWindow<NavAgentRadiusWindow>("NavMesh Agent 半径");
        var settings = NavMesh.GetSettingsByID(0);
        win.radius = settings.agentRadius;
        win.minSize = new Vector2(300, 100);
        win.maxSize = new Vector2(400, 120);
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("越小 → 窄通道覆盖越多；越大 → 离墙越远\n建议 0.05 ~ 0.5", EditorStyles.wordWrappedLabel);
        GUILayout.Space(5);
        radius = EditorGUILayout.Slider("Agent 半径", radius, 0.01f, 0.5f);
        GUILayout.Space(5);

        if (GUILayout.Button("保存并重新烘焙"))
        {
            EditorTools.SetHumanoidAgentRadius(radius);
            AssetDatabase.Refresh();
            Close();
            EditorTools.CreateAndBakeNavMesh2D();
        }
    }
}

public class PixelsPerUnitWindow : EditorWindow
{
    private float ppu = 200f;

    public static void Open()
    {
        var win = GetWindow<PixelsPerUnitWindow>("设置 Pixels Per Unit");
        win.minSize = new Vector2(320, 120);
        win.maxSize = new Vector2(420, 140);
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("修改 Art/animation 下所有图片的 Pixels Per Unit\n数值越大，Sprite 在场景中越小", EditorStyles.wordWrappedLabel);
        GUILayout.Space(5);
        ppu = EditorGUILayout.FloatField("Pixels Per Unit", ppu);
        GUILayout.Space(5);

        if (GUILayout.Button("应用到所有动画图片"))
        {
            string[] folders = new[]
            {
                "Assets/Art/animation/角色1",
                "Assets/Art/animation/角色2",
                "Assets/Art/animation/猫",
                "Assets/Art/animation/狗",
                "Assets/Art/animation/鸟"
            };

            int count = 0;
            foreach (var folder in folders)
            {
                var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) continue;

                    importer.spritePixelsPerUnit = ppu;
                    importer.SaveAndReimport();
                    count++;
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"已将 {count} 张动画图片的 Pixels Per Unit 设为 {ppu}");
            Close();
        }
    }
}

public class MonsterComboWindow : EditorWindow
{
    private Vector2 scrollPos;
    private List<BulletType> templateCombo = new List<BulletType> { BulletType.Dot, BulletType.Dot, BulletType.Line };
    private float templateDetectRange = 5f;

    private struct MonsterEntry
    {
        public Monster monster;
        public List<BulletType> combo;
        public float detectRange;
        public bool foldout;
    }

    private List<MonsterEntry> entries = new List<MonsterEntry>();

    public static void Open()
    {
        var win = GetWindow<MonsterComboWindow>("批量设置 Monster Combo");
        win.minSize = new Vector2(420, 350);
        win.RefreshMonsters();
    }

    private void OnEnable()
    {
        RefreshMonsters();
    }

    private void RefreshMonsters()
    {
        entries.Clear();
        var monsters = Object.FindObjectsOfType<Monster>(true);
        foreach (var m in monsters)
        {
            var so = new SerializedObject(m);
            var prop = so.FindProperty("requiredCombo");
            var combo = new List<BulletType>();
            for (int i = 0; i < prop.arraySize; i++)
                combo.Add((BulletType)prop.GetArrayElementAtIndex(i).enumValueIndex);
            float dr = so.FindProperty("detectRange").floatValue;
            entries.Add(new MonsterEntry { monster = m, combo = combo, detectRange = dr, foldout = true });
        }
    }

    private void OnGUI()
    {
        GUILayout.Space(6);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label($"场景中共 {entries.Count} 个 Monster", EditorStyles.boldLabel);
        if (GUILayout.Button("刷新列表", GUILayout.Width(80)))
            RefreshMonsters();
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(6);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        EditorGUILayout.LabelField("批量模板", EditorStyles.boldLabel);
        DrawComboList(templateCombo, "模板");
        templateDetectRange = EditorGUILayout.FloatField("Detect Range", templateDetectRange);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("应用模板到全部 Monster"))
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                e.combo = new List<BulletType>(templateCombo);
                e.detectRange = templateDetectRange;
                entries[i] = e;
            }
            ApplyAll();
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(4);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.LabelField("逐个设置", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        for (int idx = 0; idx < entries.Count; idx++)
        {
            var e = entries[idx];
            if (e.monster == null) continue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            e.foldout = EditorGUILayout.Foldout(e.foldout, e.monster.gameObject.name, true, EditorStyles.foldoutHeader);
            if (GUILayout.Button("选中", GUILayout.Width(42)))
                Selection.activeGameObject = e.monster.gameObject;
            if (GUILayout.Button("用模板", GUILayout.Width(52)))
            {
                e.combo = new List<BulletType>(templateCombo);
                e.detectRange = templateDetectRange;
                ApplyEntry(e);
            }
            EditorGUILayout.EndHorizontal();

            if (e.foldout)
            {
                EditorGUI.indentLevel++;
                bool changed = DrawComboList(e.combo, e.monster.gameObject.name);

                float newRange = EditorGUILayout.FloatField("Detect Range", e.detectRange);
                if (newRange != e.detectRange)
                {
                    e.detectRange = newRange;
                    changed = true;
                }

                if (changed) ApplyEntry(e);
                EditorGUI.indentLevel--;
            }

            entries[idx] = e;
            EditorGUILayout.EndVertical();
            GUILayout.Space(2);
        }

        EditorGUILayout.EndScrollView();
    }

    private bool DrawComboList(List<BulletType> combo, string label)
    {
        bool changed = false;

        for (int i = 0; i < combo.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15);
            GUILayout.Label($"[{i}]", GUILayout.Width(28));

            var newVal = (BulletType)EditorGUILayout.EnumPopup(combo[i], GUILayout.Width(80));
            if (newVal != combo[i]) { combo[i] = newVal; changed = true; }

            if (GUILayout.Button("×", GUILayout.Width(22)))
            {
                combo.RemoveAt(i);
                changed = true;
                GUILayout.EndHorizontal();
                break;
            }

            if (i > 0 && GUILayout.Button("↑", GUILayout.Width(22)))
            {
                (combo[i], combo[i - 1]) = (combo[i - 1], combo[i]);
                changed = true;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(EditorGUI.indentLevel * 15 + 30);
        if (GUILayout.Button("+ Dot", GUILayout.Width(60)))
        {
            combo.Add(BulletType.Dot);
            changed = true;
        }
        if (GUILayout.Button("+ Line", GUILayout.Width(60)))
        {
            combo.Add(BulletType.Line);
            changed = true;
        }
        EditorGUILayout.EndHorizontal();

        return changed;
    }

    private void ApplyEntry(MonsterEntry entry)
    {
        if (entry.monster == null) return;
        Undo.RecordObject(entry.monster, "Set Monster Properties");
        var so = new SerializedObject(entry.monster);

        var comboProp = so.FindProperty("requiredCombo");
        comboProp.arraySize = entry.combo.Count;
        for (int i = 0; i < entry.combo.Count; i++)
            comboProp.GetArrayElementAtIndex(i).enumValueIndex = (int)entry.combo[i];

        so.FindProperty("detectRange").floatValue = entry.detectRange;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(entry.monster);
        EditorSceneManager.MarkSceneDirty(entry.monster.gameObject.scene);
    }

    private void ApplyAll()
    {
        foreach (var e in entries)
            ApplyEntry(e);
        Debug.Log($"已将模板应用到 {entries.Count} 个 Monster（Combo + Detect Range）");
    }
}

public static partial class EditorToolsExtra
{
    [MenuItem("Tools/创建 CursorSettings 配置")]
    public static void CreateCursorSettings()
    {
        const string path = "Assets/Resources/CursorSettings.asset";
        var existing = AssetDatabase.LoadAssetAtPath<CursorSettings>(path);
        if (existing != null)
        {
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = existing;
            Debug.Log("[EditorTools] CursorSettings 已存在，已选中");
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        var asset = ScriptableObject.CreateInstance<CursorSettings>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
        Debug.Log("[EditorTools] 已创建 CursorSettings: " + path);
    }
}
