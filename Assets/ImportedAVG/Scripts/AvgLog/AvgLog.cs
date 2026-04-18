using System.Collections.Generic;

namespace AVG {
  public class AvgLogLineModel {
    public string chapterId;
    public string dialogId;
    public bool isCharTalk;
    public string charName;
    public string text;
    public List<AvgLogOptionModel> options;
    public AvgLogModifyCache modifyCache;

    public void Fill(string inputChapterId, AvgDialogModel dlgModel) {
      DialogData dlgData = dlgModel.dialogData;
      chapterId = inputChapterId;
      dialogId = dlgData.id;
      
      isCharTalk = !string.IsNullOrEmpty(dlgModel.talkingCharName);
      if (isCharTalk) {
        charName = dlgModel.talkingCharName;
      }
      
      text = dlgData.dlgText ?? "";
      if (dlgData.options != null && dlgData.options.Count > 0) {
        options = new List<AvgLogOptionModel>();
        for(int i =  0; i < dlgData.options.Count; i++) {
          var option = dlgData.options[i];
          AvgLogOptionModel logOption = new AvgLogOptionModel {
            text = option.optText ?? "",
            selected = false, 
          };
          options.Add(logOption);
        }
      }

      modifyCache = new AvgLogModifyCache();

      if (dlgData.setVars != null && dlgData.setVars.Count > 0) {
        foreach (SetVar setVar in dlgData.setVars) {
          modifyCache.CacheSetVar(setVar);
        }
      }

      if (dlgData.setFlags != null && dlgData.setFlags.Count > 0) {
        foreach (string flagName in dlgData.setFlags) {
          modifyCache.CacheAddFlag(flagName);
        }
      }

      foreach (var eventData in dlgModel.events) {
        modifyCache.CacheEvent(eventData);
      }
    }
  }

  public class AvgLogOptionModel {
    public string text;
    public bool selected;
  }

  public class AvgLogModifyCache {
    public Dictionary<string, float> changedVars = new(); //<varName, original value>
    public HashSet<string> addedFlags = new();
    public string cachedBgm;
    public string cachedEnvBgm;

    public void CacheSetVar(SetVar setVar) {
      if (!changedVars.ContainsKey(setVar.varName)) {
        var valueBefore = AvgDataUtil.GetVariable(setVar.varName);
        changedVars.Add(setVar.varName, valueBefore);
      }
    }

    public void CacheAddFlag(string flagName) {
      addedFlags.Add(flagName);
    }

    public void CacheEvent(EventData eventData) {
      switch (eventData.type) {
        case EventData.Type.PLAY_BGM:
        case EventData.Type.STOP_BGM:
          cachedBgm = AvgController.Instance?.soundManager?.currBgm;
          break;
        case EventData.Type.PLAY_ENV_BGM:
        case EventData.Type.STOP_ENV_BGM:
          cachedEnvBgm = AvgController.Instance?.soundManager?.currEnvBgm;
          break;
      }
    }
  }
  
  public class AvgLog {
    private bool m_isInited;
    private LinkedList<AvgLogLineModel> m_list;
    private int m_maxCount;

    public void InitIfNot() {
      if (m_isInited) {
        return;
      }
      m_isInited = true;
      m_maxCount = AvgUtil.LOG_LIMIT_CNT;
      m_list = new LinkedList<AvgLogLineModel>();
    }

    public void AddLog(AvgLogLineModel lineData) {
      m_list.AddLast(lineData);
      
      // 超过限制时移除最早的记录
      if (m_list.Count > m_maxCount) {
        m_list.RemoveFirst();
      }
    }
    
    public IEnumerable<AvgLogLineModel> GetAll() {
      return m_list;
    }
    
    public AvgLogLineModel GetLast() {
      if (m_list.Count == 0) {
        return null;
      }
      return m_list.Last.Value;
    }
    
    /// <summary>
    /// 获取log总数
    /// </summary>
    public int Count => m_list.Count;
    
    /// <summary>
    /// 清空所有日志
    /// </summary>
    public void Clear() {
      m_list.Clear();
    }
    
    /// <summary>
    /// 清除指定索引及之后的所有记录（包括该索引，保留0到index-1的所有记录）
    /// 索引从0开始，0是最早的log
    /// 例如：ClearAfter(2) 会保留索引0、1的记录，清除索引2及之后的所有记录
    /// 同时会恢复被清除log中的ModifyCache：变量值、flag、bgm和envBgm
    /// </summary>
    public void ClearAfter(int index) {
      if (m_list.Count == 0 || index < 0) {
        return;
      }
      
      // 如果index超出范围，不需要清除
      if (index >= m_list.Count) {
        return;
      }
      
      // 收集要清除的节点（从index到末尾）
      var nodesToRemove = new List<LinkedListNode<AvgLogLineModel>>();
      var node = m_list.First;
      for (int i = 0; i < index; i++) {
        node = node.Next;
      }
      while (node != null) {
        nodesToRemove.Add(node);
        node = node.Next;
      }
      
      // 按从新到旧的顺序恢复ModifyCache（从列表末尾往前）
      string oldestBgm = null;
      string oldestEnvBgm = null;
      bool hasBgmCache = false;
      bool hasEnvBgmCache = false;
      
      for (int i = nodesToRemove.Count - 1; i >= 0; i--) {
        var logData = nodesToRemove[i].Value;
        if (logData?.modifyCache == null) {
          continue;
        }
        
        var cache = logData.modifyCache;
        
        // 恢复changedVars中记录的原始变量值
        foreach (var kvp in cache.changedVars) {
          AvgDataUtil.SetVariable(kvp.Key, kvp.Value);
        }
        
        // 移除addedFlags中的flag
        foreach (var flagName in cache.addedFlags) {
          AvgDataUtil.RemoveFlag(flagName);
        }
        
        // 记录bgm和envBgm，保留最旧的（即最后遍历到的）
        if (cache.cachedBgm != null) {
          oldestBgm = cache.cachedBgm;
          hasBgmCache = true;
        }
        if (cache.cachedEnvBgm != null) {
          oldestEnvBgm = cache.cachedEnvBgm;
          hasEnvBgmCache = true;
        }
      }
      
      // 恢复最旧记录的bgm和envBgm
      var soundManager = AvgController.Instance?.soundManager;
      if (soundManager != null) {
        if (hasBgmCache && !string.IsNullOrEmpty(oldestBgm)) {
          soundManager.PlayBgm(oldestBgm);
        }
        if (hasEnvBgmCache && !string.IsNullOrEmpty(oldestEnvBgm)) {
          soundManager.PlayEnvBgm(oldestEnvBgm);
        }
      }
      
      // 删除节点
      foreach (var nodeToRemove in nodesToRemove) {
        m_list.Remove(nodeToRemove);
      }
    }
    
    /// <summary>
    /// 根据chapterId和dialogId查找log的索引，返回-1表示未找到
    /// </summary>
    public int FindIndex(string chapterId, string dialogId) {
      if (string.IsNullOrEmpty(chapterId) || string.IsNullOrEmpty(dialogId) || m_list.Count == 0) {
        return -1;
      }
      
      int index = 0;
      foreach (var logData in m_list) {
        if (logData != null && logData.chapterId == chapterId && logData.dialogId == dialogId) {
          return index;
        }
        index++;
      }
      
      return -1;
    }
  }
}