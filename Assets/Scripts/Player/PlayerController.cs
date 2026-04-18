using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
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

    [Header("Interaction")]
    [SerializeField] private float interactRange = 1.2f;
    [SerializeField] private Vector2 promptOffset = new Vector2(0, 1.2f);

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;
    private Vector2 facingDirection = Vector2.down;
    private bool isDead;

    private Camera gameCamera;
    private float mouseDownTime;
    private bool mouseIsDown;
    private bool chargeReadyFired;
    private GameObject interactPrompt;
    private GameObject chargeIndicator;

    private static readonly int IsRunning = Animator.StringToHash("isRunning");
    private static readonly int ShootTrigger = Animator.StringToHash("isShooting");
    private static readonly int InteractTrigger = Animator.StringToHash("isInteracting");

    public Vector2 FacingDirection => facingDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        var tdc = FindObjectOfType<TopDownCamera>();
        gameCamera = tdc != null ? tdc.Cam : Camera.main;
        if (gameCamera == null) gameCamera = FindObjectOfType<Camera>();
        Debug.Log($"[Player] gameCamera={gameCamera != null}, projectilePrefab={projectilePrefab != null}");

        CreateInteractPrompt();
        CreateChargeIndicator();
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
        interactPrompt.transform.localScale = Vector3.one * 0.4f;
        foreach (var c in interactPrompt.GetComponents<Collider2D>()) Destroy(c);
        var sr = interactPrompt.AddComponent<SpriteRenderer>();
        sr.sprite = GetSpriteOrFallback();
        sr.color = Color.white;
        sr.sortingOrder = 20;
        interactPrompt.SetActive(false);
    }

    private void CreateChargeIndicator()
    {
        chargeIndicator = new GameObject("ChargeIndicator");
        chargeIndicator.tag = "Player";
        chargeIndicator.transform.SetParent(transform);
        chargeIndicator.transform.localPosition = new Vector3(0, -0.8f, 0);
        chargeIndicator.transform.localScale = new Vector3(0, 0.1f, 1);
        foreach (var c in chargeIndicator.GetComponents<Collider2D>()) Destroy(c);
        var sr = chargeIndicator.AddComponent<SpriteRenderer>();
        sr.sprite = GetSpriteOrFallback();
        sr.color = new Color(0.4f, 0.8f, 1f);
        sr.sortingOrder = 20;
        chargeIndicator.SetActive(false);
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
            }
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            mouseDownTime = Time.time;
            mouseIsDown = true;
            chargeReadyFired = false;
        }

        if (mouseIsDown)
        {
            float held = Time.time - mouseDownTime;
            float ratio = Mathf.Clamp01(held / holdThreshold);
            chargeIndicator.SetActive(true);
            chargeIndicator.transform.localScale = new Vector3(ratio * 0.8f, 0.1f, 1);
            chargeIndicator.GetComponent<SpriteRenderer>().color =
                ratio >= 1f ? new Color(0.4f, 0.8f, 1f) : Color.yellow;

            if (ratio >= 1f && !chargeReadyFired)
            {
                chargeReadyFired = true;
                SpawnChargeBurst();
            }
        }

        if (Input.GetMouseButtonUp(0) && mouseIsDown)
        {
            mouseIsDown = false;
            chargeIndicator.SetActive(false);

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
        Vector2 spawnPos = origin;

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

    private void SpawnChargeBurst()
    {
        var burst = new GameObject("ChargeBurst");
        burst.transform.SetParent(transform);
        burst.transform.localPosition = new Vector3(0.5f, -0.8f, 0);
        var sr = burst.AddComponent<SpriteRenderer>();
        sr.sprite = RuntimeSprite.Get();
        sr.color = new Color(0.4f, 0.8f, 1f, 0.9f);
        sr.sortingOrder = 21;
        burst.transform.localScale = Vector3.one * 0.1f;
        burst.AddComponent<ChargeBurstEffect>();
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

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.white;

        var pm = LevelPhaseManager.Instance;
        if (pm != null)
            pm.ResetAllObjects();

        var spawn = FindObjectOfType<SpawnPoint>();
        if (spawn != null)
            transform.position = spawn.transform.position;
    }

    public void TeleportTo(Vector2 position)
    {
        transform.position = position;
    }
}
