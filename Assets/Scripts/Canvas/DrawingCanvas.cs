using UnityEngine;
using UnityEngine.UI;

public class DrawingCanvas : MonoBehaviour
{
    [SerializeField] private IconToolbar toolbar;
    [SerializeField] private RectTransform canvasArea;

    private const string BG_RESOURCE_PATH = "Art/ui/note_bg";

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

        if (config != null && toolbar != null && config.ShowIcons)
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

        var sprite = Resources.Load<Sprite>(BG_RESOURCE_PATH);
        if (sprite == null)
        {
            var tex = Resources.Load<Texture2D>(BG_RESOURCE_PATH);
            if (tex != null)
                sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f));
        }

        if (sprite == null)
        {
            Debug.LogWarning($"[DrawingCanvas] 未找到背景图资源: {BG_RESOURCE_PATH}");
            return;
        }

        var bgGo = new GameObject("CanvasBackground");
        bgGo.transform.SetParent(canvasArea, false);
        bgGo.transform.SetAsFirstSibling();

        var rt = bgGo.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = bgGo.AddComponent<Image>();
        img.sprite = sprite;
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
