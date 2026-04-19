using UnityEngine;

public class AfterImageEffect : MonoBehaviour
{
    [Tooltip("扬尘生成间隔(秒)")]
    [SerializeField] private float spawnInterval = 0.08f;
    [Tooltip("扬尘淡出时长(秒)")]
    [SerializeField] private float fadeDuration = 0.35f;
    [Tooltip("扬尘初始透明度")]
    [SerializeField] private float startAlpha = 0.35f;
    [Tooltip("扬尘粒子大小")]
    [SerializeField] private float dustSize = 0.12f;
    [Tooltip("扬尘相对角色脚底的偏移")]
    [SerializeField] private Vector2 footOffset = new Vector2(0, -0.4f);
    [Tooltip("扬尘散布范围")]
    [SerializeField] private float spread = 0.25f;
    [Tooltip("扬尘颜色")]
    [SerializeField] private Color dustColor = new Color(0.75f, 0.65f, 0.5f, 0.35f);
    [Tooltip("扬尘向上飘动速度")]
    [SerializeField] private float riseSpeed = 0.4f;

    private float timer;

    public void Tick(bool isMoving)
    {
        if (!isMoving)
        {
            timer = 0f;
            return;
        }

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer -= spawnInterval;
            SpawnDust();
        }
    }

    private void SpawnDust()
    {
        Vector2 basePos = (Vector2)transform.position + footOffset;
        Vector2 pos = basePos + Random.insideUnitCircle * spread;

        var go = new GameObject("FootDust");
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * dustSize * Random.Range(0.7f, 1.3f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = RuntimeSprite.Get();
        sr.sortingOrder = 1;
        var c = dustColor;
        c.a = startAlpha;
        sr.color = c;

        go.AddComponent<DustFade>().Init(fadeDuration, riseSpeed);
    }
}

public class DustFade : MonoBehaviour
{
    private SpriteRenderer sr;
    private float duration;
    private float elapsed;
    private Color startColor;
    private float rise;

    public void Init(float dur, float riseSpeed)
    {
        sr = GetComponent<SpriteRenderer>();
        startColor = sr.color;
        duration = dur;
        rise = riseSpeed;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;
        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += Vector3.up * (rise * Time.deltaTime);
        transform.localScale *= (1f - Time.deltaTime * 1.5f);

        var c = startColor;
        c.a = Mathf.Lerp(startColor.a, 0f, t);
        sr.color = c;
    }
}
