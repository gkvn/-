using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(NavMeshAgent))]
public class Monster : MonoBehaviour, IResettable
{
    [Header("Combo Sequence")]
    [Tooltip("击败此怪物所需的子弹组合（Dot=点击, Line=长按）")]
    [SerializeField] private BulletType[] requiredCombo = new BulletType[]
    {
        BulletType.Dot, BulletType.Dot, BulletType.Line
    };

    [Header("AI")]
    [Tooltip("开始追踪玩家的距离")]
    [SerializeField] private float detectRange = 5f;
    [Tooltip("追踪移动速度")]
    [SerializeField] private float chaseSpeed = 2f;

    private int comboIndex;
    private List<GameObject> comboIndicators = new List<GameObject>();
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private NavMeshAgent agent;
    private Transform playerTransform;
    private bool isChasing;
    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();

        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;

        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = false;

        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = chaseSpeed;
        agent.acceleration = 8f;
        agent.stoppingDistance = 0.1f;

        var player = FindObjectOfType<PlayerController>();
        if (player != null) playerTransform = player.transform;

        Debug.Log($"[Monster] Start — isOnNavMesh={agent.isOnNavMesh}, pos={transform.position}");
        if (!agent.isOnNavMesh)
            Debug.LogWarning("[Monster] 不在 NavMesh 上！请运行 Tools → 创建并烘焙 NavMesh 2D");

        CreateComboDisplay();
        UpdateComboDisplay();

        var pm = LevelPhaseManager.Instance;
        if (pm != null)
            pm.OnPhaseChanged += OnPhaseChanged;
    }

    private void OnDestroy()
    {
        var pm = LevelPhaseManager.Instance;
        if (pm != null)
            pm.OnPhaseChanged -= OnPhaseChanged;
    }

    private void Update()
    {
        if (playerTransform == null || agent == null || !agent.isOnNavMesh) return;

        var pm = LevelPhaseManager.Instance;
        bool shouldChase = pm != null && pm.CurrentPhase == LevelPhase.Dark;

        if (!shouldChase)
        {
            if (agent.hasPath) agent.ResetPath();
            agent.isStopped = true;
            return;
        }

        agent.isStopped = false;
        float dist = Vector2.Distance(transform.position, playerTransform.position);
        isChasing = dist <= detectRange;

        if (isChasing)
            agent.SetDestination(playerTransform.position);
        else if (agent.hasPath)
            agent.ResetPath();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Player")) return;
        var player = collision.collider.GetComponent<PlayerController>();
        if (player != null)
            player.Die();
    }

    // ── Phase handling ──

    private void OnPhaseChanged(LevelPhase phase)
    {
        bool isDark = phase == LevelPhase.Dark;
        SetComboDisplayVisible(!isDark);
    }

    private void SetComboDisplayVisible(bool visible)
    {
        foreach (var go in comboIndicators)
        {
            if (go != null) go.SetActive(visible);
        }
    }

    // ── Combo system ──

    public void OnBulletHit(BulletType type)
    {
        var pm = LevelPhaseManager.Instance;
        if (pm != null && pm.CurrentPhase == LevelPhase.Light) return;

        if (comboIndex >= requiredCombo.Length) return;

        FlashRed();

        if (requiredCombo[comboIndex] == type)
        {
            comboIndex++;
            Debug.Log($"[Monster] Correct! {comboIndex}/{requiredCombo.Length}");

            if (comboIndex >= requiredCombo.Length)
            {
                Defeat();
                return;
            }
        }
        else
        {
            comboIndex = 0;
            Debug.Log("[Monster] Wrong input! Combo reset.");
        }

        UpdateComboDisplay();
    }

    private void Defeat()
    {
        Debug.Log("[Monster] Defeated!");
        if (agent != null && agent.isOnNavMesh) agent.ResetPath();
        gameObject.SetActive(false);
    }

    private void FlashRed()
    {
        if (spriteRenderer == null) return;
        CancelInvoke(nameof(ResetColor));
        spriteRenderer.color = Color.red;
        Invoke(nameof(ResetColor), 0.15f);
    }

    private void ResetColor()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = Color.magenta;
    }

    // ── Combo display ──

    private void CreateComboDisplay()
    {
        var parentSR = GetComponent<SpriteRenderer>();
        Sprite baseSprite = parentSR != null ? parentSR.sprite : null;
        if (baseSprite == null) baseSprite = RuntimeSprite.Get();

        float spacing = 0.5f;
        float startX = -(requiredCombo.Length - 1) * spacing / 2f;

        for (int i = 0; i < requiredCombo.Length; i++)
        {
            var go = new GameObject($"Combo_{i}");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(startX + i * spacing, 1.8f, 0);

            bool isDot = requiredCombo[i] == BulletType.Dot;
            go.transform.localScale = isDot
                ? Vector3.one * 0.35f
                : new Vector3(0.7f, 0.25f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = baseSprite;
            sr.sortingOrder = 15;
            sr.color = Color.gray;

            comboIndicators.Add(go);
        }
    }

    private void UpdateComboDisplay()
    {
        for (int i = 0; i < comboIndicators.Count; i++)
        {
            var sr = comboIndicators[i].GetComponent<SpriteRenderer>();
            if (sr == null) continue;

            if (i < comboIndex)
                sr.color = Color.green;
            else if (i == comboIndex)
                sr.color = Color.white;
            else
                sr.color = Color.gray;
        }
    }

    public void ResetState()
    {
        gameObject.SetActive(true);

        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.Warp(startPosition);
        }
        else
        {
            transform.position = startPosition;
        }

        if (rb != null) rb.velocity = Vector2.zero;
        comboIndex = 0;
        isChasing = false;
        UpdateComboDisplay();
        if (spriteRenderer != null)
            spriteRenderer.color = Color.magenta;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}
