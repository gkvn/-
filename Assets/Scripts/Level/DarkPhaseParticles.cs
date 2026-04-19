using UnityEngine;
using System.Collections.Generic;

public class DarkPhaseParticles : MonoBehaviour
{
    [Tooltip("粒子数量")]
    [SerializeField] private int particleCount = 30;
    [Tooltip("黑夜阶段粒子颜色")]
    [SerializeField] private Color darkColor = new Color(0.5f, 0.7f, 1f, 0.5f);
    [Tooltip("白天阶段粒子颜色")]
    [SerializeField] private Color lightColor = new Color(1f, 0.95f, 0.7f, 0.3f);
    [Tooltip("粒子分布范围(场景中心为原点)")]
    [SerializeField] private float range = 12f;
    [Tooltip("粒子飘动速度")]
    [SerializeField] private float driftSpeed = 0.3f;
    [Tooltip("粒子大小")]
    [SerializeField] private float particleSize = 0.08f;
    [Tooltip("分布中心偏移")]
    [SerializeField] private Vector2 centerOffset = Vector2.zero;

    private Color currentColor;
    public Color CurrentColor => currentColor;
    public Color DarkColorValue => darkColor;
    public Color LightColorValue => lightColor;
    public float DriftSpeedValue => driftSpeed;
    public float ParticleSizeValue => particleSize;

    private List<ParticleData> particles = new List<ParticleData>();
    private bool active;

    private class ParticleData
    {
        public GameObject go;
        public SpriteRenderer sr;
        public Vector2 worldPos;
        public Vector2 driftDir;
        public float breathPhase;
    }

    private void Start()
    {
        var pm = LevelPhaseManager.Instance;
        if (pm != null)
            pm.OnPhaseChanged += OnPhaseChanged;

        LevelPhase phase = (pm != null) ? pm.CurrentPhase : LevelPhase.Dark;
        currentColor = (phase == LevelPhase.Dark) ? darkColor : lightColor;
        CreateParticles();
        SetActive(true);
    }

    private void OnDestroy()
    {
        var pm = LevelPhaseManager.Instance;
        if (pm != null)
            pm.OnPhaseChanged -= OnPhaseChanged;
    }

    private void OnPhaseChanged(LevelPhase phase)
    {
        currentColor = (phase == LevelPhase.Dark) ? darkColor : lightColor;
        ApplyColor();
    }

    private void ApplyColor()
    {
        foreach (var p in particles)
        {
            if (p.sr != null)
                p.sr.color = currentColor;
        }
    }

    private void SetActive(bool on)
    {
        active = on;
        foreach (var p in particles)
        {
            if (p.go != null) p.go.SetActive(on);
        }
    }

    private void CreateParticles()
    {
        var sprite = RuntimeSprite.GetCircle();
        for (int i = 0; i < particleCount; i++)
        {
            var go = new GameObject("DarkParticle_" + i);
            go.transform.SetParent(transform);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = currentColor;
            sr.sortingOrder = 15;
            go.transform.localScale = Vector3.one * particleSize;

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(0.5f, range);
            Vector2 pos = centerOffset + new Vector2(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist);

            particles.Add(new ParticleData
            {
                go = go,
                sr = sr,
                worldPos = pos,
                driftDir = Random.insideUnitCircle.normalized,
                breathPhase = Random.Range(0f, Mathf.PI * 2f)
            });
            go.transform.position = pos;
            go.SetActive(false);
        }
    }

    private void Update()
    {
        if (!active) return;

        for (int i = 0; i < particles.Count; i++)
        {
            var p = particles[i];
            if (p.go == null) continue;

            p.worldPos += p.driftDir * (driftSpeed * Time.deltaTime);

            if (((Vector2)p.worldPos - centerOffset).magnitude > range)
            {
                p.driftDir = (centerOffset - p.worldPos).normalized + Random.insideUnitCircle * 0.3f;
                p.driftDir = p.driftDir.normalized;
            }

            p.go.transform.position = (Vector3)(p.worldPos);

            float alpha = currentColor.a * (0.5f + 0.5f * Mathf.Sin(Time.time * 1.5f + p.breathPhase));
            p.sr.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
        }
    }
}
