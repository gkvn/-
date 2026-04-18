using UnityEngine;
using UnityEngine.UI;

public class LocalizedText : MonoBehaviour
{
    [TextArea(1, 3)]
    [SerializeField] private string chinese;
    [TextArea(1, 3)]
    [SerializeField] private string english;

    private Text uiText;
    private TextMesh textMesh;

    private void Awake()
    {
        uiText = GetComponent<Text>();
        textMesh = GetComponent<TextMesh>();
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
        string text = lang == GameLanguage.Chinese ? chinese : english;
        if (uiText != null) uiText.text = text;
        if (textMesh != null) textMesh.text = text;
    }

    public void SetTexts(string cn, string en)
    {
        chinese = cn;
        english = en;
        var lm = LanguageManager.Instance;
        Apply(lm != null ? lm.CurrentLanguage : GameLanguage.Chinese);
    }
}
