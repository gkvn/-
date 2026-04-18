using UnityEngine;

[RequireComponent(typeof(Camera))]
public class TopDownCamera : MonoBehaviour
{
    [Header("镜头设置")]
    [Tooltip("正交大小，值越大看到的范围越广")]
    [SerializeField] private float cameraSize = 8f;

    [Header("背景色")]
    [Tooltip("亮灯阶段背景色")]
    [SerializeField] private Color lightBackground = new Color(0.15f, 0.18f, 0.15f, 1f);
    [Tooltip("黑灯阶段背景色")]
    [SerializeField] private Color darkBackground = Color.black;

    [Header("背景图")]
    [Tooltip("亮灯阶段背景图（留空则只用背景色）")]
    [SerializeField] private Sprite lightBackgroundImage;
    [Tooltip("黑灯阶段背景图（留空则只用背景色）")]
    [SerializeField] private Sprite darkBackgroundImage;
    [Tooltip("背景图位置偏移")]
    [SerializeField] private Vector2 backgroundOffset = Vector2.zero;
    [Tooltip("背景图缩放")]
    [SerializeField] private Vector2 backgroundScale = Vector2.one;

    private Camera cam;
    private SpriteRenderer bgRenderer;

    public Camera Cam => cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        cam.rect = new Rect(0.5f, 0f, 0.5f, 1f);
        cam.backgroundColor = lightBackground;

        CreateBackgroundObject();
    }

    private void CreateBackgroundObject()
    {
        if (lightBackgroundImage == null && darkBackgroundImage == null) return;

        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(transform);
        bgGo.transform.localPosition = new Vector3(backgroundOffset.x, backgroundOffset.y, 10f);
        bgGo.transform.localScale = new Vector3(backgroundScale.x, backgroundScale.y, 1f);

        bgRenderer = bgGo.AddComponent<SpriteRenderer>();
        bgRenderer.sortingOrder = -100;
        bgRenderer.sprite = lightBackgroundImage;
    }

    public void SetDarkMode(bool dark)
    {
        if (cam != null)
            cam.backgroundColor = dark ? darkBackground : lightBackground;

        if (bgRenderer != null)
        {
            Sprite target = dark ? darkBackgroundImage : lightBackgroundImage;
            bgRenderer.sprite = target;
            bgRenderer.enabled = target != null;
        }
    }
}
