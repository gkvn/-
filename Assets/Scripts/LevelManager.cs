using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("场景配置")]
    [Tooltip("主菜单场景名称")]
    [SerializeField] private string mainMenuScene = "MainMenu";

    [Tooltip("按顺序排列的关卡场景名称")]
    [SerializeField] private List<string> levelScenes = new List<string>();

    private int currentLevelIndex = -1;

    public int CurrentLevelIndex => currentLevelIndex;
    public int TotalLevels => levelScenes.Count;
    public bool IsLastLevel => currentLevelIndex >= levelScenes.Count - 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartGame()
    {
        if (levelScenes.Count == 0)
        {
            Debug.LogWarning("LevelManager: 没有配置任何关卡！");
            return;
        }
        currentLevelIndex = 0;
        SceneManager.LoadScene(levelScenes[0]);
    }

    public void LoadNextLevel()
    {
        if (IsLastLevel)
        {
            ReturnToMainMenu();
            return;
        }
        currentLevelIndex++;
        SceneManager.LoadScene(levelScenes[currentLevelIndex]);
    }

    public void ReturnToMainMenu()
    {
        currentLevelIndex = -1;
        SceneManager.LoadScene(mainMenuScene);
    }
}
