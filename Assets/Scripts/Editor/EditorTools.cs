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

        surface.BuildNavMesh();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("NavMesh 2D 烘焙完成！按 Ctrl+S 保存场景。");
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
        mod.area = 0; // Walkable

        Debug.Log($"NavFloor 地面：center={bounds.center}, size={box.size}");
    }

    static void MarkWallsNotWalkable()
    {
        int wallCount = 0;

        // Find the "Wall" root object in scene (covers edge_walls + other_walls)
        var wallRoot = GameObject.Find("Wall");
        if (wallRoot != null)
        {
            wallCount += MarkChildrenNotWalkable(wallRoot.transform);
        }

        // Also mark any other objects whose name contains "Wall" and has a non-trigger collider
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
                mod.area = 1; // Not Walkable
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
            mod.area = 1; // Not Walkable
            count++;
        }
        return count;
    }
}
