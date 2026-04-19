using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AVG;

/// <summary>
/// 每关放置一份：画布图标 + 三处可选 AVG 章节（与 ImportedAVG Resources/Json/Chapters 下的 chapter id 一致）。
/// </summary>
[DefaultExecutionOrder(100)]
public class LevelConfig : MonoBehaviour
{
    [Header("画布配置")]
    [Tooltip("是否显示左半边画图板（关闭则本关无画图板，游戏画面全屏）")]
    [SerializeField] private bool showDrawingCanvas = true;

    [Tooltip("本关卡可用的标记图标")]
    [SerializeField] private List<Sprite> availableIcons = new List<Sprite>();

    [Header("AVG（chapterId；留空则不播放）")]
    [Tooltip("关卡场景加载后、流程开始前播放")]
    [SerializeField] private string avgChapterOnLevelStart;
    [Tooltip("玩家在白天阶段到达出口、即将切换黑夜前播放")]
    [SerializeField] private string avgChapterBeforeNight;
    [Tooltip("玩家在黑夜阶段到达出口、即将显示通关 UI 前播放")]
    [SerializeField] private string avgChapterOnLevelComplete;

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
    public List<Sprite> AvailableIcons => availableIcons;
    public string AvgChapterOnLevelStart => avgChapterOnLevelStart;
    public string AvgChapterBeforeNight => avgChapterBeforeNight;
    public string AvgChapterOnLevelComplete => avgChapterOnLevelComplete;

    private void Start()
    {
        if (string.IsNullOrWhiteSpace(avgChapterOnLevelStart))
            return;
        StartCoroutine(RunIntroAvgWhenReady());
    }

    private IEnumerator RunIntroAvgWhenReady()
    {
        yield return WaitForAvgControllerAsync();
        if (AvgController.Instance == null)
            yield break;
        yield return null;
        RunAvgChapterIfConfigured(avgChapterOnLevelStart, null);
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
