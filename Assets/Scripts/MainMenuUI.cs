using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("背景")]
    [SerializeField] private Image background;

    [Header("语言切换")]
    [Tooltip("语言切换按钮上的文字")]
    [SerializeField] private Text languageButtonText;

    private void Start()
    {
        UpdateLanguageButtonText();
        var lm = LanguageManager.Instance;
        if (lm != null)
            lm.OnLanguageChanged += OnLanguageChanged;
    }

    private void OnDestroy()
    {
        var lm = LanguageManager.Instance;
        if (lm != null)
            lm.OnLanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(GameLanguage lang)
    {
        UpdateLanguageButtonText();
    }

    private void UpdateLanguageButtonText()
    {
        if (languageButtonText == null) return;
        var lm = LanguageManager.Instance;
        if (lm == null) return;
        languageButtonText.text = lm.CurrentLanguage == GameLanguage.Chinese
            ? "中文 / English" : "English / 中文";
    }

    public void SetBackgroundSprite(Sprite sprite)
    {
        if (background != null)
            background.sprite = sprite;
    }

    public void OnStartGame()
    {
        LevelManager.Instance.StartGame();
    }

    public void OnToggleLanguage()
    {
        var lm = LanguageManager.Instance;
        if (lm != null)
            lm.ToggleLanguage();
    }
}
