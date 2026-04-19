using UnityEngine;
using UnityEngine.UI;

public class CursorManager : MonoBehaviour
{
    [Header("右侧（探索场景）")]
    [Tooltip("探索场景的鼠标贴图")]
    [SerializeField] private Sprite gameCursorSprite;
    [Tooltip("探索场景鼠标大小(像素)")]
    [SerializeField] private float gameCursorSize = 32f;

    [Header("左侧（画布）")]
    [Tooltip("画布的鼠标贴图")]
    [SerializeField] private Sprite canvasCursorSprite;
    [Tooltip("画布鼠标大小(像素)")]
    [SerializeField] private float canvasCursorSize = 32f;

    private Canvas cursorCanvas;
    private Image cursorImage;
    private RectTransform cursorRect;
    private bool isOnGameSide;
    private bool hasCustomCursor;

    private void Start()
    {
        hasCustomCursor = gameCursorSprite != null || canvasCursorSprite != null;
        if (!hasCustomCursor) return;

        var go = new GameObject("CursorCanvas");
        go.transform.SetParent(transform);
        cursorCanvas = go.AddComponent<Canvas>();
        cursorCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        cursorCanvas.sortingOrder = 9999;

        var imgGo = new GameObject("CursorImage");
        imgGo.transform.SetParent(go.transform);
        cursorRect = imgGo.AddComponent<RectTransform>();
        cursorImage = imgGo.AddComponent<Image>();
        cursorImage.raycastTarget = false;

        Cursor.visible = false;
        ApplyCursor(false);
    }

    private void Update()
    {
        if (!hasCustomCursor) return;

        Cursor.visible = false;

        bool onRight = Input.mousePosition.x > Screen.width * 0.5f;
        if (onRight != isOnGameSide)
        {
            isOnGameSide = onRight;
            ApplyCursor(isOnGameSide);
        }

        cursorRect.position = Input.mousePosition;
    }

    private void ApplyCursor(bool gameSide)
    {
        Sprite spr = gameSide ? gameCursorSprite : canvasCursorSprite;
        float size = gameSide ? gameCursorSize : canvasCursorSize;

        if (spr != null)
        {
            cursorImage.sprite = spr;
            cursorImage.enabled = true;
        }
        else
        {
            cursorImage.enabled = false;
        }

        cursorRect.sizeDelta = new Vector2(size, size);
    }

    private void OnDisable()
    {
        Cursor.visible = true;
    }

    private void OnDestroy()
    {
        Cursor.visible = true;
    }
}
