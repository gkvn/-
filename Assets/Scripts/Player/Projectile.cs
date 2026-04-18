using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 3f;

    [Header("子弹贴图")]
    [SerializeField] private Sprite dotSprite;
    [SerializeField] private Sprite lineSprite;

    public BulletType Type { get; private set; }

    private Rigidbody2D rb;

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
    }

    public void Launch(Vector2 direction)
    {
        Launch(direction, BulletType.Dot);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) return;

        var monster = other.GetComponent<Monster>();
        if (monster != null)
            monster.OnBulletHit(Type);

        Destroy(gameObject);
    }
}
