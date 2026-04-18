using UnityEngine;
using System;
using System.Collections.Generic;

public class VictoryEffect : MonoBehaviour
{
    private Transform center;
    private GameObject spriteA;
    private GameObject spriteB;
    private GameObject glowA;
    private GameObject glowB;
    private float elapsed;
    private float duration;
    private float orbitRadius;
    private Color glowColor;
    private Action onComplete;
    private bool finished;

    private static Sprite circleGradient;
    private static Sprite circleSolid;

    private struct RevealedInfo
    {
        public SpriteRenderer sr;
        public SpriteMaskInteraction originalMask;
    }
    private List<RevealedInfo> revealedRenderers = new List<RevealedInfo>();

    public void Play(Transform target, Sprite sprA, Sprite sprB,
                     float radius, float rotDuration, Color glow, Action callback)
    {
        center = target;
        duration = rotDuration;
        orbitRadius = radius;
        glowColor = glow;
        onComplete = callback;

        if (circleGradient == null)
            circleGradient = CreateCircleSprite(32, true);
        if (circleSolid == null)
            circleSolid = CreateCircleSprite(32, false);

        spriteA = CreateOrbitSprite("VictoryA", sprA, 0f);
        spriteB = CreateOrbitSprite("VictoryB", sprB, 180f);
        glowA = CreateGlowCircle("GlowA", spriteA);
        glowB = CreateGlowCircle("GlowB", spriteB);

        RevealNearbyRenderers();
    }

    private GameObject CreateOrbitSprite(string name, Sprite spr, float startAngle)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spr != null ? spr : RuntimeSprite.Get();
        sr.sortingOrder = 26;
        sr.color = Color.white;
        go.transform.localScale = Vector3.one * 0.5f;

        float rad = startAngle * Mathf.Deg2Rad;
        go.transform.position = center.position +
            new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * orbitRadius;

        return go;
    }

    private GameObject CreateGlowCircle(string name, GameObject parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one * 4f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = circleGradient;
        sr.sortingOrder = 25;
        sr.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0.35f);

        var maskGo = new GameObject(name + "_Mask");
        maskGo.transform.SetParent(parent.transform);
        maskGo.transform.localPosition = Vector3.zero;
        maskGo.transform.localScale = Vector3.one * 4f;
        var mask = maskGo.AddComponent<SpriteMask>();
        mask.sprite = circleSolid;

        return go;
    }

    private void Update()
    {
        if (finished || center == null) return;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        float angle = t * 360f * Mathf.Deg2Rad;

        if (spriteA != null)
        {
            spriteA.transform.position = center.position +
                new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * orbitRadius;
        }
        if (spriteB != null)
        {
            spriteB.transform.position = center.position +
                new Vector3(Mathf.Cos(angle + Mathf.PI), Mathf.Sin(angle + Mathf.PI), 0) * orbitRadius;
        }

        float pulse = (Mathf.Sin(elapsed * 10f) + 1f) * 0.5f;
        float glowAlpha = Mathf.Lerp(0.2f, 0.5f, pulse);
        float glowScale = Mathf.Lerp(3.5f, 5f, pulse);
        UpdateGlow(glowA, glowAlpha, glowScale);
        UpdateGlow(glowB, glowAlpha, glowScale);

        if (t >= 1f)
        {
            finished = true;
            RestoreAllRenderers();
            if (spriteA != null) Destroy(spriteA);
            if (spriteB != null) Destroy(spriteB);
            onComplete?.Invoke();
            Destroy(gameObject);
        }
    }

    private void UpdateGlow(GameObject glow, float alpha, float scale)
    {
        if (glow == null) return;
        glow.transform.localScale = Vector3.one * scale;
        var sr = glow.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = new Color(glowColor.r, glowColor.g, glowColor.b, alpha);

        var maskTr = glow.transform.parent.Find(glow.name + "_Mask");
        if (maskTr != null)
            maskTr.localScale = Vector3.one * scale;
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
        float c = resolution / 2f;
        float radiusSq = c * c;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dx = x - c + 0.5f;
                float dy = y - c + 0.5f;
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
