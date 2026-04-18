using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ExitPoint : MonoBehaviour
{
    private void Start()
    {
        var col = GetComponent<Collider2D>();
        Debug.Log($"[ExitPoint] Start — isTrigger={col.isTrigger}, bounds={col.bounds}, scale={transform.lossyScale}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[ExitPoint] OnTriggerEnter2D — other={other.name}, tag={other.tag}");
        if (!other.CompareTag("Player")) return;

        var pm = LevelPhaseManager.Instance;
        if (pm == null) { Debug.LogWarning("[ExitPoint] LevelPhaseManager.Instance is null"); return; }

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
