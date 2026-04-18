using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Combat")]
    [SerializeField] private GameObject projectilePrefab;
    [Tooltip("按住超过此时长(秒)发射线子弹，否则发射点子弹")]
    [SerializeField] private float holdThreshold = 0.25f;
    [Tooltip("子弹发射点相对角色的偏移")]
    [SerializeField] private Vector2 bulletSpawnOffset = new Vector2(0, 0.5f);
    [Tooltip("子弹生成点离墙壁的安全距离")]
    [SerializeField] private float bulletWallMargin = 0.05f;

    [Header("Appearance")]
    [Tooltip("亮灯阶段角色贴图")]
    [SerializeField] private Sprite lightSprite;
    [Tooltip("黑灯阶段角色贴图")]
    [SerializeField] private Sprite darkSprite;

    [Header("Charge Indicator")]
    [Tooltip("蓄力条相对角色的位置")]
    [SerializeField] private Vector2 chargeBarOffset = new Vector2(0, -0.8f);
    [Tooltip("蓄力条满时的宽度")]
    [SerializeField] private float chargeBarWidth = 0.8f;
    [Tooltip("蓄力条高度")]
    [SerializeField] private float chargeBarHeight = 0.1f;
    [Tooltip("蓄力条贴图（留空则自动生成黄色纯色）")]
    [SerializeField] private Sprite chargeBarSprite;
    [Tooltip("蓄力中颜色")]
    [SerializeField] private Color chargeBarChargingColor = Color.yellow;
    [Tooltip("蓄力满颜色")]
    [SerializeField] private Color chargeBarReadyColor = new Color(0.4f, 0.8f, 1f);

    [Header("Interaction")]
    [SerializeField] private float interactRange = 1.2f;
    [SerializeField] private Vector2 promptOffset = new Vector2(0, 1.2f);
    [Tooltip("交互提示图标大小")]
    [SerializeField] private float promptScale = 0.4f;
    [Tooltip("交互提示图标贴图（留空则使用角色贴图或默认白块）")]
    [SerializeField] private Sprite promptSprite;

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;
    private Vector2 facingDirection = Vector2.down;
    private bool isDead;

    private Camera gameCamera;
    private float mouseDownTime;
    private bool mouseIsDown;
    private GameObject interactPrompt;
    private GameObject chargeIndicator;
    private GameObject chargeGlowLeft;
    private GameObject chargeGlowRight;

    private static readonly int IsRunning = Animator.StringToHash("isRunning");
    private static readonly int ShootTrigger = Animator.StringToHash("isShooting");
    private static readonly int InteractTrigger = Animator.StringToHash("isInteracting");

    public Vector2 FacingDirection => facingDirection;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        var tdc = FindObjectOfType<TopDownCamera>();
        gameCamera = tdc != null ? tdc.Cam : Camera.main;
        if (gameCamera == null) gameCamera = FindObjectOfType<Camera>();
        Debug.Log($"[Player] gameCamera={gameCamera != null}, projectilePrefab={projectilePrefab != null}");

        CreateInteractPrompt();
        CreateChargeIndicator();
    }

    private bool subscribedToPhase;

    private void OnEnable()
    {
        TrySubscribePhase();
    }

    private void Start()
    {
        TrySubscribePhase();
    }

    private void TrySubscribePhase()
    {
        if (subscribedToPhase) return;
        var pm = LevelPhaseManager.Instance;
        if (pm == null) return;
        pm.OnPhaseChanged += OnPhaseChanged;
        ApplyPhaseSprite(pm.CurrentPhase);
        subscribedToPhase = true;
    }

    private void OnDisable()
    {
        var pm = LevelPhaseManager.Instance;
        if (pm != null)
            pm.OnPhaseChanged -= OnPhaseChanged;
        subscribedToPhase = false;
    }

    private void OnPhaseChanged(LevelPhase phase)
    {
        ApplyPhaseSprite(phase);
    }

    private void ApplyPhaseSprite(LevelPhase phase)
    {
        if (spriteRenderer == null) return;
        Sprite target = phase == LevelPhase.Dark ? darkSprite : lightSprite;
        if (target != null)
            spriteRenderer.sprite = target;
    }

    private Sprite GetSpriteOrFallback()
    {
        var parentSR = GetComponent<SpriteRenderer>();
        if (parentSR != null && parentSR.sprite != null)
            return parentSR.sprite;
        return RuntimeSprite.Get();
    }

    private void CreateInteractPrompt()
    {
        interactPrompt = new GameObject("InteractPrompt");
        interactPrompt.tag = "Player";
        interactPrompt.transform.SetParent(transform);
        interactPrompt.transform.localPosition = promptOffset;
        interactPrompt.transform.localScale = Vector3.one * promptScale;
        foreach (var c in interactPrompt.GetComponents<Collider2D>()) Destroy(c);
        var sr = interactPrompt.AddComponent<SpriteRenderer>();
        sr.sprite = promptSprite != null ? promptSprite : GetSpriteOrFallback();
        sr.color = Color.white;
        sr.sortingOrder = 20;
        interactPrompt.SetActive(false);
    }

    private void CreateChargeIndicator()
    {
        chargeIndicator = new GameObject("ChargeIndicator");
        chargeIndicator.tag = "Player";
        chargeIndicator.transform.SetParent(transform);
        chargeIndicator.transform.localPosition = new Vector3(chargeBarOffset.x, chargeBarOffset.y, 0);
        chargeIndicator.transform.localScale = new Vector3(0, chargeBarHeight, 1);
        foreach (var c in chargeIndicator.GetComponents<Collider2D>()) Destroy(c);
        var sr = chargeIndicator.AddComponent<SpriteRenderer>();
        sr.sprite = chargeBarSprite != null ? chargeBarSprite : RuntimeSprite.Get();
        sr.color = chargeBarChargingColor;
        sr.sortingOrder = 20;
        chargeIndicator.SetActive(false);

        chargeGlowLeft = CreateChargeGlowEdge("ChargeGlowL");
        chargeGlowRight = CreateChargeGlowEdge("ChargeGlowR");
    }

    private GameObject CreateChargeGlowEdge(string name)
    {
        var glow = new GameObject(name);
        glow.tag = "Player";
        glow.transform.SetParent(transform);
        glow.transform.localPosition = Vector3.zero;
        glow.transform.localScale = Vector3.zero;
        foreach (var c in glow.GetComponents<Collider2D>()) Destroy(c);
        var sr = glow.AddComponent<SpriteRenderer>();
        sr.sprite = RuntimeSprite.Get();
        sr.color = new Color(chargeBarReadyColor.r, chargeBarReadyColor.g, chargeBarReadyColor.b, 0f);
        sr.sortingOrder = 21;
        glow.SetActive(false);
        return glow;
    }

    private void Update()
    {
        if (isDead) return;
        HandleMovementInput();
        HandleShoot();
        UpdateInteractPrompt();
        HandleInteract();
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        rb.velocity = moveInput * moveSpeed;
    }

    // ── Movement ──

    private void HandleMovementInput()
    {
        moveInput = Vector2.zero;
        if (Input.GetKey(KeyCode.W)) moveInput.y += 1;
        if (Input.GetKey(KeyCode.S)) moveInput.y -= 1;
        if (Input.GetKey(KeyCode.A)) moveInput.x -= 1;
        if (Input.GetKey(KeyCode.D)) moveInput.x += 1;
        moveInput = moveInput.normalized;

        if (moveInput != Vector2.zero)
            facingDirection = moveInput.normalized;

        if (animator != null)
            animator.SetBool(IsRunning, moveInput != Vector2.zero);
    }

    // ── Shooting ──

    private void HandleShoot()
    {
        bool overUI = UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        if (overUI)
        {
            if (mouseIsDown)
            {
                mouseIsDown = false;
                chargeIndicator.SetActive(false);
                SetChargeGlowActive(false);
            }
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            mouseDownTime = Time.time;
            mouseIsDown = true;
        }

        if (mouseIsDown)
        {
            float held = Time.time - mouseDownTime;
            float ratio = Mathf.Clamp01(held / holdThreshold);
            chargeIndicator.SetActive(true);
            chargeIndicator.transform.localScale = new Vector3(ratio * chargeBarWidth, chargeBarHeight, 1);
            chargeIndicator.GetComponent<SpriteRenderer>().color =
                ratio >= 1f ? chargeBarReadyColor : chargeBarChargingColor;

            UpdateChargeGlow(ratio >= 1f);
        }

        if (Input.GetMouseButtonUp(0) && mouseIsDown)
        {
            mouseIsDown = false;
            chargeIndicator.SetActive(false);
            SetChargeGlowActive(false);

            float held = Time.time - mouseDownTime;
            BulletType type = held >= holdThreshold ? BulletType.Line : BulletType.Dot;
            Debug.Log($"[Shoot] Fire! type={type}, held={held:F2}s");
            FireBullet(type);
        }
    }

    private void FireBullet(BulletType type)
    {
        if (gameCamera == null)
        {
            Debug.LogError("[Shoot] gameCamera is null! Cannot fire.");
            return;
        }

        if (animator != null)
            animator.SetTrigger(ShootTrigger);

        Vector3 mouseScreen = Input.mousePosition;
        mouseScreen.z = Mathf.Abs(gameCamera.transform.position.z);
        Vector3 mouseWorld = gameCamera.ScreenToWorldPoint(mouseScreen);
        Vector2 origin = (Vector2)transform.position + bulletSpawnOffset;
        Vector2 dir = ((Vector2)mouseWorld - origin).normalized;
        Vector2 spawnPos = GetSafeBulletSpawn((Vector2)transform.position, origin, dir);

        GameObject proj;
        if (projectilePrefab != null)
        {
            proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            proj = CreateRuntimeBullet(spawnPos);
        }

        var p = proj.GetComponent<Projectile>();
        if (p != null) p.Launch(dir, type);
    }

    private Vector2 GetSafeBulletSpawn(Vector2 playerCenter, Vector2 desiredOrigin, Vector2 dir)
    {
        float dist = Vector2.Distance(playerCenter, desiredOrigin);
        RaycastHit2D hit = Physics2D.Raycast(playerCenter, dir, dist + 0.1f, ~0);

        if (hit.collider != null && !hit.collider.CompareTag("Player") && !hit.collider.isTrigger)
            return hit.point - dir * bulletWallMargin;

        return desiredOrigin;
    }

    private GameObject CreateRuntimeBullet(Vector2 pos)
    {
        var go = new GameObject("Projectile");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = RuntimeSprite.Get();
        sr.sortingOrder = 5;
        var r = go.AddComponent<Rigidbody2D>();
        r.gravityScale = 0;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;
        go.AddComponent<Projectile>();
        return go;
    }

    private void UpdateChargeGlow(bool charged)
    {
        if (!charged)
        {
            SetChargeGlowActive(false);
            return;
        }

        SetChargeGlowActive(true);

        float glowH = chargeBarHeight * 1.6f;
        float glowW = chargeBarHeight * 0.6f;
        float halfBar = chargeBarWidth / 2f;

        chargeGlowLeft.transform.localPosition = new Vector3(
            chargeBarOffset.x - halfBar - glowW * 0.3f, chargeBarOffset.y, 0);
        chargeGlowRight.transform.localPosition = new Vector3(
            chargeBarOffset.x + halfBar + glowW * 0.3f, chargeBarOffset.y, 0);

        chargeGlowLeft.transform.localScale = new Vector3(glowW, glowH, 1);
        chargeGlowRight.transform.localScale = new Vector3(glowW, glowH, 1);

        float pulse = (Mathf.Sin(Time.time * 8f) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(0.15f, 0.5f, pulse);
        var c = new Color(chargeBarReadyColor.r, chargeBarReadyColor.g, chargeBarReadyColor.b, alpha);

        chargeGlowLeft.GetComponent<SpriteRenderer>().color = c;
        chargeGlowRight.GetComponent<SpriteRenderer>().color = c;
    }

    private void SetChargeGlowActive(bool active)
    {
        if (chargeGlowLeft != null) chargeGlowLeft.SetActive(active);
        if (chargeGlowRight != null) chargeGlowRight.SetActive(active);
    }

    // ── Interaction ──

    private IInteractable FindNearbyInteractable()
    {
        var hits = Physics2D.OverlapCircleAll(
            (Vector2)transform.position + facingDirection * 0.5f,
            interactRange);

        foreach (var hit in hits)
        {
            var interactable = hit.GetComponent<IInteractable>();
            if (interactable != null)
                return interactable;
        }
        return null;
    }

    private void UpdateInteractPrompt()
    {
        bool canInteract = FindNearbyInteractable() != null;
        if (interactPrompt != null && interactPrompt.activeSelf != canInteract)
            interactPrompt.SetActive(canInteract);
    }

    private void HandleInteract()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;

        if (animator != null)
            animator.SetTrigger(InteractTrigger);

        var target = FindNearbyInteractable();
        if (target != null)
            target.Interact(this);
    }

    // ── Life ──

    [Header("Death Effect")]
    [Tooltip("死亡光圈初始半径")]
    [SerializeField] private float deathLightRadius = 1.5f;
    [Tooltip("光圈收缩时长(秒)")]
    [SerializeField] private float deathShrinkDuration = 1f;

    public void Die()
    {
        if (isDead) return;
        Debug.Log("[Player] Die!");
        isDead = true;
        rb.velocity = Vector2.zero;

        var pm = LevelPhaseManager.Instance;
        bool isDark = pm != null && pm.CurrentPhase == LevelPhase.Dark;
        var sr = GetComponent<SpriteRenderer>();

        if (isDark)
        {
            var fx = gameObject.AddComponent<DeathEffect>();
            fx.Play(sr, deathLightRadius, deathShrinkDuration, Respawn);
        }
        else
        {
            var flash = gameObject.AddComponent<DeathFlashEffect>();
            flash.Play(sr, 1f, 2, Respawn);
        }
    }

    public void Respawn()
    {
        isDead = false;

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        var pm = LevelPhaseManager.Instance;
        if (pm != null)
        {
            pm.ResetAllObjects();
            ApplyPhaseSprite(pm.CurrentPhase);
        }

        var spawn = FindObjectOfType<SpawnPoint>();
        if (spawn != null)
            transform.position = spawn.transform.position;
    }

    public void TeleportTo(Vector2 position)
    {
        transform.position = position;
    }
}
