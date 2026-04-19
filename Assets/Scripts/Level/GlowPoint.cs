using UnityEngine;
using System.Collections.Generic;

public class GlowPoint : MonoBehaviour
{
    [SerializeField] private Color glowColor = new Color(1f, 0.95f, 0.6f);
    [SerializeField] private float glowRadius = 1.5f;
    [Tooltip("中间光点大小")]
    [SerializeField] private float coreSize = 0.3f;
    [SerializeField][Range(0f, 0.5f)] private float pulseAmount = 0.1f;
    [SerializeField] private float pulseSpeed = 2f;

    [Header("Outer Halo")]
    [Tooltip("外圈光晕大小倍率（相对 glowRadius）")]
    [SerializeField] private float haloScaleMultiplier = 1.6f;
    [Tooltip("外圈光晕最低透明度")]
    [SerializeField] private float haloAlphaMin = 0.03f;
    [Tooltip("外圈光晕最高透明度")]
    [SerializeField] private float haloAlphaMax = 0.12f;

    [Header("Reveal")]
    [Tooltip("是否在黑夜阶段照亮范围内的物体")]
    [SerializeField] private bool revealInDark = true;

    private SpriteRenderer coreSR;
    private SpriteRenderer auraSR;
    private SpriteRenderer haloSR;
    private SpriteMask mask;
    private float baseAuraAlpha;

    private static Sprite circleGradient;
    private static Sprite circleSolid;

    private struct RevealedInfo
    {
        public SpriteRenderer sr;
        public SpriteMaskInteraction originalMask;
    }
    private List<RevealedInfo> revealedRenderers = new List<RevealedInfo>();
    private bool isRevealing;

    private void Start()
    {
        var sprite = RuntimeSprite.Get();

        if (circleGradient == null)
            circleGradient = CreateCircleSprite(32, true);
        if (circleSolid == null)
            circleSolid = CreateCircleSprite(32, false);

        var coreGO = new GameObject("Core");
        coreGO.transform.SetParent(transform);
        coreGO.transform.localPosition = Vector3.zero;
        coreGO.transform.localScale = Vector3.one * coreSize;
        coreSR = coreGO.AddComponent<SpriteRenderer>();
        coreSR.sprite = sprite;
        coreSR.color = glowColor;
        coreSR.sortingOrder = 8;

        var auraGO = new GameObject("Aura");
        auraGO.transform.SetParent(transform);
        auraGO.transform.localPosition = Vector3.zero;
        auraGO.transform.localScale = Vector3.one * glowRadius;

        auraSR = auraGO.AddComponent<SpriteRenderer>();
        auraSR.sprite = circleGradient;
        baseAuraAlpha = 0.18f;
        auraSR.color = new Color(glowColor.r, glowColor.g, glowColor.b, baseAuraAlpha);
        auraSR.sortingOrder = 7;

        if (revealInDark)
        {
            mask = auraGO.AddComponent<SpriteMask>();
            mask.sprite = circleSolid;
        }

        var haloGO = new GameObject("OuterHalo");
        haloGO.transform.SetParent(transform);
        haloGO.transform.localPosition = Vector3.zero;
        haloGO.transform.localScale = Vector3.one * glowRadius * haloScaleMultiplier;

        haloSR = haloGO.AddComponent<SpriteRenderer>();
        haloSR.sprite = circleGradient;
        haloSR.color = new Color(glowColor.r, glowColor.g, glowColor.b, haloAlphaMin);
        haloSR.sortingOrder = 6;

        var pm = LevelPhaseManager.Instance;
        if (pm != null)
        {
            pm.OnPhaseChanged += OnPhaseChanged;
            OnPhaseChanged(pm.CurrentPhase);
        }
    }

    private void OnDestroy()
    {
        var pm = LevelPhaseManager.Instance;
        if (pm != null)
            pm.OnPhaseChanged -= OnPhaseChanged;

        RestoreAllRenderers();
    }

    private void OnPhaseChanged(LevelPhase phase)
    {
        if (!revealInDark) return;

        if (phase == LevelPhase.Dark)
            RevealNearbyRenderers();
        else
            RestoreAllRenderers();
    }

    private void Update()
    {
        float sin = Mathf.Sin(Time.time * pulseSpeed);

        if (auraSR != null && pulseAmount > 0f)
        {
            float pulse = 1f + sin * pulseAmount;
            auraSR.transform.localScale = Vector3.one * glowRadius * pulse;

            float alpha = baseAuraAlpha * (1f + sin * 0.3f);
            auraSR.color = new Color(glowColor.r, glowColor.g, glowColor.b, alpha);
        }

        if (haloSR != null)
        {
            float haloPulse = 1f + sin * pulseAmount * 1.5f;
            haloSR.transform.localScale = Vector3.one * glowRadius * haloScaleMultiplier * haloPulse;

            float haloAlpha = Mathf.Lerp(haloAlphaMin, haloAlphaMax, (sin + 1f) * 0.5f);
            haloSR.color = new Color(glowColor.r, glowColor.g, glowColor.b, haloAlpha);
        }
    }

    private void RevealNearbyRenderers()
    {
        if (isRevealing) return;
        isRevealing = true;

        var hideables = FindObjectsOfType<DarkPhaseHideable>();
        foreach (var h in hideables)
        {
            float dist = Vector2.Distance(transform.position, h.transform.position);
            if (dist > glowRadius) continue;

            var sr = h.GetComponent<SpriteRenderer>();
            if (sr == null) continue;

            revealedRenderers.Add(new RevealedInfo
            {
                sr = sr,
                originalMask = sr.maskInteraction
            });

            sr.enabled = true;
            sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }
    }

    private void RestoreAllRenderers()
    {
        foreach (var info in revealedRenderers)
        {
            if (info.sr == null) continue;
            info.sr.maskInteraction = info.originalMask;
        }
        revealedRenderers.Clear();
        isRevealing = false;
    }

    private static Sprite CreateCircleSprite(int size, bool gradient)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size * 0.5f;
        float radiusSq = center * center;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float distSq = dx * dx + dy * dy;
                float ratio = distSq / radiusSq;

                if (ratio > 1f)
                    tex.SetPixel(x, y, Color.clear);
                else
                {
                    float a = gradient ? (1f - ratio) : 1f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.95f, 0.6f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, glowRadius);
    }
}
