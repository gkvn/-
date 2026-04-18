using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace AVG {
  /// <summary>
  /// 加载 avg_table / 章节 JSON；变量、标记、阅读历史仅保存在本次运行内存中（不写盘）。
  /// </summary>
  public class AvgDataManager {
    private const string AVG_TABLE_PATH = "Json/avg_table";
    private const string CHAPTER_PATH_PREFIX = "Json/Chapters/";

    private AvgDB m_avgDB;
    private ChapterDB m_loadedChapter;
    private string m_currentChapterId;
    private bool m_isInited;

    private readonly HashSet<string> m_sessionFlags = new();
    private readonly Dictionary<string, float> m_sessionVars = new();
    private readonly AvgGlobalSave m_sessionState = new();

    public AvgDB avgDB => m_avgDB;

    /// <summary>打字速度、自动播放等待等；本会话内有效，退出游戏不保留。</summary>
    public AvgGlobalSave globalSave => m_sessionState;

    public HashSet<string> flags => m_sessionFlags;
    public Dictionary<string, float> vars => m_sessionVars;

    public List<string> readHistory => m_sessionState.readHistory;

    public void InitIfNot() {
      if (m_isInited) {
        return;
      }
      m_isInited = true;
      _LoadAvgDB();
      if (m_sessionState.settings == null) {
        m_sessionState.settings = new AvgSetting();
      }
      if (m_sessionState.readHistory == null) {
        m_sessionState.readHistory = new List<string>();
      }
    }

    public ChapterDB LoadChapter(string chapterId) {
      if (string.IsNullOrEmpty(chapterId)) {
        Debug.LogError("章节ID不能为空");
        return null;
      }

      if (m_loadedChapter != null && m_currentChapterId == chapterId) {
        return m_loadedChapter;
      }

      if (m_loadedChapter != null && m_currentChapterId != chapterId) {
        Debug.Log($"释放前一个章节: {m_currentChapterId}");
        m_loadedChapter = null;
        m_currentChapterId = null;
        Resources.UnloadUnusedAssets();
      }

      string path = $"{CHAPTER_PATH_PREFIX}{chapterId}";
      TextAsset jsonAsset = Resources.Load<TextAsset>(path);
      if (jsonAsset == null) {
        Debug.LogError($"无法加载章节数据文件: {path}");
        return null;
      }

      ChapterDB chapterDB = JsonConvert.DeserializeObject<ChapterDB>(jsonAsset.text);
      if (chapterDB == null) {
        Debug.LogError($"章节 {chapterId} 数据反序列化失败");
        return null;
      }

      if (chapterDB.chapterId != chapterId) {
        Debug.LogWarning($"章节ID不匹配: 文件中的ID为 {chapterDB.chapterId}，请求的ID为 {chapterId}");
      }

      m_loadedChapter = chapterDB;
      m_currentChapterId = chapterId;
      Debug.Log($"章节 {chapterId} 加载成功 - 对话数: {chapterDB.dialogs?.Count ?? 0}");
      return chapterDB;
    }

    private void _LoadAvgDB() {
      TextAsset jsonAsset = Resources.Load<TextAsset>(AVG_TABLE_PATH);
      if (jsonAsset == null) {
        Debug.LogError($"无法加载AvgDB数据文件: {AVG_TABLE_PATH}");
        return;
      }
      m_avgDB = JsonConvert.DeserializeObject<AvgDB>(jsonAsset.text);
      if (m_avgDB == null) {
        Debug.LogError("AvgDB数据反序列化失败");
        return;
      }
      Debug.Log($"AvgDB加载成功 - 章节数: {m_avgDB.chapters?.Count ?? 0}, " + $"条件数: {m_avgDB.conditions?.Count ?? 0}, " +
                $"事件数: {m_avgDB.events?.Count ?? 0}");
    }
  }
}
