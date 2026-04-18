using UnityEngine;

[RequireComponent(typeof(Camera))]
public class TopDownCamera : MonoBehaviour
{
    [Header("镜头设置")]
    [Tooltip("正交大小，值越大看到的范围越广")]
    [SerializeField] private float cameraSize = 8f;

    [Tooltip("亮灯阶段背景色")]
    [SerializeField] private Color lightBackground = new Color(0.15f, 0.18f, 0.15f, 1f);

    [Tooltip("黑灯阶段背景色")]
    [SerializeField] private Color darkBackground = Color.black;

    private Camera cam;

    public Camera Cam => cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        cam.rect = new Rect(0.5f, 0f, 0.5f, 1f);
        cam.backgroundColor = lightBackground;
    }

    public void SetDarkMode(bool dark)
    {
        if (cam != null)
            cam.backgroundColor = dark ? darkBackground : lightBackground;
    }
}
