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

    [Header("Health Bar")]
    [Tooltip("血条相对怪物的偏移位置")]
    [SerializeField] private Vector2 healthBarOffset = new Vector2(0, 1.4f);
    [Tooltip("血条宽度")]
    [SerializeField] private float healthBarWidth = 1.2f;
    [Tooltip("血条高度")]
    [SerializeField] private float healthBarHeight = 0.15f;

    private int comboIndex;
    private List<GameObject> comboIndicators = new List<GameObject>();
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private NavMeshAgent agent;
    private Transform playerTransform;
    private bool isChasing;
    private Vector3 startPosition;

    private GameObject healthBarBg;
    private GameObject healthBarFill;
    private List<GameObject> healthBarDividers = new List<GameObject>();

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
        CreateHealthBar();
        UpdateHealthBar();

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
        SetHealthBarVisible(isDark);
    }

    private void SetComboDisplayVisible(bool visible)
    {
        foreach (var go in comboIndicators)
        {
            if (go != null) go.SetActive(visible);
        }
    }

    // ── Combo system ──

    public bool OnBulletHit(BulletType type)
    {
        var pm = LevelPhaseManager.Instance;
        if (pm != null && pm.CurrentPhase == LevelPhase.Light) return false;

        if (comboIndex >= requiredCombo.Length) return false;

        FlashRed();

        bool correct = requiredCombo[comboIndex] == type;
        if (correct)
        {
            comboIndex++;
            Debug.Log($"[Monster] Correct! {comboIndex}/{requiredCombo.Length}");
            UpdateHealthBar();

            if (comboIndex >= requiredCombo.Length)
            {
                Defeat();
                return true;
            }
        }
        else
        {
            comboIndex = 0;
            Debug.Log("[Monster] Wrong input! Combo reset.");
            UpdateHealthBar();
        }

        UpdateComboDisplay();
        return correct;
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

    // ── Health bar ──

    private void CreateHealthBar()
    {
        Sprite spr = RuntimeSprite.Get();

        healthBarBg = new GameObject("HealthBarBg");
        healthBarBg.transform.SetParent(transform);
        healthBarBg.transform.localPosition = new Vector3(healthBarOffset.x, healthBarOffset.y, 0);
        healthBarBg.transform.localScale = new Vector3(healthBarWidth, healthBarHeight, 1);
        var bgSr = healthBarBg.AddComponent<SpriteRenderer>();
        bgSr.sprite = spr;
        bgSr.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        bgSr.sortingOrder = 16;

        healthBarFill = new GameObject("HealthBarFill");
        healthBarFill.transform.SetParent(transform);
        healthBarFill.transform.localPosition = new Vector3(healthBarOffset.x, healthBarOffset.y, 0);
        healthBarFill.transform.localScale = new Vector3(healthBarWidth, healthBarHeight, 1);
        var fillSr = healthBarFill.AddComponent<SpriteRenderer>();
        fillSr.sprite = spr;
        fillSr.color = new Color(0.9f, 0.2f, 0.2f, 0.9f);
        fillSr.sortingOrder = 17;

        CreateHealthBarDividers(spr);
        SetHealthBarVisible(false);
    }

    private void CreateHealthBarDividers(Sprite spr)
    {
        int total = requiredCombo.Length;
        if (total <= 1) return;

        float dividerWidth = healthBarHeight * 0.3f;
        float segmentWidth = healthBarWidth / total;
        float barLeft = healthBarOffset.x - healthBarWidth / 2f;

        for (int i = 1; i < total; i++)
        {
            var div = new GameObject($"HealthBarDiv_{i}");
            div.transform.SetParent(transform);
            float x = barLeft + segmentWidth * i;
            div.transform.localPosition = new Vector3(x, healthBarOffset.y, 0);
            div.transform.localScale = new Vector3(dividerWidth, healthBarHeight * 1.1f, 1);
            var sr = div.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            sr.sortingOrder = 18;
            healthBarDividers.Add(div);
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBarFill == null || healthBarBg == null) return;
        float total = requiredCombo.Length;
        float remaining = total - comboIndex;
        float ratio = remaining / total;

        float w = healthBarWidth * ratio;
        float offset = -(healthBarWidth - w) / 2f;

        healthBarFill.transform.localScale = new Vector3(w, healthBarHeight, 1);
        healthBarFill.transform.localPosition = new Vector3(healthBarOffset.x + offset, healthBarOffset.y, 0);

        var fillSr = healthBarFill.GetComponent<SpriteRenderer>();
        if (fillSr != null)
        {
            if (ratio > 0.5f) fillSr.color = new Color(0.9f, 0.2f, 0.2f, 0.9f);
            else if (ratio > 0.25f) fillSr.color = new Color(0.9f, 0.6f, 0.1f, 0.9f);
            else fillSr.color = new Color(0.9f, 0.9f, 0.1f, 0.9f);
        }
    }

    private void SetHealthBarVisible(bool visible)
    {
        if (healthBarBg != null) healthBarBg.SetActive(visible);
        if (healthBarFill != null) healthBarFill.SetActive(visible);
        foreach (var div in healthBarDividers)
        {
            if (div != null) div.SetActive(visible);
        }
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
            go.transform.localPosition = new Vector3(healthBarOffset.x + startX + i * spacing, healthBarOffset.y, 0);

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
        UpdateHealthBar();
        if (spriteRenderer != null)
            spriteRenderer.color = Color.magenta;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}
