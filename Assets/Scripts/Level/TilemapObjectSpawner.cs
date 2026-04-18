using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class TilemapObjectSpawner : MonoBehaviour
{
    [SerializeField] private Tilemap objectsTilemap;

    [Header("Tile → Prefab 映射")]
    [SerializeField] private TileBase spawnTile;
    [SerializeField] private GameObject spawnPrefab;
    [SerializeField] private TileBase exitTile;
    [SerializeField] private GameObject exitPrefab;
    [SerializeField] private TileBase spikeTile;
    [SerializeField] private GameObject spikePrefab;
    [SerializeField] private TileBase buttonTile;
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private TileBase doorTile;
    [SerializeField] private GameObject doorPrefab;
    [SerializeField] private TileBase monsterTile;
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private TileBase playerTile;
    [SerializeField] private GameObject playerPrefab;

    private void Awake()
    {
        if (objectsTilemap == null)
        {
            Debug.LogError("[Spawner] objectsTilemap 未赋值！请在 Inspector 中拖入 Objects Tilemap。");
            return;
        }

        DiagnoseSetup();
        SpawnAll();
        objectsTilemap.gameObject.SetActive(false);
    }

    void DiagnoseSetup()
    {
        int nullTiles = 0, nullPrefabs = 0;
        if (spawnTile == null)   { Debug.LogWarning("[Spawner] spawnTile 未赋值"); nullTiles++; }
        if (exitTile == null)    { Debug.LogWarning("[Spawner] exitTile 未赋值"); nullTiles++; }
        if (spikeTile == null)   { Debug.LogWarning("[Spawner] spikeTile 未赋值"); nullTiles++; }
        if (buttonTile == null)  { Debug.LogWarning("[Spawner] buttonTile 未赋值"); nullTiles++; }
        if (doorTile == null)    { Debug.LogWarning("[Spawner] doorTile 未赋值"); nullTiles++; }
        if (monsterTile == null) { Debug.LogWarning("[Spawner] monsterTile 未赋值"); nullTiles++; }
        if (playerTile == null)  { Debug.LogWarning("[Spawner] playerTile 未赋值"); nullTiles++; }

        if (spawnPrefab == null)   { Debug.LogWarning("[Spawner] spawnPrefab 未赋值"); nullPrefabs++; }
        if (exitPrefab == null)    { Debug.LogWarning("[Spawner] exitPrefab 未赋值"); nullPrefabs++; }
        if (spikePrefab == null)   { Debug.LogWarning("[Spawner] spikePrefab 未赋值"); nullPrefabs++; }
        if (buttonPrefab == null)  { Debug.LogWarning("[Spawner] buttonPrefab 未赋值"); nullPrefabs++; }
        if (doorPrefab == null)    { Debug.LogWarning("[Spawner] doorPrefab 未赋值"); nullPrefabs++; }
        if (monsterPrefab == null) { Debug.LogWarning("[Spawner] monsterPrefab 未赋值"); nullPrefabs++; }
        if (playerPrefab == null)  { Debug.LogWarning("[Spawner] playerPrefab 未赋值"); nullPrefabs++; }

        if (nullTiles > 0 || nullPrefabs > 0)
            Debug.LogError($"[Spawner] 有 {nullTiles} 个 Tile 和 {nullPrefabs} 个 Prefab 未赋值！请在 Inspector 中检查 TilemapObjectSpawner。");

        var bounds = objectsTilemap.cellBounds;
        int tileCount = 0;
        int matchedCount = 0;
        foreach (var pos in bounds.allPositionsWithin)
        {
            var tile = objectsTilemap.GetTile(pos);
            if (tile == null) continue;
            tileCount++;

            bool matched = tile == spawnTile || tile == exitTile || tile == spikeTile ||
                           tile == buttonTile || tile == doorTile || tile == monsterTile ||
                           tile == playerTile;

            if (matched)
                matchedCount++;
            else
                Debug.LogWarning($"[Spawner] 位置 {pos} 的 Tile \"{tile.name}\" 不在映射表中，将被忽略");
        }

        Debug.Log($"[Spawner] Objects 层共 {tileCount} 个 Tile，其中 {matchedCount} 个能匹配到 Prefab");
    }

    void SpawnAll()
    {
        var bounds = objectsTilemap.cellBounds;
        var buttons = new List<PressureButton>();
        var doors = new List<Door>();
        int spawned = 0;

        foreach (var cellPos in bounds.allPositionsWithin)
        {
            var tile = objectsTilemap.GetTile(cellPos);
            if (tile == null) continue;

            Vector3 worldPos = objectsTilemap.CellToWorld(cellPos)
                               + new Vector3(0.5f, 0.5f, 0);

            GameObject go = Spawn(tile, worldPos);
            if (go == null) continue;

            spawned++;
            Debug.Log($"[Spawner] 生成 {go.name} 在 {worldPos}");

            var btn = go.GetComponent<PressureButton>();
            if (btn != null) buttons.Add(btn);

            var door = go.GetComponent<Door>();
            if (door != null) doors.Add(door);
        }

        LinkButtonsToDoors(buttons, doors);
        Debug.Log($"[Spawner] 完成！共生成 {spawned} 个物体");
    }

    GameObject Spawn(TileBase tile, Vector3 pos)
    {
        GameObject prefab = null;

        if      (tile == spawnTile)   prefab = spawnPrefab;
        else if (tile == exitTile)    prefab = exitPrefab;
        else if (tile == spikeTile)   prefab = spikePrefab;
        else if (tile == buttonTile)  prefab = buttonPrefab;
        else if (tile == doorTile)    prefab = doorPrefab;
        else if (tile == monsterTile) prefab = monsterPrefab;
        else if (tile == playerTile)  prefab = playerPrefab;

        if (prefab == null) return null;
        return Instantiate(prefab, pos, Quaternion.identity);
    }

    void LinkButtonsToDoors(List<PressureButton> buttons, List<Door> doors)
    {
        if (buttons.Count == 0 || doors.Count == 0) return;

        var usedDoors = new HashSet<int>();

        foreach (var btn in buttons)
        {
            float minDist = float.MaxValue;
            int bestIdx = -1;

            for (int i = 0; i < doors.Count; i++)
            {
                if (usedDoors.Contains(i)) continue;
                float dist = Vector2.Distance(
                    btn.transform.position, doors[i].transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    bestIdx = i;
                }
            }

            if (bestIdx >= 0)
            {
                var field = typeof(PressureButton).GetField("linkedDoor",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
                if (field != null)
                    field.SetValue(btn, doors[bestIdx]);

                usedDoors.Add(bestIdx);
                Debug.Log($"自动关联：{btn.name} → {doors[bestIdx].name}");
            }
        }
    }
}
