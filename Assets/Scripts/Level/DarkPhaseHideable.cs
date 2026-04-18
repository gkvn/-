using UnityEngine;

public class DarkPhaseHideable : MonoBehaviour
{
    [Tooltip("隐藏时是否保留碰撞体（墙壁/陷阱应保留）")]
    [SerializeField] private bool keepColliderWhenHidden = true;

    private SpriteRenderer spriteRenderer;
    private Collider2D col;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    public void SetVisible(bool visible)
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = visible;

        if (!keepColliderWhenHidden && col != null)
            col.enabled = visible;
    }
}
