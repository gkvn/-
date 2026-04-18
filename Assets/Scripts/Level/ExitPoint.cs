using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ExitPoint : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var pm = LevelPhaseManager.Instance;
        if (pm == null) return;

        if (pm.CurrentPhase == LevelPhase.Light)
            pm.TransitionToDark();
        else
            pm.OnLevelComplete();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
