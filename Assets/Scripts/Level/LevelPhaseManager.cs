using UnityEngine;
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
        SetPhase(LevelPhase.Dark);
        var player = FindObjectOfType<PlayerController>();
        var spawn = FindObjectOfType<SpawnPoint>();
        if (player != null && spawn != null)
            player.TeleportTo(spawn.transform.position);
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
