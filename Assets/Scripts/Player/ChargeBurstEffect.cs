using UnityEngine;

public class ChargeBurstEffect : MonoBehaviour
{
    private float elapsed;
    private const float Duration = 0.12f;
    private const float MaxScale = 0.5f;
    private SpriteRenderer sr;

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
        transform.localScale = Vector3.one * Mathf.Lerp(0.1f, MaxScale, easeOut);

        if (sr != null)
        {
            var c = sr.color;
            c.a = Mathf.Lerp(0.9f, 0f, t);
            sr.color = c;
        }
    }
}
