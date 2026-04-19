using UnityEngine;

[CreateAssetMenu(fileName = "CursorSettings", menuName = "Game/Cursor Settings")]
public class CursorSettings : ScriptableObject
{
    [Header("右侧（探索场景）")]
    [Tooltip("探索场景的鼠标贴图")]
    public Sprite gameCursorSprite;
    [Tooltip("探索场景鼠标大小(像素)")]
    public float gameCursorSize = 32f;

    [Header("左侧（画布）")]
    [Tooltip("画布的鼠标贴图")]
    public Sprite canvasCursorSprite;
    [Tooltip("画布鼠标大小(像素)")]
    public float canvasCursorSize = 32f;
}
