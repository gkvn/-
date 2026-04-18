using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 3f;

    [Header("子弹贴图")]
    [SerializeField] private Sprite dotSprite;
    [SerializeField] private Sprite lineSprite;

    [Header("命中停留")]
    [Tooltip("击中后子弹停留时间(秒)")]
    [SerializeField] private float hitLingerTime = 0.4f;

    public BulletType Type { get; private set; }

    private Rigidbody2D rb;
    private bool hasHit;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        Destroy(gameObject, lifetime);
    }

    public void Launch(Vector2 direction, BulletType type)
    {
        Type = type;
        rb.velocity = direction.normalized * speed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        if (sr.sprite == null)
            sr.sprite = RuntimeSprite.Get();

        if (type == BulletType.Line)
        {
            if (lineSprite != null) sr.sprite = lineSprite;
            transform.localScale = new Vector3(1.2f, 0.3f, 1f);
            sr.color = new Color(0.4f, 0.8f, 1f);
        }
        else
        {
            if (dotSprite != null) sr.sprite = dotSprite;
            transform.localScale = Vector3.one * 0.5f;
            sr.color = Color.yellow;
        }

        var col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            float maxScale = Mathf.Max(transform.localScale.x, transform.localScale.y);
            col.radius = 0.2f / maxScale;
        }

    }

    public void Launch(Vector2 direction)
    {
        Launch(direction, BulletType.Dot);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        if (other.CompareTag("Player")) return;

        var monster = other.GetComponent<Monster>();
        if (monster != null)
        {
            bool wasCorrect = monster.OnBulletHit(Type);
            HitStop(wasCorrect ? HitType.MonsterCorrect : HitType.MonsterWrong);
            return;
        }

        if (!other.isTrigger)
            HitStop(HitType.Wall);
    }

    private enum HitType { Wall, MonsterCorrect, MonsterWrong }

    private void HitStop(HitType hitType)
    {
        hasHit = true;
        rb.velocity = Vector2.zero;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        SpawnHitEffect(hitType);

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.4f);

        CancelInvoke();
        Destroy(gameObject, hitLingerTime);
    }

    private void SpawnHitEffect(HitType hitType)
    {
        var fx = new GameObject("HitFX");
        fx.transform.position = transform.position;

        var sr = fx.AddComponent<SpriteRenderer>();
        sr.sprite = RuntimeSprite.Get();
        sr.sortingOrder = 18;

        switch (hitType)
        {
            case HitType.Wall:
                sr.color = new Color(1f, 1f, 1f, 0.8f);
                fx.transform.localScale = Vector3.one * 0.15f;
                fx.AddComponent<HitEffect>().Init(0.5f, Color.white);
                break;

            case HitType.MonsterCorrect:
                sr.color = new Color(1f, 0.3f, 0.1f, 0.9f);
                fx.transform.localScale = Vector3.one * 0.2f;
                fx.AddComponent<HitEffect>().Init(0.8f, new Color(1f, 0.3f, 0.1f));
                break;

            case HitType.MonsterWrong:
                sr.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
                fx.transform.localScale = Vector3.one * 0.15f;
                fx.AddComponent<HitEffect>().Init(0.4f, Color.gray);
                break;
        }
    }
}
