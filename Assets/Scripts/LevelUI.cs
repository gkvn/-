using UnityEngine;
using UnityEngine.UI;

public class LevelUI : MonoBehaviour
{
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button finishButton;
    [SerializeField] private Text phaseText;

    [Header("重开按钮")]
    [Tooltip("重开按钮大小")]
    [SerializeField] private Vector2 restartButtonSize = new Vector2(50, 50);
    [Tooltip("重开按钮位置偏移（相对右上角）")]
    [SerializeField] private Vector2 restartButtonOffset = new Vector2(-30, -30);

    private GameObject blackOverlay;
    private GameObject dynamicCompletePanel;

    private void Awake()
    {
        var cfg = FindObjectOfType<LevelConfig>();
        if (cfg != null && cfg.HasAvg)
            CreateBlackOverlay();
    }

    private void Start()
    {
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);

        if (phaseText != null)
            phaseText.gameObject.SetActive(false);

        CreateRestartButton();

        if (blackOverlay != null)
            blackOverlay.transform.SetAsLastSibling();
    }

    private void CreateBlackOverlay()
    {
        blackOverlay = new GameObject("BlackOverlay");
        blackOverlay.transform.SetParent(transform, false);
        blackOverlay.transform.SetAsLastSibling();

        var img = blackOverlay.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = true;

        var rt = blackOverlay.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public void HideBlackOverlay()
    {
        if (blackOverlay != null)
        {
            Destroy(blackOverlay);
            blackOverlay = null;
        }
    }

    private void CreateRestartButton()
    {
        var go = new GameObject("RestartButton");
        go.transform.SetParent(transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = restartButtonOffset;
        rt.sizeDelta = restartButtonSize;

        var img = go.AddComponent<Image>();
        var sprite = Resources.Load<Sprite>("Art/ui/restart");
        if (sprite != null)
        {
            img.sprite = sprite;
            img.color = Color.white;
        }
        else
        {
            Debug.LogWarning("未找到重开按钮图片: Art/ui/restart");
            img.color = new Color(0.2f, 0.2f, 0.2f, 0.7f);
        }

        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(OnRestartLevel);
    }

    public void OnRestartLevel()
    {
        var pm = LevelPhaseManager.Instance;
        if (pm != null)
            pm.RestartLevel();
    }

    private void OnDestroy()
    {
    }

    public void ShowLevelComplete()
    {
        if (dynamicCompletePanel == null)
        {
            var prefab = Resources.Load<GameObject>("Prefabs/LevelCompletePanel");
            if (prefab != null)
            {
                var canvas = GetComponentInParent<Canvas>();
                var parent = canvas != null ? canvas.transform : transform;

                dynamicCompletePanel = Instantiate(prefab);
                dynamicCompletePanel.transform.SetParent(parent, false);
                dynamicCompletePanel.transform.SetAsLastSibling();

                foreach (var btn in dynamicCompletePanel.GetComponentsInChildren<Button>(true))
                {
                    if (btn.gameObject.name == "NextLevelButton")
                        btn.onClick.AddListener(OnNextLevel);
                    else if (btn.gameObject.name == "FinishButton")
                        btn.onClick.AddListener(OnFinish);
                }
            }
            else
            {
                Debug.LogWarning("LevelCompletePanel prefab 未找到，回退使用场景内版本");
                if (levelCompletePanel != null)
                    levelCompletePanel.SetActive(true);
            }
        }

        if (dynamicCompletePanel != null)
        {
            dynamicCompletePanel.SetActive(true);
            dynamicCompletePanel.transform.SetAsLastSibling();

            bool isLast = LevelManager.Instance != null && LevelManager.Instance.IsLastLevel;
            foreach (var btn in dynamicCompletePanel.GetComponentsInChildren<Button>(true))
            {
                if (btn.gameObject.name == "NextLevelButton")
                    btn.gameObject.SetActive(!isLast);
                else if (btn.gameObject.name == "FinishButton")
                    btn.gameObject.SetActive(isLast);
            }
        }
        else
        {
            bool isLast = LevelManager.Instance != null && LevelManager.Instance.IsLastLevel;
            if (nextLevelButton != null)
                nextLevelButton.gameObject.SetActive(!isLast);
            if (finishButton != null)
                finishButton.gameObject.SetActive(isLast);
        }
    }

    public void OnNextLevel()
    {
        if (LevelManager.Instance != null)
            LevelManager.Instance.LoadNextLevel();
        else
            Debug.Log("LevelManager 不存在，无法切换关卡（Test 场景下正常）");
    }

    public void OnFinish()
    {
        if (LevelManager.Instance != null)
            LevelManager.Instance.ReturnToMainMenu();
        else
            Debug.Log("LevelManager 不存在，无法返回主菜单（Test 场景下正常）");
    }
}
