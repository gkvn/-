using UnityEngine;
using UnityEngine.UI;

public class LocalizedImage : MonoBehaviour
{
    [Tooltip("中文贴图")]
    [SerializeField] private Sprite chineseSprite;
    [Tooltip("英文贴图")]
    [SerializeField] private Sprite englishSprite;

    private SpriteRenderer spriteRenderer;
    private Image uiImage;

    public void Initialize(Sprite chinese, Sprite english)
    {
        chineseSprite = chinese;
        englishSprite = english;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        uiImage = GetComponent<Image>();
    }

    private void Start()
    {
        var lm = LanguageManager.Instance;
        if (lm != null)
        {
            lm.OnLanguageChanged += OnLanguageChanged;
            Apply(lm.CurrentLanguage);
        }
        else
        {
            Apply(GameLanguage.Chinese);
        }
    }

    private void OnDestroy()
    {
        var lm = LanguageManager.Instance;
        if (lm != null)
            lm.OnLanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(GameLanguage lang)
    {
        Apply(lang);
    }

    private void Apply(GameLanguage lang)
    {
        Sprite spr = lang == GameLanguage.Chinese ? chineseSprite : englishSprite;
        if (spr == null) spr = chineseSprite != null ? chineseSprite : englishSprite;
        if (spr == null) return;

        if (spriteRenderer != null)
            spriteRenderer.sprite = spr;

        if (uiImage != null)
        {
            uiImage.sprite = spr;
            if (uiImage.color.a < 0.01f)
                uiImage.color = Color.white;
            uiImage.SetNativeSize();
        }
    }
}
