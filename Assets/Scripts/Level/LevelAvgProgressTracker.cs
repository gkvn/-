using System;
using AVG;
using UnityEngine;

/// <summary>
/// 记录当前关卡 AVG 章节（start / mid / end）对应的游戏里程碑。日志跳回更早对白时，若跳转目标章节早于
/// 「游戏已到达阶段」或「跳转前正在播的章节」，则在本章播完后按顺序接章（如 start→mid→end），每章从新章首句开始；
/// 仅在尚未触发过的里程碑处执行进黑夜 / 通关。播放方式与普通 AVG 一致（不强制快进）。
/// </summary>
public static class LevelAvgProgressTracker
{
    private static AvgController _subscribedTo;

    private static bool _catchUpActive;
    private static int _catchUpTargetSegment;
    private static int _catchUpLevelIndex;

    private static bool _passedStart;
    private static bool _passedMid;
    private static bool _passedEnd;

    private static PlayerController _blockedPlayer;
    private static bool _didBlockPlayer;

    /// <summary>
    /// 新关卡场景开始时调用：清空里程碑与追赶状态，并解除上一场景 AvgController 的事件绑定。
    /// </summary>
    public static void ResetForNewLevel()
    {
        EndCatchUpSilently();
        UnbindChapterEnded();
        _passedStart = false;
        _passedMid = false;
        _passedEnd = false;
    }

    public static void NotifyMidTransitionApplied()
    {
        _passedMid = true;
    }

    public static void NotifyLevelCompleteApplied()
    {
        _passedEnd = true;
    }

    public static void EnsureSubscribed()
    {
        var avg = AvgController.Instance;
        if (avg == null)
            return;

        if (_subscribedTo == avg)
            return;

        UnbindChapterEnded();
        avg.ChapterPlaybackEnded += OnChapterPlaybackEnded;
        _subscribedTo = avg;
    }

    private static void UnbindChapterEnded()
    {
        if (_subscribedTo == null)
            return;
        _subscribedTo.ChapterPlaybackEnded -= OnChapterPlaybackEnded;
        _subscribedTo = null;
    }

    /// <param name="targetChapterId">日志选中的章节（跳转目标）。</param>
    /// <param name="originChapterId">打开日志并点击前，当前正在播的章节（用于亮灯播 mid 时跳回 start 仍要接回整章 mid 等）。</param>
    public static void OnLogJumped(string targetChapterId, string originChapterId)
    {
        EnsureSubscribed();
        if (string.IsNullOrEmpty(targetChapterId))
            return;

        if (!TryParseChapter(targetChapterId, out int levelIdx, out int jumpSegment))
            return;

        int originSegment = -1;
        if (!string.IsNullOrEmpty(originChapterId) &&
            TryParseChapter(originChapterId, out int originLevel, out int os) &&
            originLevel == levelIdx)
        {
            originSegment = os;
        }

        int sync = GetGameSyncSegment();
        int chainEnd = Math.Max(sync, originSegment);

        if (jumpSegment >= chainEnd)
            return;

        _catchUpActive = true;
        _catchUpTargetSegment = chainEnd;
        _catchUpLevelIndex = levelIdx;

        var avg = AvgController.Instance;
        if (avg == null)
        {
            EndCatchUpSilently();
            return;
        }

        LevelConfig.SetAvgFlowBlocking(true);
        TryBlockPlayerForCatchUp();
    }

    private static void OnChapterPlaybackEnded(string chapterId)
    {
        if (!TryParseChapter(chapterId, out int levelIdx, out int endedSegment))
        {
            if (_catchUpActive)
                EndCatchUp();
            return;
        }

        if (!_catchUpActive)
        {
            if (endedSegment == 0)
                _passedStart = true;
            return;
        }

        if (levelIdx != _catchUpLevelIndex)
        {
            EndCatchUp();
            return;
        }

        ApplyCatchUpMilestone(endedSegment);

        var avg = AvgController.Instance;
        if (avg == null)
        {
            EndCatchUp();
            return;
        }

        if (endedSegment < _catchUpTargetSegment)
        {
            string nextChapter = BuildChapterId(_catchUpLevelIndex, endedSegment + 1);
            if (!avg.TryStartChapter(nextChapter))
            {
                Debug.LogError($"[LevelAvgProgressTracker] 追赶模式无法加载章节: {nextChapter}");
                EndCatchUp();
                return;
            }
        }
        else
        {
            EndCatchUp();
        }
    }

    private static void ApplyCatchUpMilestone(int endedSegment)
    {
        if (endedSegment == 0 && !_passedStart)
        {
            HideLevelOverlay();
            _passedStart = true;
        }
        else if (endedSegment == 1 && !_passedMid)
        {
            LevelPhaseManager.Instance?.TransitionToDark();
        }
        else if (endedSegment == 2 && !_passedEnd)
        {
            if (BgmManager.Instance != null)
                BgmManager.Instance.PlayLevelEndBgm();
            LevelPhaseManager.Instance?.OnLevelComplete();
        }
    }

    /// <summary>
    /// 已与游戏阶段同步到的最高章节段：-1 尚未完成 start；0 亮灯；1 黑夜；2 已触发通关。
    /// </summary>
    private static int GetGameSyncSegment()
    {
        if (_passedEnd)
            return 2;
        if (_passedMid)
            return 1;
        if (_passedStart)
            return 0;
        return -1;
    }

    private static void HideLevelOverlay()
    {
        var levelUI = UnityEngine.Object.FindObjectOfType<LevelUI>();
        if (levelUI != null)
            levelUI.HideBlackOverlay();
    }

    private static void TryBlockPlayerForCatchUp()
    {
        var cfg = UnityEngine.Object.FindObjectOfType<LevelConfig>();
        if (cfg == null || !cfg.BlockPlayerDuringAvgForCatchUp())
            return;

        _blockedPlayer = UnityEngine.Object.FindObjectOfType<PlayerController>();
        if (_blockedPlayer != null)
        {
            _blockedPlayer.enabled = false;
            _didBlockPlayer = true;
        }
    }

    private static void EndCatchUp()
    {
        _catchUpActive = false;
        _catchUpTargetSegment = 0;
        _catchUpLevelIndex = 0;

        LevelConfig.SetAvgFlowBlocking(false);

        if (_didBlockPlayer && _blockedPlayer != null)
            _blockedPlayer.enabled = true;
        _didBlockPlayer = false;
        _blockedPlayer = null;
    }

    private static void EndCatchUpSilently()
    {
        _catchUpActive = false;
        _catchUpTargetSegment = 0;
        _catchUpLevelIndex = 0;
        LevelConfig.SetAvgFlowBlocking(false);
        if (_didBlockPlayer && _blockedPlayer != null)
            _blockedPlayer.enabled = true;
        _didBlockPlayer = false;
        _blockedPlayer = null;
    }

    private static bool TryParseChapter(string chapterId, out int levelIndex, out int segmentIndex)
    {
        levelIndex = 0;
        segmentIndex = 0;
        if (string.IsNullOrEmpty(chapterId))
            return false;

        int u = chapterId.LastIndexOf('_');
        if (u <= 0 || u >= chapterId.Length - 1)
            return false;

        string prefix = chapterId.Substring(0, u);
        string suffix = chapterId.Substring(u + 1);
        const string levelPrefix = "level";
        if (!prefix.StartsWith(levelPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        string numPart = prefix.Substring(levelPrefix.Length);
        if (!int.TryParse(numPart, out levelIndex))
            return false;

        segmentIndex = SuffixToSegment(suffix);
        return segmentIndex >= 0;
    }

    private static int SuffixToSegment(string suffix)
    {
        if (string.Equals(suffix, "start", StringComparison.OrdinalIgnoreCase))
            return 0;
        if (string.Equals(suffix, "mid", StringComparison.OrdinalIgnoreCase))
            return 1;
        if (string.Equals(suffix, "end", StringComparison.OrdinalIgnoreCase))
            return 2;
        return -1;
    }

    private static string BuildChapterId(int levelIndex, int segmentIndex)
    {
        string s = segmentIndex switch
        {
            0 => "start",
            1 => "mid",
            2 => "end",
            _ => null
        };
        if (s == null)
            return null;
        return $"level{levelIndex}_{s}";
    }
}
