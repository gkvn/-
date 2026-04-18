using System.Collections.Generic;
using UnityEngine;

namespace AVG {
  public class AvgCharImageModel {
    public string charBody;
    public string charFace;
    public bool isTalking;
  }

  public class AvgDialogModel {
    public DialogData dialogData;
    public string talkingCharName;
    public string talkingCharAvatar;
    public Dictionary<int/*slotIndex*/, AvgCharImageModel> charImageModels = new();
    public List<EventData> events = new();
    public Dictionary<int, List<EventData>> optionEvents = new();
  }
    
  public class AvgModel {
    private ChapterDB m_currentChapter;
    private AvgDialogModel m_currDlg;
    
    public ChapterDB currentChapter => m_currentChapter;
    public AvgDialogModel currDlg => m_currDlg;

    /// <summary>
    /// 当前句之后是否还有可继续的内容（下一条对白、选项、或跳转章节）。
    /// 若为 false，本句播完后即为本章可供推进的最后一屏对白。
    /// </summary>
    public bool HasFollowingContent() {
      if (m_currDlg?.dialogData == null) {
        return false;
      }
      DialogData d = m_currDlg.dialogData;
      if (d.options != null && d.options.Count > 0) {
        return true;
      }
      if (!string.IsNullOrEmpty(d.nextChapter)) {
        return true;
      }
      if (d.nextIds != null && d.nextIds.Count > 0) {
        return true;
      }
      return false;
    }
    
    public bool NextChapter(string chapterId) {
      if (string.IsNullOrEmpty(chapterId)) {
        Debug.LogError("章节ID不能为空");
        return false;
      }
      
      var avgDataLoader =  AvgController.Instance?.dataManager;
      m_currentChapter = avgDataLoader?.LoadChapter(chapterId);
      
      var chapters = avgDataLoader?.avgDB?.chapters;
      if (chapters != null && chapters.ContainsKey(chapterId)) {
        string startDlgId = chapters[chapterId].startDlgId;
        if (!string.IsNullOrEmpty(startDlgId)) {
          JumpToDialog(startDlgId);
          return true;
        }
      }
      
      return false;
    }
    
    public bool JumpToDialog(string dialogId) {
      if (m_currentChapter == null || m_currentChapter.dialogs == null) {
        Debug.LogError("当前没有加载章节");
        return false;
      }
      
      if (string.IsNullOrEmpty(dialogId)) {
        Debug.LogError("对话ID不能为空");
        return false;
      }
      
      if (!m_currentChapter.dialogs.TryGetValue(dialogId, out DialogData dialog)) {
        Debug.LogError($"找不到对话: {dialogId}");
        return false;
      }
      
      m_currDlg = new AvgDialogModel {
        dialogData = dialog,
        talkingCharName = null,
        talkingCharAvatar = null,
        charImageModels = new Dictionary<int, AvgCharImageModel>()
      };

      _BuildCharImageModels(dialog, m_currDlg);
      _LoadEventsIfNeed(dialog, m_currDlg);
      _LoadOptionEventsIfNeed(dialog, m_currDlg); 
      return true;
    }
    
    public bool NextDialog() {
      if (m_currDlg == null || m_currDlg.dialogData == null) {
        Debug.LogWarning("当前没有对话");
        return false;
      }
      
      DialogData dialog = m_currDlg.dialogData;
      
      if (dialog.options != null && dialog.options.Count > 0) {
        Debug.LogWarning("当前对话有选项，请先选择选项");
        return false;
      }
      
      if (!string.IsNullOrEmpty(dialog.nextChapter)) {
        return NextChapter(dialog.nextChapter);
      }
      
      if (dialog.nextIds == null || dialog.nextIds.Count == 0) {
        Debug.Log("当前对话没有下一条对话");
        return false;
      }
      
      string nextId = dialog.nextIds[0];
      return JumpToDialog(nextId);
    }
    
    public bool SelectOption(int optionIndex) {
      if (m_currDlg == null || m_currDlg.dialogData == null) {
        Debug.LogError("当前没有对话");
        return false;
      }
      
      DialogData dialog = m_currDlg.dialogData;
      
      if (dialog.options == null || optionIndex < 0 || optionIndex >= dialog.options.Count) {
        Debug.LogError($"选项索引无效: {optionIndex}");
        return false;
      }
      
      DlgOption option = dialog.options[optionIndex];
      
      if (!string.IsNullOrEmpty(option.nextChapter)) {
        return NextChapter(option.nextChapter);
      }
      
      if (string.IsNullOrEmpty(option.nextId)) {
        Debug.LogError("选项没有指定下一条对话ID或章节ID");
        return false;
      }
      
      return JumpToDialog(option.nextId);
    }
    
    private void _BuildCharImageModels(DialogData dialog, AvgDialogModel dlgModel) {
      if (dialog == null || dialog.charDisplays == null || dialog.charDisplays.Count <= 0) {
        return;
      }
      
      var avgDataLoader =  AvgController.Instance?.dataManager;
      AvgDB avgDB = avgDataLoader?.avgDB;
      if (avgDB == null || avgDB.charDisplays == null) {
        return;
      }
      
      foreach (CharSlotData slotData in dialog.charDisplays) {
        if (string.IsNullOrEmpty(slotData.charDisplayId)) {
          continue;
        }
        
        if (!avgDB.charDisplays.TryGetValue(slotData.charDisplayId, out CharDisplayData charDisplay)) {
          continue;
        }
        
        AvgCharImageModel charImageModel = new AvgCharImageModel {
          charBody = charDisplay.charBody,
          charFace = charDisplay.charFace,
          isTalking = slotData.isTalking
        };
        
        dlgModel.charImageModels[slotData.slotIndex] = charImageModel;
        
        if (slotData.isTalking) {
          dlgModel.talkingCharName = charDisplay.charName;
          dlgModel.talkingCharAvatar = charDisplay.charAvatar;
        }
      }
    }

    private void _LoadEventsIfNeed(DialogData dlgData, AvgDialogModel dlgModel) {
      if (dlgData == null || dlgModel == null) {
        return;
      }
      if (dlgData.events == null || dlgData.events.Count == 0) {
        return;
      }
      var avgDataLoader =  AvgController.Instance?.dataManager;
      AvgDB avgDB = avgDataLoader?.avgDB;
      if (avgDB == null || avgDB.events == null) {
        return;
      }

      dlgModel.events = new List<EventData>();
      foreach (string eventId in dlgData.events) {
        if (avgDB.events.TryGetValue(eventId, out EventData eventData)) {
          dlgModel.events.Add(eventData);
        }
      }
    }
    
    private void _LoadOptionEventsIfNeed(DialogData dlgData, AvgDialogModel dlgModel) {
      if (dlgData == null || dlgModel == null) {
        return;
      }
      if (dlgData.options == null || dlgData.options.Count == 0) {
        return;
      }
      var avgDataLoader =  AvgController.Instance?.dataManager;
      AvgDB avgDB = avgDataLoader?.avgDB;
      if (avgDB == null || avgDB.events == null) {
        return;
      }

      for (int i = 0; i < dlgData.options.Count; i++) {
        var option = dlgData.options[i];
        if (option.events == null || option.events.Count == 0) {
          continue;
        }
        var events = new List<EventData>();
        foreach (string eventId in option.events) {
          if (avgDB.events.TryGetValue(eventId, out EventData eventData)) {
            events.Add(eventData);
          }
        }
        dlgModel.optionEvents.Add(i, events);
      }
    }
  }
}
