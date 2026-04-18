using UnityEngine;
using System;

public enum GameLanguage { Chinese, English }

public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance { get; private set; }

    public GameLanguage CurrentLanguage { get; private set; } = GameLanguage.Chinese;

    public event Action<GameLanguage> OnLanguageChanged;

    private const string PrefKey = "GameLanguage";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        string saved = PlayerPrefs.GetString(PrefKey, "Chinese");
        CurrentLanguage = saved == "English" ? GameLanguage.English : GameLanguage.Chinese;
    }

    public void SetLanguage(GameLanguage lang)
    {
        if (CurrentLanguage == lang) return;
        CurrentLanguage = lang;
        PlayerPrefs.SetString(PrefKey, lang.ToString());
        PlayerPrefs.Save();
        OnLanguageChanged?.Invoke(lang);
    }

    public void ToggleLanguage()
    {
        SetLanguage(CurrentLanguage == GameLanguage.Chinese
            ? GameLanguage.English : GameLanguage.Chinese);
    }

    public string Pick(string chinese, string english)
    {
        return CurrentLanguage == GameLanguage.Chinese ? chinese : english;
    }
}
