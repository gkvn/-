using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System;
using Newtonsoft.Json;
using AVG;

/// <summary>
/// CSV 源与输出均位于 ImportedAVG 下，与运行时读取的 Json 一致。
/// 菜单：Tools/AVG/转换所有CSV到JSON
/// </summary>
public static class CSVToJSONConverter {
  private static readonly string CSV_DIALOG_PATH = "Assets/ImportedAVG/CSV/Chapters";
  private static readonly string CSV_PATH = "Assets/ImportedAVG/CSV";
  private static readonly string JSON_OUTPUT_PATH = "Assets/ImportedAVG/Resources/Json";
  private static readonly string JSON_CHAPTER_OUTPUT_PATH = "Assets/ImportedAVG/Resources/Json/Chapters";
  
  [MenuItem("Tools/AVG/转换所有CSV到JSON")]
  public static void ConvertAllCSVToJSON() {
    _ConvertAVGTables();
    if (!Directory.Exists(JSON_OUTPUT_PATH)) {
      Directory.CreateDirectory(JSON_OUTPUT_PATH);
    }

    if (Directory.Exists(CSV_DIALOG_PATH)) {
      string[] csvFiles = Directory.GetFiles(CSV_DIALOG_PATH, "*.csv");
      foreach (string csvFile in csvFiles) {
        _ConvertChapterDialogTable(csvFile);
      }
      Debug.Log($"在 {CSV_DIALOG_PATH} 中找到 {csvFiles.Length} 个章节CSV文件");
    } else {
      Debug.LogWarning($"章节CSV目录不存在: {CSV_DIALOG_PATH}");
    }

    Debug.Log("所有CSV文件转换完成");
    AssetDatabase.Refresh();
  }

  [MenuItem("Tools/AVG/转换当前章节CSV")]
  public static void ConvertCurrentChapter() {
    _ConvertAVGTables();
    string csvPath = EditorUtility.OpenFilePanel("选择章节CSV文件", Path.GetFullPath(CSV_DIALOG_PATH), "csv");
    if (!string.IsNullOrEmpty(csvPath)) {
      _ConvertChapterDialogTable(csvPath);
      AssetDatabase.Refresh();
      Debug.Log("当前章节CSV文件转换完成");
    }
  }

  private static void _ConvertAVGTables() {
    string configPath = Path.Combine(CSV_PATH, "chapter_table.csv");
    if (!File.Exists(configPath)) {
      Debug.LogWarning($"章节配置文件不存在: {configPath}");
      return;
    }
    Dictionary<string, ChapterData> chapterConfigs = _ParseChapters(configPath);

    Dictionary<string, EventData> events = _ParseEvents();
    Dictionary<string, CondData> conditions = _ParseConditions();
    Dictionary<string, CharDisplayData> charDisplays = _ParseCharDisplays();

    var db = new AvgDB();
    db.chapters = chapterConfigs;
    db.events = events;
    db.conditions = conditions;
    db.charDisplays = charDisplays;

    string json = JsonConvert.SerializeObject(db, Formatting.Indented);

    if (!Directory.Exists(JSON_OUTPUT_PATH)) {
      Directory.CreateDirectory(JSON_OUTPUT_PATH);
    }

    string outputPath = Path.Combine(JSON_OUTPUT_PATH, "avg_table.json");
    File.WriteAllText(outputPath, json, Encoding.UTF8);
    Debug.Log($"已转换avg配置到: {outputPath} (章节: {chapterConfigs.Count}, 事件: {events.Count}, 条件: {conditions.Count}, 角色展示: {charDisplays.Count})");
  }

  public static void _ConvertChapterDialogTable(string csvFilePath) {
    if (!File.Exists(csvFilePath)) {
      Debug.LogError($"CSV文件不存在: {csvFilePath}");
      return;
    }

    string fileName = Path.GetFileNameWithoutExtension(csvFilePath);

    Debug.Log($"开始转换章节CSV文件: {fileName}");

    Dictionary<string /*id*/, DialogData> dialogs = _ParseDialogs(csvFilePath);

    ChapterDB chapter = new ChapterDB {
      chapterId = fileName,
      dialogs = dialogs,
    };

    string json = JsonConvert.SerializeObject(chapter, Formatting.Indented);

    if (!Directory.Exists(JSON_CHAPTER_OUTPUT_PATH)) {
      Directory.CreateDirectory(JSON_CHAPTER_OUTPUT_PATH);
    }

    string outputPath = Path.Combine(JSON_CHAPTER_OUTPUT_PATH, $"{fileName}.json");
    File.WriteAllText(outputPath, json, Encoding.UTF8);

    Debug.Log($"已转换 {dialogs.Count} 条对话: {outputPath}");
  }

  private static Dictionary<string /*id*/, DialogData> _ParseDialogs(string csvFilePath) {
    Dictionary<string /*id*/, DialogData> dialogs = new Dictionary<string /*id*/, DialogData>();

    string[] lines = File.ReadAllLines(csvFilePath, Encoding.UTF8);
    if (lines.Length < 2) {
      Debug.LogWarning($"CSV文件数据过少: {csvFilePath}");
      return dialogs;
    }

    string[] headers = _ParseCSVLine(lines[0]);
    Dictionary<string, int> columnMap = _CreateColumnMap(headers);

    Dictionary<string /*optionId*/, DlgOption> optionsMap = new Dictionary<string, DlgOption>();
    for (int i = 1; i < lines.Length; i++) {
      if (string.IsNullOrWhiteSpace(lines[i])) continue;

      string[] fields = _ParseCSVLine(lines[i]);
      if (fields.Length == 0) continue;

      if (fields.Length > 0) {
        string idField = fields[0].Trim();
        string optionId = null;
        if (idField.StartsWith("#Option:", StringComparison.OrdinalIgnoreCase) || 
            idField.StartsWith("#option:", StringComparison.OrdinalIgnoreCase)) {
          optionId = idField.Substring(8).Trim();
        } else if (idField.StartsWith("#Option：", StringComparison.OrdinalIgnoreCase) || 
                   idField.StartsWith("#option：", StringComparison.OrdinalIgnoreCase)) {
          optionId = idField.Substring(8).Trim();
        }
        
        if (!string.IsNullOrEmpty(optionId)) {
          DlgOption option = _ParseOptionLine(fields, columnMap, optionId);
          if (option != null) {
            optionsMap[optionId] = option;
          }
        }
      }
    }

    List<DialogData> dialogList = new List<DialogData>();

    for (int i = 1; i < lines.Length; i++) {
      if (string.IsNullOrWhiteSpace(lines[i])) continue;

      string[] fields = _ParseCSVLine(lines[i]);
      if (fields.Length == 0) continue;

      if (fields.Length > 0) {
        string idField = fields[0].Trim();
        if (idField.StartsWith("#Option:", StringComparison.OrdinalIgnoreCase) || 
            idField.StartsWith("#option:", StringComparison.OrdinalIgnoreCase) ||
            idField.StartsWith("#Option：", StringComparison.OrdinalIgnoreCase) || 
            idField.StartsWith("#option：", StringComparison.OrdinalIgnoreCase)) {
          continue;
        }
      }

      DialogData dialog = _ParseDialogLine(fields, columnMap, optionsMap);
      if (dialog != null) {
        dialogs.Add(dialog.id, dialog);
        dialogList.Add(dialog);
      }
    }

    for (int i = 0; i < dialogList.Count; i++) {
      DialogData dialog = dialogList[i];
      if (dialog.nextIds.Count == 0 && i + 1 < dialogList.Count) {
        DialogData nextDialog = dialogList[i + 1];
        dialog.nextIds.Add(nextDialog.id);
      }
    }

    return dialogs;
  }

  private static DialogData _ParseDialogLine(string[] fields, Dictionary<string, int> columnMap, Dictionary<string, DlgOption> allOptionsMap) {
    DialogData dialog = new DialogData();

    Dictionary<int, string> charDisplayIdMap = new Dictionary<int, string>();
    HashSet<string> talkingCharDisplayIds = new HashSet<string>();

    if (fields.Length > 0)
      dialog.id = fields[0].Trim();

    if (fields.Length > 1)
      dialog.dlgText = fields[1].Trim();

    for (int j = 2; j < fields.Length; j++) {
      string tagsField = fields[j].Trim();
      if (string.IsNullOrEmpty(tagsField)) continue;
      
      string[] tags = tagsField.Split(',');
      foreach (string tag in tags) {
        string trimmedTag = tag.Trim();
        if (string.IsNullOrEmpty(trimmedTag)) continue;
        _ParseTag(trimmedTag, dialog, charDisplayIdMap, talkingCharDisplayIds, allOptionsMap);
      }
    }

    var sortedCharDisplays = new List<KeyValuePair<int, string>>(charDisplayIdMap);
    sortedCharDisplays.Sort((a, b) => a.Key.CompareTo(b.Key));
    foreach (var kvp in sortedCharDisplays) {
      CharSlotData slotData = new CharSlotData {
        charDisplayId = kvp.Value,
        slotIndex = kvp.Key - 1,
        isTalking = talkingCharDisplayIds.Contains(kvp.Value)
      };
      dialog.charDisplays.Add(slotData);
    }

    return dialog;
  }

  private static void _ParseTag(string tag, DialogData dialog, Dictionary<int, string> charDisplayIdMap, HashSet<string> talkingCharDisplayIds, Dictionary<string, DlgOption> allOptionsMap) {
    if (!tag.StartsWith("#")) return;

    string content = tag.Substring(1);

    string tagName;
    int index = 0;
    string value;
    bool hasIndex = false;

    Match match = Regex.Match(content, @"^(\w+)@(\d+)[:：](.+)$");
    if (match.Success) {
      tagName = match.Groups[1].Value.ToLower();
      index = int.Parse(match.Groups[2].Value);
      value = match.Groups[3].Value;
      hasIndex = true;
    } else {
      Match simpleMatch = Regex.Match(content, @"^(\w+)[:：](.+)$");
      if (!simpleMatch.Success) {
        Debug.LogWarning($"tag格式不正确，无法解析: #{content}");
        return;
      }
      tagName = simpleMatch.Groups[1].Value.ToLower();
      value = simpleMatch.Groups[2].Value;
    }

    switch (tagName) {
      case "event":
        dialog.events.Add(value);
        break;

      case "setflag":
        dialog.setFlags.Add(value);
        break;

      case "setvariable":
        Match varMatch = Regex.Match(value, @"([a-zA-Z_]+)([\+\-\=])(\d+(?:\.\d+)?)");
        if (varMatch.Success) {
          string varName = varMatch.Groups[1].Value;
          string op = varMatch.Groups[2].Value;
          float numValue = float.Parse(varMatch.Groups[3].Value);
          var varOpt = new SetVar() {
            varName = varName,
            num = numValue,
          };
          switch (op) {
            case "+":
              varOpt.opt = SetVar.Operation.ADD;
              break;
            case "-":
              varOpt.opt = SetVar.Operation.MINUS;
              break;
            case "=":
              varOpt.opt = SetVar.Operation.SET;
              break;
          }
          dialog.setVars.Add(varOpt);
        }
        break;

      case "next":
        string[] nextParts = value.Split('|');
        if (nextParts.Length >= 1 && !string.IsNullOrEmpty(nextParts[0].Trim())) {
          string nextId = nextParts[0].Trim();
          dialog.nextIds.Add(nextId);
          if (nextParts.Length >= 2 && !string.IsNullOrEmpty(nextParts[1].Trim())) {
            dialog.nextConds[nextId] = nextParts[1].Trim();
          }
        }
        break;

      case "option":
        string optionId = value.Trim();
        if (allOptionsMap != null && allOptionsMap.ContainsKey(optionId)) {
          DlgOption sourceOption = allOptionsMap[optionId];
          DlgOption option = new DlgOption {
            optText = sourceOption.optText,
            nextId = sourceOption.nextId,
            condId = sourceOption.condId,
            nextChapter = sourceOption.nextChapter,
            events = new List<string>(sourceOption.events)
          };
          dialog.options.Add(option);
        } else {
          Debug.LogWarning($"未找到option_id: {optionId}");
        }
        break;

      case "char":
        if (!hasIndex) {
          Debug.LogWarning($"char tag必须包含index（1或2），格式: #char@1:hero1");
          break;
        }
        if (index >= 1 && index <= 2) {
          if (!string.IsNullOrEmpty(value)) {
            charDisplayIdMap[index] = value;
          }
        } else {
          Debug.LogWarning($"char的index必须是1或2，当前为: {index}");
        }
        break;

      case "istalking":
        if (!string.IsNullOrEmpty(value)) {
          talkingCharDisplayIds.Add(value);
        }
        break;

      case "cgid":
        if (!string.IsNullOrEmpty(value)) {
          dialog.cgId = value;
        }
        break;

      case "nextchapter":
        if (!string.IsNullOrEmpty(value)) {
          dialog.nextChapter = value.Trim();
        }
        break;

      default:
        Debug.LogWarning($"未识别的tag: #{tagName}，值: {value}");
        break;
    }
  }

  private static DlgOption _ParseOptionLine(string[] fields, Dictionary<string, int> columnMap, string optionId) {
    DlgOption option = new DlgOption();

    if (fields.Length > 1) {
      option.optText = fields[1].Trim();
    }

    for (int j = 2; j < fields.Length; j++) {
      string tagsField = fields[j].Trim();
      if (string.IsNullOrEmpty(tagsField)) continue;
      
      string[] tags = tagsField.Split(',');
      foreach (string tag in tags) {
        string trimmedTag = tag.Trim();
        if (string.IsNullOrEmpty(trimmedTag)) continue;
        _ParseOptionTag(trimmedTag, option);
      }
    }

    return option;
  }

  private static void _ParseOptionTag(string tag, DlgOption option) {
    if (!tag.StartsWith("#")) return;

    string content = tag.Substring(1);

    string tagName;
    string value;

    Match match = Regex.Match(content, @"^(\w+)@(\d+)[:：](.+)$");
    if (match.Success) {
      tagName = match.Groups[1].Value.ToLower();
      value = match.Groups[3].Value;
    } else {
      Match simpleMatch = Regex.Match(content, @"^(\w+)[:：](.+)$");
      if (!simpleMatch.Success) {
        Debug.LogWarning($"option tag格式不正确，无法解析: #{content}");
        return;
      }
      tagName = simpleMatch.Groups[1].Value.ToLower();
      value = simpleMatch.Groups[2].Value;
    }

    switch (tagName) {
      case "condid":
        option.condId = value.Trim();
        break;

      case "event":
        string eventId = value.Trim();
        if (!string.IsNullOrEmpty(eventId) && !option.events.Contains(eventId)) {
          option.events.Add(eventId);
        }
        break;

      case "nextid":
        option.nextId = value.Trim();
        break;

      case "nextchapter":
        option.nextChapter = value.Trim();
        break;

      default:
        Debug.LogWarning($"未识别的option tag: #{tagName}，值: {value}");
        break;
    }
  }

  private static Dictionary<string, ChapterData> _ParseChapters(string configPath) {
    Dictionary<string, ChapterData> configs = new Dictionary<string, ChapterData>();

    string[] lines = File.ReadAllLines(configPath, Encoding.UTF8);
    if (lines.Length < 2) return configs;

    string[] headers = _ParseCSVLine(lines[0]);
    Dictionary<string, int> columnMap = _CreateColumnMap(headers);

    for (int i = 1; i < lines.Length; i++) {
      if (string.IsNullOrWhiteSpace(lines[i])) continue;

      string[] fields = _ParseCSVLine(lines[i]);
      ChapterData config = new ChapterData();

      config.chapId = _GetField(fields, columnMap, "chapterid");
      if (string.IsNullOrEmpty(config.chapId)) {
        Debug.LogWarning($"第 {i + 1} 行缺少章节ID，跳过");
        continue;
      }
      
      config.chapName = _GetField(fields, columnMap, "chaptername");

      string startDlgId = _GetField(fields, columnMap, "startdlgid");
      if (!string.IsNullOrEmpty(startDlgId)) {
        config.startDlgId = startDlgId;
      }

      configs.Add(config.chapId, config);
    }

    return configs;
  }
  
  private static Dictionary<string, EventData> _ParseEvents() {
    Dictionary<string, EventData> events = new Dictionary<string, EventData>();
    
    string eventsPath = Path.Combine(CSV_PATH, "events_table.csv");
    if (!File.Exists(eventsPath)) {
      Debug.LogWarning($"事件配置文件不存在: {eventsPath}");
      return events;
    }

    string[] lines = File.ReadAllLines(eventsPath, Encoding.UTF8);
    if (lines.Length < 2) return events;

    string[] headers = _ParseCSVLine(lines[0]);
    Dictionary<string, int> columnMap = _CreateColumnMap(headers);

    for (int i = 1; i < lines.Length; i++) {
      if (string.IsNullOrWhiteSpace(lines[i])) continue;

      string[] fields = _ParseCSVLine(lines[i]);
      if (fields.Length == 0) continue;

      EventData eventData = new EventData();
      eventData.eventId = _GetField(fields, columnMap, "eventid");
      if (string.IsNullOrEmpty(eventData.eventId)) continue;

      string typeStr = _GetField(fields, columnMap, "type").ToUpper();
      if (Enum.TryParse<EventData.Type>(typeStr, out EventData.Type type)) {
        eventData.type = type;
      }

      eventData.param1 = _GetField(fields, columnMap, "param1");
      eventData.param2 = _GetField(fields, columnMap, "param2");

      events.Add(eventData.eventId, eventData);
    }

    return events;
  }

  private static Dictionary<string, CondData> _ParseConditions() {
    Dictionary<string, CondData> conditions = new Dictionary<string, CondData>();
    
    string conditionsPath = Path.Combine(CSV_PATH, "conditions_table.csv");
    if (!File.Exists(conditionsPath)) {
      Debug.LogWarning($"条件配置文件不存在: {conditionsPath}");
      return conditions;
    }

    string[] lines = File.ReadAllLines(conditionsPath, Encoding.UTF8);
    if (lines.Length < 2) return conditions;

    string[] headers = _ParseCSVLine(lines[0]);
    Dictionary<string, int> columnMap = _CreateColumnMap(headers);

    for (int i = 1; i < lines.Length; i++) {
      if (string.IsNullOrWhiteSpace(lines[i])) continue;

      string[] fields = _ParseCSVLine(lines[i]);
      if (fields.Length == 0) continue;

      string condId = _GetField(fields, columnMap, "condid");
      if (string.IsNullOrEmpty(condId)) continue;

      CondData condData = new CondData();

      string CompareVarsStr = _GetField(fields, columnMap, "CompareVars");
      if (!string.IsNullOrEmpty(CompareVarsStr)) {
        string[] CompareVars = CompareVarsStr.Split('|');
        foreach (string CompareVarStr in CompareVars) {
          string trimmed = CompareVarStr.Trim();
          if (string.IsNullOrEmpty(trimmed)) continue;

          Match match = Regex.Match(trimmed, @"([a-zA-Z_]+)\s*(<|>|<=|>=|=)\s*(\d+(?:\.\d+)?)");
          if (match.Success) {
            string varName = match.Groups[1].Value;
            string compSignStr = match.Groups[2].Value;
            float thre = float.Parse(match.Groups[3].Value);

            CompareVar CompareVar = new CompareVar {
              varName = varName,
              thre = thre
            };

            switch (compSignStr) {
              case "<":
                CompareVar.compSign = CompareVar.CompSign.LESS;
                break;
              case ">":
                CompareVar.compSign = CompareVar.CompSign.MORE;
                break;
              case "<=":
                CompareVar.compSign = CompareVar.CompSign.LESS_EQUAL;
                break;
              case ">=":
                CompareVar.compSign = CompareVar.CompSign.MORE_EQUAL;
                break;
              case "=":
                CompareVar.compSign = CompareVar.CompSign.EQUAL;
                break;
            }

            condData.compareVars.Add(CompareVar);
          }
        }
      }

      string flagsStr = _GetField(fields, columnMap, "flags");
      if (!string.IsNullOrEmpty(flagsStr)) {
        string[] flags = flagsStr.Split('|');
        foreach (string flag in flags) {
          string trimmed = flag.Trim();
          if (!string.IsNullOrEmpty(trimmed)) {
            condData.flags.Add(trimmed);
          }
        }
      }

      conditions.Add(condId, condData);
    }

    return conditions;
  }

  private static Dictionary<string, CharDisplayData> _ParseCharDisplays() {
    Dictionary<string, CharDisplayData> charDisplays = new Dictionary<string, CharDisplayData>();
    
    string charDisplaysPath = Path.Combine(CSV_PATH, "charDisplays_table.csv");
    if (!File.Exists(charDisplaysPath)) {
      Debug.LogWarning($"角色展示配置文件不存在: {charDisplaysPath}");
      return charDisplays;
    }

    string[] lines = File.ReadAllLines(charDisplaysPath, Encoding.UTF8);
    if (lines.Length < 2) return charDisplays;

    string[] headers = _ParseCSVLine(lines[0]);
    Dictionary<string, int> columnMap = _CreateColumnMap(headers);

    for (int i = 1; i < lines.Length; i++) {
      if (string.IsNullOrWhiteSpace(lines[i])) continue;

      string[] fields = _ParseCSVLine(lines[i]);
      if (fields.Length == 0) continue;

      CharDisplayData charDisplay = new CharDisplayData();
      charDisplay.charDisplayId = _GetField(fields, columnMap, "chardisplayid");
      if (string.IsNullOrEmpty(charDisplay.charDisplayId)) continue;

      charDisplay.charName = _GetField(fields, columnMap, "charname");
      charDisplay.charBody = _GetField(fields, columnMap, "charbody");
      charDisplay.charFace = _GetField(fields, columnMap, "charface");
      charDisplay.charAvatar = _GetField(fields, columnMap, "charavatar");

      charDisplays.Add(charDisplay.charDisplayId, charDisplay);
    }

    return charDisplays;
  }

  private static string[] _ParseCSVLine(string line) {
    List<string> result = new List<string>();
    bool inQuotes = false;
    StringBuilder currentField = new StringBuilder();

    for (int i = 0; i < line.Length; i++) {
      char c = line[i];

      if (c == '"') {
        if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') {
          currentField.Append('"');
          i++; 
        } else {
          inQuotes = !inQuotes;
        }
      }
      else if (c == ',' && !inQuotes) {
        result.Add(currentField.ToString());
        currentField.Clear();
      } else {
        currentField.Append(c);
      }
    }

    result.Add(currentField.ToString());
    return result.ToArray();
  }

  private static string _GetField(string[] fields, Dictionary<string, int> columnMap, string fieldName) {
    if (columnMap.ContainsKey(fieldName) && columnMap[fieldName] < fields.Length) {
      return fields[columnMap[fieldName]].Trim();
    }
    return "";
  }

  private static Dictionary<string, int> _CreateColumnMap(string[] headers) {
    Dictionary<string, int> columnMap = new Dictionary<string, int>();

    for (int i = 0; i < headers.Length; i++) {
      string header = headers[i].Trim().ToLower();
      columnMap[header] = i;
    }

    return columnMap;
  }
}
