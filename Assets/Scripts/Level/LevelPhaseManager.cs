using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public enum LevelPhase { Light, Dark }

public class LevelPhaseManager : MonoBehaviour
{
    public static LevelPhaseManager Instance { get; private set; }

    [SerializeField] private TopDownCamera topDownCamera;

    public LevelPhase CurrentPhase { get; private set; } = LevelPhase.Light;
    public event Action<LevelPhase> OnPhaseChanged;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SetPhase(LevelPhase.Light);
    }

    private bool debugLightsOn;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1) && CurrentPhase == LevelPhase.Dark)
        {
            debugLightsOn = !debugLightsOn;

            if (topDownCamera != null)
                topDownCamera.SetDarkMode(!debugLightsOn);

            var hideables = FindObjectsOfType<DarkPhaseHideable>();
            foreach (var h in hideables)
                h.SetVisible(debugLightsOn);

            Debug.Log($"[Debug] 作弊灯光：{(debugLightsOn ? "开" : "关")}（阶段仍为 Dark）");
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            RestartCurrentPhase();
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            RestartLevel();
        }
    }

    public void RestartCurrentPhase()
    {
        debugLightsOn = false;
        ResetAllObjects();

        var player = FindObjectOfType<PlayerController>();
        var spawn = FindObjectOfType<SpawnPoint>();
        if (player != null && spawn != null)
            player.TeleportTo(spawn.transform.position);
        if (player != null)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.zero;
        }

        SetPhase(CurrentPhase);
        Debug.Log($"[Debug] 重启当前阶段：{CurrentPhase}");
    }

    public void RestartLevel()
    {
        Debug.Log("[Debug] 重启整个关卡");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void SetPhase(LevelPhase phase)
    {
        CurrentPhase = phase;

        if (topDownCamera != null)
            topDownCamera.SetDarkMode(phase == LevelPhase.Dark);

        var hideables = FindObjectsOfType<DarkPhaseHideable>();
        foreach (var h in hideables)
            h.SetVisible(phase == LevelPhase.Light);

        OnPhaseChanged?.Invoke(phase);
    }

    public void TransitionToDark()
    {
        ResetAllObjects();
        SetPhase(LevelPhase.Dark);
        var player = FindObjectOfType<PlayerController>();
        var spawn = FindObjectOfType<SpawnPoint>();
        if (player != null && spawn != null)
            player.TeleportTo(spawn.transform.position);
    }

    public void ResetAllObjects()
    {
        var resettables = FindObjectsOfType<MonoBehaviour>();
        int count = 0;
        foreach (var mb in resettables)
        {
            var r = mb as IResettable;
            if (r != null)
            {
                r.ResetState();
                count++;
            }
        }
        Debug.Log($"[Phase] 已重置 {count} 个交互物体");
    }

    public void OnLevelComplete()
    {
        var levelUI = FindObjectOfType<LevelUI>();
        if (levelUI != null)
            levelUI.ShowLevelComplete();
        else
            Debug.Log("关卡通关！");
    }
}
