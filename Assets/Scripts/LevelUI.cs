using UnityEngine;
using UnityEngine.UI;

public class LevelUI : MonoBehaviour
{
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button finishButton;
    [SerializeField] private Text phaseText;

    private GameObject blackOverlay;

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

        var pm = LevelPhaseManager.Instance;
        if (pm != null)
            pm.OnPhaseChanged += UpdatePhaseText;

        UpdatePhaseText(LevelPhase.Light);
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

    private void OnDestroy()
    {
        var pm = LevelPhaseManager.Instance;
        if (pm != null)
            pm.OnPhaseChanged -= UpdatePhaseText;
    }

    private void UpdatePhaseText(LevelPhase phase)
    {
        if (phaseText == null) return;
        var lm = LanguageManager.Instance;
        if (phase == LevelPhase.Light)
            phaseText.text = lm != null ? lm.Pick("阶段一：亮灯探索", "Phase 1: Lights On") : "阶段一：亮灯探索";
        else
            phaseText.text = lm != null ? lm.Pick("阶段二：黑灯挑战", "Phase 2: Lights Off") : "阶段二：黑灯挑战";
    }

    public void ShowLevelComplete()
    {
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(true);

        bool isLast = LevelManager.Instance != null && LevelManager.Instance.IsLastLevel;
        if (nextLevelButton != null)
            nextLevelButton.gameObject.SetActive(!isLast);
        if (finishButton != null)
            finishButton.gameObject.SetActive(isLast);
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
