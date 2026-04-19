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
            "Assets/Art/animation/狗"
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
                var framesProp = so.FindProperty("animFrames");
                var fpsProp = so.FindProperty("animFps");

                var catFrames = LoadSortedSprites("Assets/Art/animation/猫");

                if (catFrames.Length > 0)
                {
                    framesProp.arraySize = catFrames.Length;
                    for (int i = 0; i < catFrames.Length; i++)
                        framesProp.GetArrayElementAtIndex(i).objectReferenceValue = catFrames[i];
                }

                fpsProp.floatValue = 3f;
                so.ApplyModifiedProperties();

                var sr = monsterPrefab.GetComponent<SpriteRenderer>();
                if (sr != null && catFrames.Length > 0)
                {
                    sr.sprite = catFrames[0];
                    sr.color = Color.white;
                    EditorUtility.SetDirty(sr);
                }

                EditorUtility.SetDirty(monsterPrefab);
                changes++;
                Debug.Log($"[Monster] 默认使用\"猫\"帧动画 frames={catFrames.Length}, fps=3");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"帧动画配置完成，修改了 {changes} 个 Prefab。\n" +
                  "如需给特定 Monster 换\"狗\"图组，在场景中选中该 Monster，把 Art/animation/狗 的图拖到 Anim Frames。");
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
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LevelGlobalsPrefabPath);
        if (prefab == null)
        {
            Debug.LogError("找不到 LevelGlobals Prefab，请先运行 Tools → 创建 LevelGlobals Prefab");
            return;
        }

        var currentScene = EditorSceneManager.GetActiveScene().path;
        var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
        int updated = 0;

        foreach (var guid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            if (sceneName == "MainMenu" || sceneName == "SampleScene")
                continue;

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();

            GameObject oldGlobals = null;
            foreach (var root in roots)
            {
                if (root.GetComponent<LevelPhaseManager>() != null)
                {
                    oldGlobals = root;
                    break;
                }
            }

            if (oldGlobals != null)
            {
                var oldSo = new SerializedObject(oldGlobals.GetComponent<LevelPhaseManager>());
                bool isPrefabInstance = PrefabUtility.IsPartOfPrefabInstance(oldGlobals);

                if (isPrefabInstance)
                {
                    var src = PrefabUtility.GetCorrespondingObjectFromSource(oldGlobals);
                    if (src != null && AssetDatabase.GetAssetPath(src) == LevelGlobalsPrefabPath)
                    {
                        PrefabUtility.RevertPrefabInstance(oldGlobals, InteractionMode.AutomatedAction);
                        Debug.Log($"  [{sceneName}] 已还原 prefab 覆盖");
                        EditorSceneManager.MarkSceneDirty(scene);
                        EditorSceneManager.SaveScene(scene);
                        updated++;
                        continue;
                    }
                }

                Object.DestroyImmediate(oldGlobals);
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = "LevelGlobals";

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            updated++;
            Debug.Log($"  [{sceneName}] 已同步 LevelGlobals");
        }

        if (!string.IsNullOrEmpty(currentScene))
            EditorSceneManager.OpenScene(currentScene, OpenSceneMode.Single);

        Debug.Log($"已同步 LevelGlobals 到 {updated} 个关卡场景。修改 Prefab 后再次运行即可全部更新。");
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
                "Assets/Art/animation/狗"
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
