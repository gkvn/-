using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CanvasParticles : MonoBehaviour
{
    [Tooltip("粒子数量（留0则自动跟随 DarkPhaseParticles 的数量）")]
    [SerializeField] private int particleCount = 0;

    private RectTransform area;
    private List<UIParticle> particles = new List<UIParticle>();
    private DarkPhaseParticles source;
    private Color currentColor;

    private class UIParticle
    {
        public RectTransform rt;
        public Image img;
        public Vector2 normalizedPos;
        public Vector2 driftDir;
        public float breathPhase;
    }

    private void Start()
    {
        area = GetComponent<RectTransform>();
        if (area == null) return;

        source = FindObjectOfType<DarkPhaseParticles>();

        var pm = LevelPhaseManager.Instance;
        if (pm != null)
            pm.OnPhaseChanged += OnPhaseChanged;

        LevelPhase phase = (pm != null) ? pm.CurrentPhase : LevelPhase.Dark;
        UpdateColor(phase);

        int count = particleCount > 0 ? particleCount : (source != null ? 30 : 30);
        CreateParticles(count);
    }

    private void OnDestroy()
    {
        var pm = LevelPhaseManager.Instance;
        if (pm != null)
            pm.OnPhaseChanged -= OnPhaseChanged;
    }

    private void OnPhaseChanged(LevelPhase phase)
    {
        UpdateColor(phase);
    }

    private void UpdateColor(LevelPhase phase)
    {
        if (source != null)
            currentColor = (phase == LevelPhase.Dark) ? source.DarkColorValue : source.LightColorValue;
        else
            currentColor = new Color(0.5f, 0.7f, 1f, 0.5f);
    }

    private void CreateParticles(int count)
    {
        var circleSprite = RuntimeSprite.GetCircle();

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("CanvasParticle_" + i);
            go.transform.SetParent(area, false);

            var rt = go.AddComponent<RectTransform>();
            float size = source != null ? source.ParticleSizeValue * 100f : 8f;
            rt.sizeDelta = new Vector2(size, size);

            var img = go.AddComponent<Image>();
            img.sprite = circleSprite;
            img.color = currentColor;
            img.raycastTarget = false;

            Vector2 nPos = new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f));
            rt.anchorMin = nPos;
            rt.anchorMax = nPos;
            rt.anchoredPosition = Vector2.zero;

            particles.Add(new UIParticle
            {
                rt = rt,
                img = img,
                normalizedPos = nPos,
                driftDir = Random.insideUnitCircle.normalized,
                breathPhase = Random.Range(0f, Mathf.PI * 2f)
            });
        }
    }

    private void Update()
    {
        if (source != null)
            currentColor = source.CurrentColor;

        float speed = source != null ? source.DriftSpeedValue : 0.3f;
        float driftNorm = speed * 0.02f * Time.deltaTime;

        for (int i = 0; i < particles.Count; i++)
        {
            var p = particles[i];
            if (p.rt == null) continue;

            p.normalizedPos += p.driftDir * driftNorm;

            if (p.normalizedPos.x < 0f || p.normalizedPos.x > 1f ||
                p.normalizedPos.y < 0f || p.normalizedPos.y > 1f)
            {
                p.driftDir = (new Vector2(0.5f, 0.5f) - p.normalizedPos).normalized
                             + Random.insideUnitCircle * 0.3f;
                p.driftDir = p.driftDir.normalized;
                p.normalizedPos.x = Mathf.Clamp01(p.normalizedPos.x);
                p.normalizedPos.y = Mathf.Clamp01(p.normalizedPos.y);
            }

            p.rt.anchorMin = p.normalizedPos;
            p.rt.anchorMax = p.normalizedPos;

            float alpha = currentColor.a * (0.5f + 0.5f * Mathf.Sin(Time.time * 1.5f + p.breathPhase));
            p.img.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
        }
    }
}
