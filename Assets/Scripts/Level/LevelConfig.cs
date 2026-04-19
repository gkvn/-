using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AVG;

/// <summary>
/// 每关放置一份：画布图标 + AVG 章节序号。
/// 根据序号自动拼接 chapterId：level{N}_start / level{N}_mid / level{N}_end。
/// </summary>
[DefaultExecutionOrder(100)]
public class LevelConfig : MonoBehaviour
{
    [Header("画布配置")]
    [Tooltip("是否显示左半边画图板（关闭则本关无画图板，游戏画面全屏）")]
    [SerializeField] private bool showDrawingCanvas = true;

    [Tooltip("是否在工具栏显示标记图标")]
    [SerializeField] private bool showIcons = true;


    [Header("AVG 章节序号")]
    [Tooltip("仅在直接运行单个场景（无 LevelManager）时使用的回退序号，-1 表示不播放 AVG。从 MainMenu 启动时自动使用 LevelManager 中的关卡索引。")]
    [SerializeField] private int avgLevelIndexOverride = -1;

    [Header("AVG 与操作")]
    [Tooltip("播 AVG 期间禁用 PlayerController，结束后恢复")]
    [SerializeField] private bool blockPlayerDuringAvg = true;

    [Tooltip("等待 AvgController 出现在场景中的最长时间（秒）")]
    [SerializeField] private float avgControllerWaitTimeout = 15f;

    [Tooltip("为 true 时：除 ChapterPlaybackEnded 外，还在「本章最后一句展示完毕」时结束阻塞（不必再点一次下一句）。建议开启，避免卡在 WAITING。")]
    [SerializeField] private bool completeOnFinalLineReveal = true;

    /// <summary>正在等待当前 AVG 章节播完（ChapterPlaybackEnded）时为 true，用于防止重复触发出口等。</summary>
    public static bool IsAvgFlowBlocking { get; private set; }

    public bool ShowDrawingCanvas => showDrawingCanvas;
    public bool ShowIcons => showIcons;

    private const string ICON_RESOURCE_PATH = "Icon/";
    private const int ICON_COUNT = 5;

    private List<Sprite> _cachedIcons;

    public List<Sprite> AvailableIcons
    {
        get
        {
            if (_cachedIcons == null)
            {
                _cachedIcons = new List<Sprite>(ICON_COUNT);
                for (int i = 1; i <= ICON_COUNT; i++)
                {
                    var sprite = Resources.Load<Sprite>($"{ICON_RESOURCE_PATH}{i}");
                    if (sprite != null)
                        _cachedIcons.Add(sprite);
                    else
                        Debug.LogWarning($"[LevelConfig] 未找到图标资源: {ICON_RESOURCE_PATH}{i}");
                }
            }
            return _cachedIcons;
        }
    }

    /// <summary>
    /// 有 LevelManager 且已开始游戏时，使用其 CurrentLevelIndex；否则回退到 Inspector 配置的 avgLevelIndexOverride。
    /// </summary>
    private int EffectiveLevelIndex
    {
        get
        {
            if (LevelManager.Instance != null && LevelManager.Instance.CurrentLevelIndex >= 0)
                return LevelManager.Instance.CurrentLevelIndex;
            return avgLevelIndexOverride;
        }
    }

    public bool HasAvg => EffectiveLevelIndex >= 0;
    public string AvgChapterOnLevelStart => HasAvg ? $"level{EffectiveLevelIndex}_start" : "";
    public string AvgChapterBeforeNight => HasAvg ? $"level{EffectiveLevelIndex}_mid" : "";
    public string AvgChapterOnLevelComplete => HasAvg ? $"level{EffectiveLevelIndex}_end" : "";

    private const string AVG_CONTROLLER_PREFAB_PATH = "Prefabs/avg_controller";

    private void Start()
    {
        if (!HasAvg)
            return;
        SpawnAvgController();
        StartCoroutine(RunIntroAvgWhenReady());
    }

    private void SpawnAvgController()
    {
        if (AvgController.Instance != null)
            return;

        var prefab = Resources.Load<GameObject>(AVG_CONTROLLER_PREFAB_PATH);
        if (prefab == null)
        {
            Debug.LogError($"[LevelConfig] 无法加载 AVG 预制体: {AVG_CONTROLLER_PREFAB_PATH}");
            return;
        }

        var levelUI = FindObjectOfType<LevelUI>();
        Transform parent = levelUI != null ? levelUI.transform : null;
        var go = Instantiate(prefab, parent);
        go.name = "avg_controller";
        go.transform.SetAsLastSibling();
    }

    private IEnumerator RunIntroAvgWhenReady()
    {
        yield return WaitForAvgControllerAsync();
        if (AvgController.Instance == null)
        {
            HideLevelUIOverlay();
            yield break;
        }
        yield return null;
        RunAvgChapterIfConfigured(AvgChapterOnLevelStart, HideLevelUIOverlay);
    }

    private void HideLevelUIOverlay()
    {
        var levelUI = FindObjectOfType<LevelUI>();
        if (levelUI != null)
            levelUI.HideBlackOverlay();
    }

    /// <summary>
    /// chapterId 为空或仅空白则直接调用 onComplete；否则播放 AVG，在章节可继续关卡时调用 onComplete。
    /// 完成条件：ChapterPlaybackEnded（点「下一句」确认无后续）或（可选）FinalDialogLineRevealComplete（本章最后一句已展示完毕）。
    /// </summary>
    public void RunAvgChapterIfConfigured(string chapterId, Action onComplete)
    {
        if (onComplete == null)
            onComplete = () => { };

        if (string.IsNullOrWhiteSpace(chapterId))
        {
            onComplete();
            return;
        }

        StartCoroutine(RunAvgChapterIfConfiguredAsync(chapterId.Trim(), onComplete));
    }

    private IEnumerator RunAvgChapterIfConfiguredAsync(string chapterId, Action onComplete)
    {
        yield return WaitForAvgControllerAsync();
        var avg = AvgController.Instance;
        if (avg == null)
        {
            Debug.LogWarning("[LevelConfig] 场景中无 AvgController（等待超时），跳过 AVG。");
            onComplete();
            yield break;
        }

        yield return null;

        PlayerController player = null;
        bool blockedPlayer = false;
        if (blockPlayerDuringAvg)
        {
            player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                player.enabled = false;
                blockedPlayer = true;
            }
        }

        IsAvgFlowBlocking = true;

        bool completed = false;
        Action<string> endedHandler = null;
        Action<string, string> finalHandler = null;

        void TryComplete(string reason)
        {
            if (completed)
                return;
            completed = true;

            if (avg != null)
            {
                if (endedHandler != null)
                    avg.ChapterPlaybackEnded -= endedHandler;
                if (finalHandler != null && completeOnFinalLineReveal)
                    avg.FinalDialogLineRevealComplete -= finalHandler;
            }

            IsAvgFlowBlocking = false;
            if (blockedPlayer && player != null)
                player.enabled = true;
            onComplete();
        }

        endedHandler = _ => TryComplete("ChapterPlaybackEnded");
        finalHandler = (_, __) => TryComplete("FinalDialogLineRevealComplete");

        avg.ChapterPlaybackEnded += endedHandler;
        if (completeOnFinalLineReveal)
            avg.FinalDialogLineRevealComplete += finalHandler;

        if (!avg.TryStartChapter(chapterId))
        {
            if (avg != null)
            {
                avg.ChapterPlaybackEnded -= endedHandler;
                if (completeOnFinalLineReveal)
                    avg.FinalDialogLineRevealComplete -= finalHandler;
            }
            IsAvgFlowBlocking = false;
            if (blockedPlayer && player != null)
                player.enabled = true;
            onComplete();
        }
    }

    private IEnumerator WaitForAvgControllerAsync()
    {
        float t = 0f;
        while (AvgController.Instance == null && t < avgControllerWaitTimeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}
