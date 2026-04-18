using UnityEngine;
using System;
using System.Collections.Generic;

public class DeathEffect : MonoBehaviour
{
    private SpriteRenderer playerSr;
    private GameObject lightCircle;
    private SpriteRenderer circleSr;
    private Color originalColor;
    private float elapsed;
    private float duration;
    private float startRadius;
    private Action onComplete;

    private static Sprite circleSprite;
    private static Sprite maskSprite;

    private struct RevealedInfo
    {
        public SpriteRenderer sr;
        public SpriteMaskInteraction originalMask;
    }
    private List<RevealedInfo> revealedRenderers = new List<RevealedInfo>();

    public void Play(SpriteRenderer playerSprite, float radius, float shrinkDuration, Action callback)
    {
        playerSr = playerSprite;
        originalColor = playerSr != null ? playerSr.color : Color.white;
        startRadius = radius;
        duration = shrinkDuration;
        onComplete = callback;

        if (circleSprite == null)
            circleSprite = CreateCircleSprite(24, true);
        if (maskSprite == null)
            maskSprite = CreateCircleSprite(24, false);

        lightCircle = new GameObject("DeathLight");
        lightCircle.transform.position = transform.position;

        var mask = lightCircle.AddComponent<SpriteMask>();
        mask.sprite = maskSprite;

        circleSr = lightCircle.AddComponent<SpriteRenderer>();
        circleSr.sprite = circleSprite;
        circleSr.color = new Color(1f, 0.95f, 0.7f, 0.5f);
        circleSr.sortingOrder = 25;

        lightCircle.transform.localScale = Vector3.one * startRadius * 2f;

        RevealNearbyRenderers();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        if (playerSr != null)
        {
            float darken = Mathf.Clamp01(elapsed / (duration * 0.3f));
            playerSr.color = Color.Lerp(originalColor, Color.black, darken);
        }

        float t = Mathf.Clamp01(elapsed / duration);
        float easeIn = t * t;
        float currentRadius = Mathf.Lerp(startRadius, 0f, easeIn);

        if (lightCircle != null)
        {
            lightCircle.transform.localScale = Vector3.one * currentRadius * 2f;
            lightCircle.transform.position = transform.position;

            var c = circleSr.color;
            c.a = Mathf.Lerp(0.5f, 0f, easeIn);
            circleSr.color = c;
        }

        if (elapsed >= duration)
        {
            if (playerSr != null)
                playerSr.color = originalColor;
            if (lightCircle != null)
                Destroy(lightCircle);
            RestoreAllRenderers();
            onComplete?.Invoke();
            Destroy(this);
        }
    }

    private void RevealNearbyRenderers()
    {
        var hideables = FindObjectsOfType<DarkPhaseHideable>();
        foreach (var h in hideables)
        {
            var sr = h.GetComponent<SpriteRenderer>();
            if (sr == null || sr.enabled) continue;

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
            info.sr.enabled = false;
            info.sr.maskInteraction = info.originalMask;
        }
        revealedRenderers.Clear();
    }

    private static Sprite CreateCircleSprite(int resolution, bool gradient)
    {
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        float center = resolution / 2f;
        float radiusSq = center * center;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float distSq = dx * dx + dy * dy;
                float ratio = distSq / radiusSq;

                if (ratio > 1f)
                    tex.SetPixel(x, y, Color.clear);
                else
                {
                    float alpha = gradient ? (1f - ratio) : 1f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
        }

        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), Vector2.one * 0.5f, resolution);
    }
}
