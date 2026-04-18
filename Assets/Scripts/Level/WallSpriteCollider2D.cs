using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 将 <see cref="BoxCollider2D"/> 的 Size/Offset 与 <see cref="SpriteRenderer"/> 实际显示一致（含 Flip、平铺/切片）。
/// 贴图「高度」对应的局部轴向尺寸始终由 Sprite 驱动，无法在 Inspector 中通过改碰撞体来单独扭曲该方向。
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class WallSpriteCollider2D : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;
    private BoxCollider2D _box;

    private void Awake() => Cache();
    private void OnEnable() => Apply();
    private void Reset() => Apply();
    private void OnValidate() => Apply();

    private void LateUpdate() => Apply();

    /// <summary>
    /// 供编辑器在烘焙 NavMesh 等步骤前调用：同帧内立即对齐碰撞体，避免仅依赖 LateUpdate 导致烘焙读到旧尺寸。
    /// </summary>
    public void SyncColliderNow()
    {
        Cache();
        Apply();
    }

    private void Cache()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _box = GetComponent<BoxCollider2D>();
    }

    private void Apply()
    {
        Cache();
        if (_spriteRenderer == null || _box == null || _spriteRenderer.sprite == null)
            return;

        Bounds lb = _spriteRenderer.localBounds;
        var targetSize = new Vector2(lb.size.x, lb.size.y);
        var targetOffset = new Vector2(lb.center.x, lb.center.y);

        if (Approximately(_box.size, targetSize) && Approximately(_box.offset, targetOffset))
            return;

        _box.size = targetSize;
        _box.offset = targetOffset;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(_box);
            EditorUtility.SetDirty(this);
        }
#endif
    }

    private static bool Approximately(Vector2 a, Vector2 b) =>
        Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);
}
