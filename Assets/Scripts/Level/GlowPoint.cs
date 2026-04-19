using UnityEngine;

public class GlowPoint : MonoBehaviour
{
    [SerializeField] private Color glowColor = new Color(1f, 0.95f, 0.6f);
    [SerializeField] private float glowRadius = 1.5f;
    [SerializeField][Range(0.05f, 1f)] private float coreSize = 0.3f;
    [SerializeField][Range(0f, 0.5f)] private float pulseAmount = 0.1f;
    [SerializeField] private float pulseSpeed = 2f;

    [Header("Outer Halo")]
    [Tooltip("外圈光晕大小倍率（相对 glowRadius）")]
    [SerializeField] private float haloScaleMultiplier = 1.6f;
    [Tooltip("外圈光晕最低透明度")]
    [SerializeField] private float haloAlphaMin = 0.03f;
    [Tooltip("外圈光晕最高透明度")]
    [SerializeField] private float haloAlphaMax = 0.12f;

    private SpriteRenderer coreSR;
    private SpriteRenderer auraSR;
    private SpriteRenderer haloSR;
    private float baseAuraAlpha;

    private static Sprite circleSprite;

    private void Start()
    {
        var sprite = RuntimeSprite.Get();

        if (circleSprite == null)
            circleSprite = CreateCircleSprite(32);

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

        auraSR = auraGO.AddComponent<SpriteRenderer>();
        auraSR.sprite = circleSprite;
        baseAuraAlpha = 0.18f;
        auraSR.color = new Color(glowColor.r, glowColor.g, glowColor.b, baseAuraAlpha);
        auraSR.sortingOrder = 7;

        var haloGO = new GameObject("OuterHalo");
        haloGO.transform.SetParent(transform);
        haloGO.transform.localPosition = Vector3.zero;
        float haloScale = (glowRadius * haloScaleMultiplier) / coreSize;
        haloGO.transform.localScale = Vector3.one * haloScale;

        haloSR = haloGO.AddComponent<SpriteRenderer>();
        haloSR.sprite = circleSprite;
        haloSR.color = new Color(glowColor.r, glowColor.g, glowColor.b, haloAlphaMin);
        haloSR.sortingOrder = 6;
    }

    private void Update()
    {
        float sin = Mathf.Sin(Time.time * pulseSpeed);

        if (auraSR != null && pulseAmount > 0f)
        {
            float pulse = 1f + sin * pulseAmount;
            float auraScale = (glowRadius / coreSize) * pulse;
            auraSR.transform.localScale = Vector3.one * auraScale;

            float alpha = baseAuraAlpha * (1f + sin * 0.3f);
            auraSR.color = new Color(glowColor.r, glowColor.g, glowColor.b, alpha);
        }

        if (haloSR != null)
        {
            float haloPulse = 1f + sin * pulseAmount * 1.5f;
            float haloScale = (glowRadius * haloScaleMultiplier / coreSize) * haloPulse;
            haloSR.transform.localScale = Vector3.one * haloScale;

            float haloAlpha = Mathf.Lerp(haloAlphaMin, haloAlphaMax, (sin + 1f) * 0.5f);
            haloSR.color = new Color(glowColor.r, glowColor.g, glowColor.b, haloAlpha);
        }
    }

    private static Sprite CreateCircleSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                float a = Mathf.Clamp01(1f - dist);
                a *= a;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.95f, 0.6f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, glowRadius);
    }
}
