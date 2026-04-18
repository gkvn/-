using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Combat")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float shootCooldown = 0.3f;

    [Header("Interaction")]
    [SerializeField] private float interactRange = 1.2f;
    [SerializeField] private Vector2 promptOffset = new Vector2(0, 1.2f);

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;
    private Vector2 facingDirection = Vector2.down;
    private float shootTimer;
    private bool isDead;
    private GameObject interactPrompt;

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
        CreateInteractPrompt();
    }

    private void CreateInteractPrompt()
    {
        interactPrompt = new GameObject("InteractPrompt");
        interactPrompt.transform.SetParent(transform);
        interactPrompt.transform.localPosition = promptOffset;
        interactPrompt.transform.localScale = Vector3.one * 0.4f;

        var sr = interactPrompt.AddComponent<SpriteRenderer>();
        sr.color = Color.white;
        sr.sortingOrder = 20;

        var playerSR = GetComponent<SpriteRenderer>();
        if (playerSR != null && playerSR.sprite != null)
            sr.sprite = playerSR.sprite;

        interactPrompt.SetActive(false);
    }

    private void Update()
    {
        if (isDead) return;
        HandleMovementInput();
        HandleShoot();
        UpdateInteractPrompt();
        HandleInteract();
        shootTimer -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        rb.velocity = moveInput * moveSpeed;
    }

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

    private void HandleShoot()
    {
        if (Input.GetKeyDown(KeyCode.Space) && shootTimer <= 0f)
        {
            shootTimer = shootCooldown;
            if (animator != null)
                animator.SetTrigger(ShootTrigger);
            if (projectilePrefab != null)
            {
                var proj = Instantiate(projectilePrefab,
                    (Vector2)transform.position + facingDirection * 0.5f,
                    Quaternion.identity);
                var p = proj.GetComponent<Projectile>();
                if (p != null) p.Launch(facingDirection);
            }
        }
    }

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

    private void Start()
    {
        var col = GetComponent<Collider2D>();
        Debug.Log($"[Player] Start — tag={gameObject.tag}, collider={col != null}, isTrigger={col?.isTrigger}, bounds={col?.bounds}, rb={rb != null}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[Player] OnTriggerEnter2D — other={other.name}, otherIsTrigger={other.isTrigger}");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"[Player] OnCollisionEnter2D — other={collision.collider.name}");
    }

    public void Die()
    {
        if (isDead) return;
        Debug.Log("[Player] Die() called!");
        isDead = true;
        rb.velocity = Vector2.zero;
        Invoke(nameof(Respawn), 0.5f);
    }

    public void Respawn()
    {
        Debug.Log("[Player] Respawn() called!");
        isDead = false;
        var spawn = FindObjectOfType<SpawnPoint>();
        if (spawn != null)
            transform.position = spawn.transform.position;
    }

    public void TeleportTo(Vector2 position)
    {
        transform.position = position;
    }
}
