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

    [Header("Visibility")]
    [Tooltip("是否在亮灯阶段显示（关闭则怪物只在黑灯阶段出现）")]
    [SerializeField] private bool showInLight = true;
    [Tooltip("亮灯阶段是否显示打击提示（点和线）")]
    [SerializeField] private bool showComboInLight = true;

    [Header("Appearance")]
    [Tooltip("怪物帧动画序列（拖入多张 Sprite，留空则使用 SpriteRenderer 的默认贴图）")]
    [SerializeField] private Sprite[] animFrames;
    [Tooltip("帧动画播放速度(帧/秒)")]
    [SerializeField] private float animFps = 6f;

    [Header("AI")]
    [Tooltip("开始追踪玩家的距离")]
    [SerializeField] private float detectRange = 5f;
    [Tooltip("追踪移动速度")]
    [SerializeField] private float chaseSpeed = 2f;

    [Header("Combo Display")]
    [Tooltip("打击提示相对怪物的偏移位置")]
    [SerializeField] private Vector2 comboOffset = new Vector2(0, 1.4f);
    [Tooltip("各提示图标之间的间距")]
    [SerializeField] private float comboSpacing = 0.5f;
    [Tooltip("\"点\"图标大小")]
    [SerializeField] private float comboDotSize = 0.35f;
    [Tooltip("\"线\"图标大小(宽, 高)")]
    [SerializeField] private Vector2 comboLineSize = new Vector2(0.7f, 0.25f);
    [Tooltip("\"点\"图标贴图（留空用默认方块）")]
    [SerializeField] private Sprite comboDotSprite;
    [Tooltip("\"线\"图标贴图（留空用默认方块）")]
    [SerializeField] private Sprite comboLineSprite;

    [Header("Knockback")]
    [Tooltip("被子弹击中时的击退距离")]
    [SerializeField] private float knockbackDistance = 0.3f;

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

    [Header("Death Animation")]
    [Tooltip("死亡变黑动画时长(秒)")]
    [SerializeField] private float deathFadeDuration = 0.6f;

    private bool isDying;
    private float deathTimer;

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

        SetupFrameAnimator();
        CreateComboDisplay();
        UpdateComboDisplay();
        CreateHealthBar();
        UpdateHealthBar();

        var pm = LevelPhaseManager.Instance;
        if (pm != null)
        {
            pm.OnPhaseChanged += OnPhaseChanged;
            ApplyPhaseVisibility(pm.CurrentPhase);
        }
    }

    private void OnDestroy()
    {
        var pm = LevelPhaseManager.Instance;
        if (pm != null)
            pm.OnPhaseChanged -= OnPhaseChanged;
    }

    private void Update()
    {
        UpdateDeathAnimation();

        if (playerTransform == null || agent == null || !agent.enabled || !agent.isOnNavMesh) return;

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
        if (isDying) return;
        if (!collision.collider.CompareTag("Player")) return;
        var player = collision.collider.GetComponent<PlayerController>();
        if (player != null)
            player.Die();
    }

    // ── Phase handling ──

    private void OnPhaseChanged(LevelPhase phase)
    {
        ApplyPhaseVisibility(phase);
    }

    private void ApplyPhaseVisibility(LevelPhase phase)
    {
        bool isDark = phase == LevelPhase.Dark;

        if (!showInLight && !isDark)
        {
            SetMonsterActive(false);
            return;
        }

        SetMonsterActive(true);
        SetComboDisplayVisible(!isDark && showComboInLight);
        SetHealthBarVisible(isDark);
    }

    private void SetMonsterActive(bool active)
    {
        if (spriteRenderer != null) spriteRenderer.enabled = active;
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = active;
        if (agent != null) agent.enabled = active;
        SetComboDisplayVisible(false);
        SetHealthBarVisible(false);
    }

    private void SetComboDisplayVisible(bool visible)
    {
        foreach (var go in comboIndicators)
        {
            if (go != null) go.SetActive(visible);
        }
    }

    // ── Combo system ──

    public bool OnBulletHit(BulletType type, Vector2 hitDirection = default)
    {
        var pm = LevelPhaseManager.Instance;
        if (pm != null && pm.CurrentPhase == LevelPhase.Light) return false;

        if (comboIndex >= requiredCombo.Length) return false;

        FlashRed();

        if (knockbackDistance > 0f && hitDirection != Vector2.zero && agent != null && agent.enabled)
            agent.Move(hitDirection * knockbackDistance);

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
        if (agent != null) agent.enabled = false;

        isDying = true;
        deathTimer = 0f;
        SetComboDisplayVisible(false);
        SetHealthBarVisible(false);

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }

    private void UpdateDeathAnimation()
    {
        if (!isDying) return;

        deathTimer += Time.deltaTime;
        float t = Mathf.Clamp01(deathTimer / deathFadeDuration);

        if (spriteRenderer != null)
        {
            float grey = Mathf.Lerp(1f, 0f, t);
            float alpha = Mathf.Lerp(1f, 0f, t * t);
            spriteRenderer.color = new Color(grey * 0.5f, grey * 0.1f, grey * 0.5f, alpha);
        }

        if (t >= 1f)
        {
            isDying = false;
            gameObject.SetActive(false);
        }
    }

    private void SetupFrameAnimator()
    {
        if (animFrames == null || animFrames.Length == 0) return;

        var fa = GetComponent<FrameAnimator>();
        if (fa == null) fa = gameObject.AddComponent<FrameAnimator>();
        fa.FPS = animFps;
        fa.SetFramesAndPlay(animFrames);

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;
    }

    private Color GetBaseColor()
    {
        return (animFrames != null && animFrames.Length > 0) ? Color.white : Color.magenta;
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
            spriteRenderer.color = GetBaseColor();
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
        Sprite fallback = RuntimeSprite.Get();
        float startX = -(requiredCombo.Length - 1) * comboSpacing / 2f;

        for (int i = 0; i < requiredCombo.Length; i++)
        {
            var go = new GameObject($"Combo_{i}");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(
                comboOffset.x + startX + i * comboSpacing, comboOffset.y, 0);

            bool isDot = requiredCombo[i] == BulletType.Dot;
            go.transform.localScale = isDot
                ? Vector3.one * comboDotSize
                : new Vector3(comboLineSize.x, comboLineSize.y, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            if (isDot)
                sr.sprite = comboDotSprite != null ? comboDotSprite : fallback;
            else
                sr.sprite = comboLineSprite != null ? comboLineSprite : fallback;
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

        if (agent != null)
        {
            agent.enabled = true;
            if (agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.Warp(startPosition);
            }
            else
            {
                transform.position = startPosition;
            }
        }
        else
        {
            transform.position = startPosition;
        }

        if (rb != null) rb.velocity = Vector2.zero;
        comboIndex = 0;
        isChasing = false;
        isDying = false;
        deathTimer = 0f;
        UpdateComboDisplay();
        UpdateHealthBar();
        if (spriteRenderer != null)
            spriteRenderer.color = GetBaseColor();

        var fa = GetComponent<FrameAnimator>();
        if (fa != null && animFrames != null && animFrames.Length > 0)
            fa.SetFramesAndPlay(animFrames);

        var pm = LevelPhaseManager.Instance;
        if (pm != null)
            ApplyPhaseVisibility(pm.CurrentPhase);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}
