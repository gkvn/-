using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapDarkPhase : MonoBehaviour
{
    private TilemapRenderer tr;

    private void Awake()
    {
        tr = GetComponent<TilemapRenderer>();
    }

    private void Start()
    {
        if (LevelPhaseManager.Instance != null)
            LevelPhaseManager.Instance.OnPhaseChanged += OnPhaseChanged;
    }

    private void OnDestroy()
    {
        if (LevelPhaseManager.Instance != null)
            LevelPhaseManager.Instance.OnPhaseChanged -= OnPhaseChanged;
    }

    private void OnPhaseChanged(LevelPhase phase)
    {
        if (tr != null)
            tr.enabled = phase == LevelPhase.Light;
    }
}
