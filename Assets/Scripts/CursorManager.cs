using UnityEngine;
using UnityEngine.UI;

public class CursorManager : MonoBehaviour
{
    [Tooltip("鼠标样式配置（留空则自动从 Resources/CursorSettings 加载）")]
    [SerializeField] private CursorSettings settings;

    [Header("直接配置（仅当 CursorSettings 未配置时使用）")]
    [SerializeField] private Sprite gameCursorSprite;
    [SerializeField] private float gameCursorSize = 32f;
    [SerializeField] private Sprite canvasCursorSprite;
    [SerializeField] private float canvasCursorSize = 32f;

    private Canvas cursorCanvas;
    private Image cursorImage;
    private RectTransform cursorRect;
    private bool isOnGameSide;
    private bool hasCustomCursor;

    private Sprite GameSprite => settings != null && settings.gameCursorSprite != null
        ? settings.gameCursorSprite : gameCursorSprite;
    private Sprite CanvasSprite => settings != null && settings.canvasCursorSprite != null
        ? settings.canvasCursorSprite : canvasCursorSprite;
    private float GameSize => settings != null && settings.gameCursorSprite != null
        ? settings.gameCursorSize : gameCursorSize;
    private float CanvasSize => settings != null && settings.canvasCursorSprite != null
        ? settings.canvasCursorSize : canvasCursorSize;

    private void Start()
    {
        if (settings == null)
            settings = Resources.Load<CursorSettings>("CursorSettings");

        hasCustomCursor = GameSprite != null || CanvasSprite != null;
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
        Sprite spr = gameSide ? GameSprite : CanvasSprite;
        float size = gameSide ? GameSize : CanvasSize;

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
