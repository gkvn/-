using UnityEngine;

public class HitEffect : MonoBehaviour
{
    private float maxScale;
    private Color baseColor;
    private float elapsed;
    private const float Duration = 0.2f;
    private SpriteRenderer sr;

    public void Init(float scale, Color color)
    {
        maxScale = scale;
        baseColor = color;
        baseColor.a = 0.9f;
    }

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / Duration;

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        float easeOut = 1f - (1f - t) * (1f - t);
        transform.localScale = Vector3.one * Mathf.Lerp(0.1f, maxScale, easeOut);

        if (sr != null)
        {
            var c = baseColor;
            c.a = Mathf.Lerp(0.9f, 0f, t);
            sr.color = c;
        }
    }
}
