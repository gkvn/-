using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Spike : MonoBehaviour
{
    private void Start()
    {
        var col = GetComponent<Collider2D>();
        Debug.Log($"[Spike] Start — isTrigger={col.isTrigger}, bounds={col.bounds}, scale={transform.lossyScale}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[Spike] OnTriggerEnter2D — other={other.name}, tag={other.tag}");
        if (!other.CompareTag("Player")) return;
        var player = other.GetComponent<PlayerController>();
        if (player != null)
            player.Die();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"[Spike] OnCollisionEnter2D — other={collision.collider.name}, tag={collision.collider.tag}");
    }
}
