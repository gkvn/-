using UnityEngine;
using System.Collections.Generic;

public class DarkPhaseParticles : MonoBehaviour
{
    [Tooltip("粒子数量")]
    [SerializeField] private int particleCount = 30;
    [Tooltip("粒子颜色")]
    [SerializeField] private Color particleColor = new Color(0.5f, 0.7f, 1f, 0.5f);
    [Tooltip("粒子分布范围(场景中心为原点)")]
    [SerializeField] private float range = 12f;
    [Tooltip("粒子飘动速度")]
    [SerializeField] private float driftSpeed = 0.3f;
    [Tooltip("粒子大小")]
    [SerializeField] private float particleSize = 0.08f;
    [Tooltip("分布中心偏移")]
    [SerializeField] private Vector2 centerOffset = Vector2.zero;

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

        CreateParticles();
        SetActive(pm != null && pm.CurrentPhase == LevelPhase.Dark);
    }

    private void OnDestroy()
    {
        var pm = LevelPhaseManager.Instance;
        if (pm != null)
            pm.OnPhaseChanged -= OnPhaseChanged;
    }

    private void OnPhaseChanged(LevelPhase phase)
    {
        SetActive(phase == LevelPhase.Dark);
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
        var sprite = RuntimeSprite.Get();
        for (int i = 0; i < particleCount; i++)
        {
            var go = new GameObject("DarkParticle_" + i);
            go.transform.SetParent(transform);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = particleColor;
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

            float alpha = particleColor.a * (0.5f + 0.5f * Mathf.Sin(Time.time * 1.5f + p.breathPhase));
            var c = p.sr.color;
            p.sr.color = new Color(c.r, c.g, c.b, alpha);
        }
    }
}
