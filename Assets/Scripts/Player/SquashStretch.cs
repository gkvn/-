using UnityEngine;

public class SquashStretch : MonoBehaviour
{
    [Tooltip("拉伸幅度(开始移动时)")]
    [SerializeField] private float stretchAmount = 0.15f;
    [Tooltip("压扁幅度(停下时)")]
    [SerializeField] private float squashAmount = 0.15f;
    [Tooltip("回弹速度")]
    [SerializeField] private float returnSpeed = 8f;

    private Vector3 baseScale = Vector3.one;
    private Vector3 targetScale = Vector3.one;
    private bool wasMoving;

    private void Start()
    {
        baseScale = transform.localScale;
        targetScale = baseScale;
    }

    public void Tick(bool isMoving, Vector2 moveDir)
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
        }
        else if (!isMoving && wasMoving)
        {
            targetScale = new Vector3(
                baseScale.x * (1f - squashAmount),
                baseScale.y * (1f + squashAmount),
                baseScale.z);
        }

        wasMoving = isMoving;
        transform.localScale = Vector3.Lerp(transform.localScale, baseScale, Time.deltaTime * returnSpeed);

        if (targetScale != baseScale)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * returnSpeed * 2f);
            if (Vector3.Distance(transform.localScale, baseScale) < 0.01f)
                targetScale = baseScale;
        }
    }
}
