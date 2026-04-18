using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(NavMeshObstacle))]
public class Door : MonoBehaviour, IResettable
{
    private Collider2D col;
    private NavMeshObstacle obstacle;
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

        obstacle = GetComponent<NavMeshObstacle>();
        obstacle.carving = true;
        obstacle.enabled = true;
    }

    public void Open()
    {
        isOpen = true;
        col.enabled = false;
        obstacle.enabled = false;
        if (spriteRenderer != null)
            spriteRenderer.color = new Color(closedColor.r, closedColor.g, closedColor.b, 0.25f);
    }

    public void Close()
    {
        isOpen = false;
        col.enabled = true;
        obstacle.enabled = true;
        if (spriteRenderer != null)
            spriteRenderer.color = closedColor;
    }

    public void ResetState()
    {
        Close();
    }
}
