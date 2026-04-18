using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Door : MonoBehaviour
{
    private Collider2D col;
    private SpriteRenderer spriteRenderer;
    private Color closedColor;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            closedColor = spriteRenderer.color;
    }

    public void Open()
    {
        isOpen = true;
        col.enabled = false;
        if (spriteRenderer != null)
            spriteRenderer.color = new Color(closedColor.r, closedColor.g, closedColor.b, 0.25f);
    }

    public void Close()
    {
        isOpen = false;
        col.enabled = true;
        if (spriteRenderer != null)
            spriteRenderer.color = closedColor;
    }
}
