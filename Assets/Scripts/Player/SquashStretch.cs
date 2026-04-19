using UnityEngine;

public class SquashStretch : MonoBehaviour
{
    [Tooltip("拉伸幅度(开始移动时)")]
    [SerializeField] private float stretchAmount = 0.15f;
    [Tooltip("压扁幅度(停下时)")]
    [SerializeField] private float squashAmount = 0.15f;
    [Tooltip("回弹速度")]
    [SerializeField] private float returnSpeed = 8f;

    [Header("Shoot Punch")]
    [Tooltip("射击时压缩幅度")]
    [SerializeField] private float shootPunchAmount = 0.2f;
    [Tooltip("射击弹回速度")]
    [SerializeField] private float shootPunchSpeed = 16f;

    private Vector3 baseScale = Vector3.one;
    private Vector3 targetScale = Vector3.one;
    private bool wasMoving;
    private bool punching;
    private bool reachedTarget;

    private void Start()
    {
        baseScale = transform.localScale;
        targetScale = baseScale;
    }

    public void Tick(bool isMoving, Vector2 moveDir)
    {
        if (!punching)
        {
            if (isMoving && !wasMoving)
            {
                float sx = Mathf.Abs(moveDir.x) > Mathf.Abs(moveDir.y)
                    ? baseScale.x * (1f + stretchAmount)
                    : baseScale.x * (1f - stretchAmount);
                float sy = Mathf.Abs(moveDir.y) >= Mathf.Abs(moveDir.x)
                    ? baseScale.y * (1f + stretchAmount)
                    : baseScale.y * (1f - stretchAmount);
                targetScale = new Vector3(sx, sy, baseScale.z);
                reachedTarget = false;
            }
            else if (!isMoving && wasMoving)
            {
                targetScale = new Vector3(
                    baseScale.x * (1f - squashAmount),
                    baseScale.y * (1f + squashAmount),
                    baseScale.z);
                reachedTarget = false;
            }
        }

        wasMoving = isMoving;

        float speed = punching ? shootPunchSpeed : returnSpeed;

        if (!reachedTarget && targetScale != baseScale)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed * 2f);
            if (Vector3.Distance(transform.localScale, targetScale) < 0.01f)
                reachedTarget = true;
        }
        else
        {
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale, Time.deltaTime * speed);
            if (Vector3.Distance(transform.localScale, baseScale) < 0.005f)
            {
                transform.localScale = baseScale;
                targetScale = baseScale;
                punching = false;
            }
        }
    }

    public void ShootPunch(Vector2 shootDir)
    {
        float dx = Mathf.Abs(shootDir.x) >= Mathf.Abs(shootDir.y)
            ? baseScale.x * (1f - shootPunchAmount)
            : baseScale.x * (1f + shootPunchAmount * 0.5f);
        float dy = Mathf.Abs(shootDir.y) > Mathf.Abs(shootDir.x)
            ? baseScale.y * (1f - shootPunchAmount)
            : baseScale.y * (1f + shootPunchAmount * 0.5f);
        targetScale = new Vector3(dx, dy, baseScale.z);
        punching = true;
        reachedTarget = false;
    }
}
