using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class SignPost : MonoBehaviour, IInteractable, IResettable
{
    [Header("显示内容")]
    [Tooltip("弹出界面的背景贴图")]
    [SerializeField] private Sprite popupBackground;
    [Tooltip("中文文字内容")]
    [TextArea(2, 5)]
    [SerializeField] private string displayTextCN = "路牌内容";
    [Tooltip("英文文字内容")]
    [TextArea(2, 5)]
    [SerializeField] private string displayTextEN = "Sign Content";
    [Tooltip("文字大小（世界空间像素）")]
    [SerializeField] private float fontSize = 28f;
    [Tooltip("文字颜色")]
    [SerializeField] private Color textColor = Color.white;

    [Header("弹出设置")]
    [Tooltip("弹出界面相对路牌的偏移")]
    [SerializeField] private Vector2 popupOffset = new Vector2(0, 1.5f);
    [Tooltip("弹出界面宽度")]
    [SerializeField] private float popupWidth = 2.5f;
    [Tooltip("弹出界面高度")]
    [SerializeField] private float popupHeight = 1.2f;
    [Tooltip("显示持续时间(秒)")]
    [SerializeField] private float displayDuration = 3f;

    private GameObject popupInstance;
    private float hideTimer;
    private bool showing;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<BoxCollider2D>();
        col.isTrigger = false;
    }

    private void Start()
    {
        var pm = LevelPhaseManager.Instance;
        if (pm != null)
        {
            pm.OnPhaseChanged += OnPhaseChanged;
            ApplyPhase(pm.CurrentPhase);
        }
    }

    private void OnDestroy()
    {
        var pm = LevelPhaseManager.Instance;
        if (pm != null)
            pm.OnPhaseChanged -= OnPhaseChanged;
    }

    private void OnPhaseChanged(LevelPhase phase)
    {
        ApplyPhase(phase);
    }

    private void ApplyPhase(LevelPhase phase)
    {
        bool visible = phase == LevelPhase.Light;
        if (spriteRenderer != null) spriteRenderer.enabled = visible;
        if (col != null) col.enabled = visible;
        if (!visible) HidePopup();
    }

    public void Interact(PlayerController player)
    {
        ShowPopup();
    }

    private void Update()
    {
        if (!showing) return;
        hideTimer -= Time.deltaTime;
        if (hideTimer <= 0f)
            HidePopup();
    }

    private void ShowPopup()
    {
        if (popupInstance != null)
        {
            hideTimer = displayDuration;
            return;
        }

        popupInstance = new GameObject("SignPopup");
        popupInstance.transform.SetParent(transform);
        popupInstance.transform.localPosition = new Vector3(popupOffset.x, popupOffset.y, 0);

        var bgGo = new GameObject("PopupBg");
        bgGo.transform.SetParent(popupInstance.transform);
        bgGo.transform.localPosition = Vector3.zero;
        bgGo.transform.localScale = new Vector3(popupWidth, popupHeight, 1);
        var bgSr = bgGo.AddComponent<SpriteRenderer>();
        bgSr.sprite = popupBackground != null ? popupBackground : RuntimeSprite.Get();
        bgSr.color = popupBackground != null ? Color.white : new Color(0.1f, 0.1f, 0.1f, 0.85f);
        bgSr.sortingOrder = 30;

        var textGo = new GameObject("PopupText");
        textGo.transform.SetParent(popupInstance.transform);
        textGo.transform.localPosition = new Vector3(0, 0, -0.01f);

        var mesh = textGo.AddComponent<TextMesh>();
        var lm = LanguageManager.Instance;
        mesh.text = lm != null ? lm.Pick(displayTextCN, displayTextEN) : displayTextCN;
        mesh.fontSize = 100;
        mesh.characterSize = fontSize / 100f;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.color = textColor;

        var mr = textGo.GetComponent<MeshRenderer>();
        if (mr != null)
            mr.sortingOrder = 31;

        showing = true;
        hideTimer = displayDuration;
    }

    private void HidePopup()
    {
        if (popupInstance != null)
            Destroy(popupInstance);
        popupInstance = null;
        showing = false;
    }

    public void ResetState()
    {
        HidePopup();
        var pm = LevelPhaseManager.Instance;
        if (pm != null)
            ApplyPhase(pm.CurrentPhase);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.5f);
        Vector3 pos = transform.position + new Vector3(popupOffset.x, popupOffset.y, 0);
        Gizmos.DrawWireCube(pos, new Vector3(popupWidth, popupHeight, 0));
    }
}
