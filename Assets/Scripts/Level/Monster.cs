using UnityEngine;

public class Monster : MonoBehaviour
{
    // Placeholder: AI logic to be added later.
    // No DarkPhaseHideable — monster outlines stay visible in dark phase.

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
