using UnityEngine;
using UnityEngine.UI;

public class DrawingCanvas : MonoBehaviour
{
    [SerializeField] private IconToolbar toolbar;
    [SerializeField] private RectTransform canvasArea;

    [Header("画布背景")]
    [Tooltip("左侧画布背景图（留空则无背景）")]
    [SerializeField] private Sprite backgroundImage;
    [Tooltip("背景颜色/透明度（配合背景图使用时为叠加色）")]
    [SerializeField] private Color backgroundColor = Color.white;

    public RectTransform CanvasArea => canvasArea;

    private void Start()
    {
        var config = FindObjectOfType<LevelConfig>();

        if (config != null && !config.ShowDrawingCanvas)
        {
            gameObject.SetActive(false);
            ExpandGameView();
            return;
        }

        CreateBackground();
        CreateCanvasParticles();

        if (config != null && toolbar != null)
            toolbar.Initialize(config.AvailableIcons, canvasArea);
    }

    private void ExpandGameView()
    {
        var parent = transform.parent;
        if (parent == null) return;

        foreach (Transform sibling in parent)
        {
            if (sibling == transform) continue;
            var rt = sibling.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }
    }

    private void CreateBackground()
    {
        if (canvasArea == null) return;
        if (backgroundImage == null && backgroundColor.a <= 0f) return;

        var bgGo = new GameObject("CanvasBackground");
        bgGo.transform.SetParent(canvasArea, false);
        bgGo.transform.SetAsFirstSibling();

        var rt = bgGo.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = bgGo.AddComponent<Image>();
        img.sprite = backgroundImage;
        img.type = backgroundImage != null ? Image.Type.Sliced : Image.Type.Simple;
        img.color = backgroundColor;
        img.raycastTarget = false;
    }

    private void CreateCanvasParticles()
    {
        if (canvasArea == null) return;
        if (canvasArea.GetComponent<CanvasParticles>() == null)
            canvasArea.gameObject.AddComponent<CanvasParticles>();
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
