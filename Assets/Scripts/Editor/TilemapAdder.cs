using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TilemapAdder
{
    [MenuItem("Tools/添加 Tilemap 到当前场景（不碰 Prefab）")]
    public static void AddTilemapToCurrentScene()
    {
        if (!EditorUtility.DisplayDialog(
            "添加 Tilemap",
            "将在当前场景中：\n• 移除所有 Wall_ 开头的旧墙壁\n• 添加 Grid > Ground / Walls / Objects Tilemap\n• 创建 Objects 标记 Tile 资源\n\n不会修改任何 Prefab。继续？",
            "确定", "取消"))
            return;

        var groundTile = AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.Tile>("Assets/Tiles/GroundTile.asset");
        var wallTile = AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.Tile>("Assets/Tiles/WallTile.asset");

        if (groundTile == null || wallTile == null)
        {
            EditorUtility.DisplayDialog("缺少 Tile 资源",
                "找不到 Assets/Tiles/GroundTile.asset 或 WallTile.asset。\n请先确认 Tile 资源存在。", "确定");
            return;
        }

        EnsureObjectTiles();
        RemoveOldWalls();
        RemoveOldInteractiveObjects();

        if (Object.FindObjectOfType<Grid>() != null)
        {
            if (!EditorUtility.DisplayDialog("已有 Grid",
                "场景中已有 Grid 对象，是否删除后重新创建？", "是", "取消"))
                return;
            foreach (var g in Object.FindObjectsOfType<Grid>())
                Object.DestroyImmediate(g.gameObject);
        }

        BuildTilemap(groundTile, wallTile);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Tilemap 已添加到当前场景。按 Ctrl+S 保存。");
    }

    [MenuItem("Tools/只创建标记 Tile 资源（不改场景）")]
    public static void CreateObjectTilesOnly()
    {
        EnsureObjectTiles();
        Debug.Log("标记 Tile 资源已就绪，可在 Tile Palette 中使用。");
    }

    // ═══════════════════════════ Object marker tiles ═══════════════════════════

    struct MarkerDef
    {
        public string name;
        public Color color;
        public MarkerDef(string n, Color c) { name = n; color = c; }
    }

    static readonly MarkerDef[] ObjectMarkers = new MarkerDef[]
    {
        new MarkerDef("SpawnTile",   Color.green),
        new MarkerDef("ExitTile",    Color.yellow),
        new MarkerDef("SpikeTile",   Color.red),
        new MarkerDef("ButtonTile",  new Color(1f, 0.5f, 0f)),
        new MarkerDef("DoorTile",    new Color(0.6f, 0.4f, 0.2f)),
        new MarkerDef("MonsterTile", Color.magenta),
        new MarkerDef("PlayerTile",  Color.cyan),
    };

    static void EnsureObjectTiles()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Tiles"))
            AssetDatabase.CreateFolder("Assets", "Tiles");
        if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
            AssetDatabase.CreateFolder("Assets", "Sprites");

        foreach (var m in ObjectMarkers)
            CreateMarkerTile(m.name, m.color);

        AssetDatabase.SaveAssets();
        Debug.Log($"已创建 {ObjectMarkers.Length} 个标记 Tile 到 Assets/Tiles/");
    }

    static void CreateMarkerTile(string name, Color color)
    {
        string tilePath = $"Assets/Tiles/{name}.asset";
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.Tile>(tilePath) != null)
            return;

        string spritePath = $"Assets/Sprites/{name}.png";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null)
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = tex.GetPixels();
            for (int i = 0; i < px.Length; i++) px[i] = color;
            tex.SetPixels(px);
            tex.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.GetFullPath(spritePath), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceSynchronousImport);
            SetSpriteImport(spritePath, 4);
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        }

        var tile = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
        tile.sprite = sprite;
        tile.color = Color.white;
        tile.colliderType = UnityEngine.Tilemaps.Tile.ColliderType.None;
        AssetDatabase.CreateAsset(tile, tilePath);
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

    // ═══════════════════════════ Cleanup ═══════════════════════════

    static void RemoveOldWalls()
    {
        int removed = 0;
        foreach (var go in Object.FindObjectsOfType<GameObject>())
        {
            if (go.name.StartsWith("Wall_") ||
                (go.name == "Wall" && go.GetComponent<DarkPhaseHideable>() != null))
            {
                Object.DestroyImmediate(go);
                removed++;
            }
        }
        if (removed > 0)
            Debug.Log($"已移除 {removed} 个旧墙壁对象");
    }

    static void RemoveOldInteractiveObjects()
    {
        int removed = 0;
        string[] typeNames = { "SpawnPoint", "ExitPoint", "Spike", "PressureButton", "Door", "Monster" };

        foreach (var go in Object.FindObjectsOfType<GameObject>())
        {
            if (go == null) continue;
            foreach (var typeName in typeNames)
            {
                if (go.GetComponent(typeName) != null)
                {
                    string n = go.name;
                    Object.DestroyImmediate(go);
                    removed++;
                    Debug.Log($"已移除旧交互对象：{n}");
                    break;
                }
            }
        }

        var player = Object.FindObjectOfType<PlayerController>();
        if (player != null)
        {
            Object.DestroyImmediate(player.gameObject);
            removed++;
            Debug.Log("已移除旧 Player 对象");
        }

        if (removed > 0)
            Debug.Log($"共移除 {removed} 个旧交互对象（将由 Objects Tilemap 替代）");
    }

    // ═══════════════════════════ Build tilemap ═══════════════════════════

    static void BuildTilemap(UnityEngine.Tilemaps.Tile groundTile, UnityEngine.Tilemaps.Tile wallTile)
    {
        var gridGO = new GameObject("Grid");
        gridGO.AddComponent<Grid>();

        // Ground layer
        var groundGO = new GameObject("Ground");
        groundGO.transform.SetParent(gridGO.transform);
        groundGO.AddComponent<Tilemap>();
        groundGO.AddComponent<TilemapRenderer>();
        groundGO.AddComponent<TilemapDarkPhase>();

        // Wall layer
        var wallGO = new GameObject("Walls");
        wallGO.transform.SetParent(gridGO.transform);
        wallGO.AddComponent<Tilemap>();
        var wallRenderer = wallGO.AddComponent<TilemapRenderer>();
        wallRenderer.sortingOrder = 1;
        wallGO.AddComponent<TilemapCollider2D>();
        var wallRB = wallGO.AddComponent<Rigidbody2D>();
        wallRB.bodyType = RigidbodyType2D.Static;
        wallGO.AddComponent<CompositeCollider2D>();
        wallGO.GetComponent<TilemapCollider2D>().usedByComposite = true;
        wallGO.AddComponent<TilemapDarkPhase>();

        // Objects layer (marker tiles → spawned at runtime)
        var objectsGO = new GameObject("Objects");
        objectsGO.transform.SetParent(gridGO.transform);
        objectsGO.AddComponent<Tilemap>();
        var objRenderer = objectsGO.AddComponent<TilemapRenderer>();
        objRenderer.sortingOrder = 5;

        // Paint default test level
        var groundMap = groundGO.GetComponent<Tilemap>();
        var wallMap = wallGO.GetComponent<Tilemap>();
        var objectsMap = objectsGO.GetComponent<Tilemap>();

        const int minX = -1, maxX = 11, minY = -1, maxY = 11;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                var pos = new Vector3Int(x, y, 0);
                bool isBorder = x == minX || x == maxX || y == minY || y == maxY;
                bool isInnerWall = y == 4 && x >= 2 && x <= 7;

                if (isBorder || isInnerWall)
                    wallMap.SetTile(pos, wallTile);
                else
                    groundMap.SetTile(pos, groundTile);
            }
        }

        // Paint marker tiles for interactive objects
        var spawnT   = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/SpawnTile.asset");
        var exitT    = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/ExitTile.asset");
        var spikeT   = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/SpikeTile.asset");
        var buttonT  = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/ButtonTile.asset");
        var doorT    = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/DoorTile.asset");
        var monsterT = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/MonsterTile.asset");
        var playerT  = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/PlayerTile.asset");

        if (playerT != null)  objectsMap.SetTile(new Vector3Int(2, 2, 0), playerT);
        if (spawnT != null)   objectsMap.SetTile(new Vector3Int(2, 1, 0), spawnT);
        if (exitT != null)    objectsMap.SetTile(new Vector3Int(9, 9, 0), exitT);
        if (spikeT != null)
        {
            objectsMap.SetTile(new Vector3Int(4, 2, 0), spikeT);
            objectsMap.SetTile(new Vector3Int(5, 2, 0), spikeT);
            objectsMap.SetTile(new Vector3Int(6, 2, 0), spikeT);
        }
        if (buttonT != null)  objectsMap.SetTile(new Vector3Int(3, 6, 0), buttonT);
        if (doorT != null)    objectsMap.SetTile(new Vector3Int(7, 6, 0), doorT);
        if (monsterT != null) objectsMap.SetTile(new Vector3Int(8, 3, 0), monsterT);

        // Setup TilemapObjectSpawner
        SetupSpawner(objectsMap);

        Debug.Log("Tilemap 已生成：Ground + Walls + Objects（带交互物体标记）");
    }

    static void SetupSpawner(Tilemap objectsMap)
    {
        var spawnerGO = new GameObject("TilemapObjectSpawner");
        var spawner = spawnerGO.AddComponent<TilemapObjectSpawner>();

        var so = new SerializedObject(spawner);
        so.FindProperty("objectsTilemap").objectReferenceValue = objectsMap;

        so.FindProperty("spawnTile").objectReferenceValue   = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/SpawnTile.asset");
        so.FindProperty("exitTile").objectReferenceValue    = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/ExitTile.asset");
        so.FindProperty("spikeTile").objectReferenceValue   = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/SpikeTile.asset");
        so.FindProperty("buttonTile").objectReferenceValue  = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/ButtonTile.asset");
        so.FindProperty("doorTile").objectReferenceValue    = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/DoorTile.asset");
        so.FindProperty("monsterTile").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/MonsterTile.asset");
        so.FindProperty("playerTile").objectReferenceValue  = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/PlayerTile.asset");

        so.FindProperty("spawnPrefab").objectReferenceValue   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SpawnPoint.prefab");
        so.FindProperty("exitPrefab").objectReferenceValue    = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ExitPoint.prefab");
        so.FindProperty("spikePrefab").objectReferenceValue   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Spike.prefab");
        so.FindProperty("buttonPrefab").objectReferenceValue  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PressureButton.prefab");
        so.FindProperty("doorPrefab").objectReferenceValue    = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Door.prefab");
        so.FindProperty("monsterPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monster.prefab");
        so.FindProperty("playerPrefab").objectReferenceValue  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ═══════════════════════════ Fix existing tilemap ═══════════════════════════

    [MenuItem("Tools/修复 Tilemap 结构（保留已画内容）")]
    public static void FixExistingTilemap()
    {
        EnsureObjectTiles();

        var grid = Object.FindObjectOfType<Grid>();
        if (grid == null)
        {
            EditorUtility.DisplayDialog("找不到 Grid", "场景中没有 Grid 对象。", "确定");
            return;
        }

        var tilemaps = grid.GetComponentsInChildren<Tilemap>();
        if (tilemaps.Length == 0)
        {
            EditorUtility.DisplayDialog("找不到 Tilemap", "Grid 下没有 Tilemap。", "确定");
            return;
        }

        // Step 1: treat existing tilemap as Walls layer, add collision
        var wallsTM = tilemaps[0];
        var wallsGO = wallsTM.gameObject;
        if (wallsGO.name != "Walls")
        {
            Debug.Log($"将现有 Tilemap \"{wallsGO.name}\" 重命名为 \"Walls\"");
            wallsGO.name = "Walls";
        }

        if (wallsGO.GetComponent<TilemapCollider2D>() == null)
            wallsGO.AddComponent<TilemapCollider2D>();

        var rb = wallsGO.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = wallsGO.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
        }

        if (wallsGO.GetComponent<CompositeCollider2D>() == null)
            wallsGO.AddComponent<CompositeCollider2D>();

        wallsGO.GetComponent<TilemapCollider2D>().usedByComposite = true;

        if (wallsGO.GetComponent<TilemapDarkPhase>() == null)
            wallsGO.AddComponent<TilemapDarkPhase>();

        var wallRenderer = wallsGO.GetComponent<TilemapRenderer>();
        if (wallRenderer != null)
            wallRenderer.sortingOrder = 1;

        Debug.Log("✓ Walls 层已加碰撞组件");

        // Step 2: create Ground layer if missing
        bool hasGround = false;
        foreach (var tm in tilemaps)
            if (tm.gameObject.name == "Ground") hasGround = true;

        if (!hasGround)
        {
            var groundGO = new GameObject("Ground");
            groundGO.transform.SetParent(grid.transform);
            groundGO.transform.SetAsFirstSibling();
            groundGO.AddComponent<Tilemap>();
            groundGO.AddComponent<TilemapRenderer>();
            groundGO.AddComponent<TilemapDarkPhase>();
            Debug.Log("✓ 已创建 Ground 层（可选，用来铺地板装饰）");
        }

        // Step 3: create Objects layer if missing
        Tilemap objectsMap = null;
        foreach (var tm in tilemaps)
            if (tm.gameObject.name == "Objects") objectsMap = tm;

        if (objectsMap == null)
        {
            var objectsGO = new GameObject("Objects");
            objectsGO.transform.SetParent(grid.transform);
            objectsMap = objectsGO.AddComponent<Tilemap>();
            var objRenderer = objectsGO.AddComponent<TilemapRenderer>();
            objRenderer.sortingOrder = 5;
            Debug.Log("✓ 已创建 Objects 层（用来放交互物体标记 Tile）");
        }

        // Step 4: create TilemapObjectSpawner if missing
        var existingSpawner = Object.FindObjectOfType<TilemapObjectSpawner>();
        if (existingSpawner == null)
        {
            SetupSpawner(objectsMap);
            Debug.Log("✓ 已创建 TilemapObjectSpawner 并配好 Tile→Prefab 映射");
        }
        else
        {
            Debug.Log("✓ TilemapObjectSpawner 已存在");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("修复完成！现在：\n" +
            "• Walls 层的砖墙已有碰撞\n" +
            "• 在 Tile Palette 选 Objects 层，用标记 Tile（纯色方块）画交互物体\n" +
            "• 按 Play 时标记会自动变成 Prefab\n" +
            "按 Ctrl+S 保存场景。");
    }

    // ═══════════════════════════ One-click Objects setup ═══════════════════════════

    [MenuItem("Tools/一键设置 Objects 层 + Palette")]
    public static void OneClickObjectsSetup()
    {
        EnsureObjectTiles();

        var grid = Object.FindObjectOfType<Grid>();
        if (grid == null)
        {
            EditorUtility.DisplayDialog("找不到 Grid",
                "场景中没有 Grid。请先运行「修复 Tilemap 结构」。", "确定");
            return;
        }

        // Find or create Objects tilemap
        Tilemap objectsMap = null;
        foreach (var tm in grid.GetComponentsInChildren<Tilemap>())
            if (tm.gameObject.name == "Objects") objectsMap = tm;

        if (objectsMap == null)
        {
            var objectsGO = new GameObject("Objects");
            objectsGO.transform.SetParent(grid.transform);
            objectsMap = objectsGO.AddComponent<Tilemap>();
            var r = objectsGO.AddComponent<TilemapRenderer>();
            r.sortingOrder = 5;
        }

        // Paint default markers
        PaintDefaultMarkers(objectsMap);

        // Create spawner if missing
        if (Object.FindObjectOfType<TilemapObjectSpawner>() == null)
            SetupSpawner(objectsMap);

        // Create Tile Palette
        CreateTilePalette();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("全部完成！\n" +
            "• Objects 层已画好默认交互物体标记\n" +
            "• Tile Palette \"LevelPalette\" 已创建（所有 Tile 已在里面）\n" +
            "• TilemapObjectSpawner 已配好\n" +
            "• 按 Play 测试，标记会自动变成 Prefab\n" +
            "• 想改布局：打开 Tile Palette，选 Objects 层，用标记 Tile 画\n" +
            "按 Ctrl+S 保存场景。");
    }

    static void PaintDefaultMarkers(Tilemap objectsMap)
    {
        var playerT  = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/PlayerTile.asset");
        var spawnT   = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/SpawnTile.asset");
        var exitT    = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/ExitTile.asset");
        var spikeT   = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/SpikeTile.asset");
        var buttonT  = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/ButtonTile.asset");
        var doorT    = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/DoorTile.asset");
        var monsterT = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/MonsterTile.asset");

        objectsMap.ClearAllTiles();

        if (playerT != null)  objectsMap.SetTile(new Vector3Int(1, 1, 0), playerT);
        if (spawnT != null)   objectsMap.SetTile(new Vector3Int(1, 0, 0), spawnT);
        if (exitT != null)    objectsMap.SetTile(new Vector3Int(9, 9, 0), exitT);

        if (spikeT != null)
        {
            objectsMap.SetTile(new Vector3Int(4, 2, 0), spikeT);
            objectsMap.SetTile(new Vector3Int(5, 2, 0), spikeT);
            objectsMap.SetTile(new Vector3Int(6, 2, 0), spikeT);
        }

        if (buttonT != null)  objectsMap.SetTile(new Vector3Int(3, 6, 0), buttonT);
        if (doorT != null)    objectsMap.SetTile(new Vector3Int(7, 6, 0), doorT);
        if (monsterT != null) objectsMap.SetTile(new Vector3Int(8, 3, 0), monsterT);

        Debug.Log("已在 Objects 层画好默认交互物体标记");
    }

    static void CreateTilePalette()
    {
        string paletteFolder = "Assets/Tiles/LevelPalette";
        if (!AssetDatabase.IsValidFolder(paletteFolder))
            AssetDatabase.CreateFolder("Assets/Tiles", "LevelPalette");

        string prefabPath = $"{paletteFolder}/LevelPalette.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            AssetDatabase.DeleteAsset(prefabPath);

        var go = new GameObject("LevelPalette");
        go.AddComponent<Grid>();

        var layer = new GameObject("Layer1");
        layer.transform.SetParent(go.transform);
        var tm = layer.AddComponent<Tilemap>();
        layer.AddComponent<TilemapRenderer>();

        string[] tileNames = {
            "GroundTile", "WallTile", "PlayerTile", "SpawnTile",
            "ExitTile", "SpikeTile", "ButtonTile", "DoorTile", "MonsterTile"
        };

        for (int i = 0; i < tileNames.Length; i++)
        {
            var tile = AssetDatabase.LoadAssetAtPath<TileBase>($"Assets/Tiles/{tileNames[i]}.asset");
            if (tile != null)
                tm.SetTile(new Vector3Int(i, 0, 0), tile);
        }

        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);

        var palette = ScriptableObject.CreateInstance<GridPalette>();
        palette.name = "GridPalette";
        palette.cellSizing = GridPalette.CellSizing.Automatic;
        AssetDatabase.AddObjectToAsset(palette, prefabPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"Tile Palette 已创建: {prefabPath}（含 {tileNames.Length} 个 Tile）");
    }

    // ═══════════════════════════ UI visibility toggle ═══════════════════════════

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
}
