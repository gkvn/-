using UnityEngine;

public class PressureButton : MonoBehaviour, IInteractable
{
    [SerializeField] private Door linkedDoor;

    private SpriteRenderer spriteRenderer;
    private bool isPressed;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Interact(PlayerController player)
    {
        isPressed = !isPressed;

        if (linkedDoor != null)
        {
            if (isPressed) linkedDoor.Open();
            else linkedDoor.Close();
        }

        if (spriteRenderer != null)
            spriteRenderer.color = isPressed ? Color.green : Color.red;
    }
}
