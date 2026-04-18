using UnityEngine;

public class DrawingCanvas : MonoBehaviour
{
    [SerializeField] private IconToolbar toolbar;
    [SerializeField] private RectTransform canvasArea;

    public RectTransform CanvasArea => canvasArea;

    private void Start()
    {
        var config = FindObjectOfType<LevelConfig>();
        if (config != null && toolbar != null)
            toolbar.Initialize(config.AvailableIcons, canvasArea);
    }

    public void ClearAllMarkers()
    {
        if (canvasArea == null) return;
        foreach (Transform child in canvasArea)
        {
            var icon = child.GetComponent<DraggableIcon>();
            if (icon != null && !icon.IsToolbarSource)
                Destroy(child.gameObject);
        }
    }
}
