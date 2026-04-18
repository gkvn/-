using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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

    [MenuItem("Tools/创建并烘焙 NavMesh 2D")]
    public static void CreateAndBakeNavMesh2D()
    {
        PrepareNavMeshScene();

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
