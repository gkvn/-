using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("背景")]
    [SerializeField] private Image background;

    [Header("语言切换")]
    [Tooltip("语言切换按钮上的文字")]
    [SerializeField] private Text languageButtonText;

    [Header("图片面板")]
    [Tooltip("从 Resources/Prefabs/ImagePanel 自动加载，也可手动指定")]
    [SerializeField] private GameObject imagePanelPrefab;

    private GameObject imagePanelInstance;
    private GameObject imagePanel;

    private void Start()
    {
        UpdateLanguageButtonText();
        var lm = LanguageManager.Instance;
        if (lm != null)
            lm.OnLanguageChanged += OnLanguageChanged;

        SetupImagePanel();
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

    public void OnShowImagePanel()
    {
        if (imagePanel != null)
            imagePanel.SetActive(true);
    }

    public void OnHideImagePanel()
    {
        if (imagePanel != null)
            imagePanel.SetActive(false);
    }

    private void SetupImagePanel()
    {
        var prefab = imagePanelPrefab;
        if (prefab == null)
            prefab = Resources.Load<GameObject>("Prefabs/ImagePanel");

        if (prefab == null)
        {
            Debug.LogWarning("ImagePanel prefab 未找到，请先通过 Tools/创建 ImagePanel Prefab 生成");
            return;
        }

        var canvas = GetComponent<Canvas>() != null ? transform : transform.root;
        imagePanelInstance = Instantiate(prefab, canvas);

        var showBtn = imagePanelInstance.transform.Find("ShowButton");
        if (showBtn != null)
            showBtn.GetComponent<Button>().onClick.AddListener(OnShowImagePanel);

        imagePanel = imagePanelInstance.transform.Find("Panel")?.gameObject;
        if (imagePanel != null)
        {
            var backBtn = imagePanel.transform.Find("BackButton");
            if (backBtn != null)
                backBtn.GetComponent<Button>().onClick.AddListener(OnHideImagePanel);

            imagePanel.SetActive(false);
        }
    }
}
