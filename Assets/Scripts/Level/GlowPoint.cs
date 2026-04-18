using UnityEngine;

public class GlowPoint : MonoBehaviour
{
    [SerializeField] private Color glowColor = new Color(1f, 0.95f, 0.6f);
    [SerializeField] private float glowRadius = 1.5f;
    [SerializeField][Range(0.05f, 1f)] private float coreSize = 0.3f;
    [SerializeField][Range(0f, 0.5f)] private float pulseAmount = 0.1f;
    [SerializeField] private float pulseSpeed = 2f;

    private SpriteRenderer coreSR;
    private SpriteRenderer auraRS;
    private float baseAuraAlpha;

    private void Start()
    {
        var sprite = RuntimeSprite.Get();

        coreSR = GetComponent<SpriteRenderer>();
        if (coreSR == null)
            coreSR = gameObject.AddComponent<SpriteRenderer>();
        coreSR.sprite = sprite;
        coreSR.color = glowColor;
        coreSR.sortingOrder = 8;
        transform.localScale = Vector3.one * coreSize;

        var auraGO = new GameObject("Aura");
        auraGO.transform.SetParent(transform);
        auraGO.transform.localPosition = Vector3.zero;
        float auraScale = glowRadius / coreSize;
        auraGO.transform.localScale = Vector3.one * auraScale;

        auraRS = auraGO.AddComponent<SpriteRenderer>();
        auraRS.sprite = sprite;
        baseAuraAlpha = 0.18f;
        auraRS.color = new Color(glowColor.r, glowColor.g, glowColor.b, baseAuraAlpha);
        auraRS.sortingOrder = 7;
    }

    private void Update()
    {
        if (pulseAmount <= 0f || auraRS == null) return;

        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        float auraScale = (glowRadius / coreSize) * pulse;
        auraRS.transform.localScale = Vector3.one * auraScale;

        float alpha = baseAuraAlpha * (1f + Mathf.Sin(Time.time * pulseSpeed) * 0.3f);
        var c = auraRS.color;
        auraRS.color = new Color(c.r, c.g, c.b, alpha);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.95f, 0.6f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, glowRadius);
    }
}
