using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace AVG {
  [Serializable]
  public class SetVar {
    public enum Operation {
      SET, MINUS, ADD,
    }

    public string varName;
    public Operation opt;
    public float num; 
  }

[Serializable]
public class CompareVar {
  public enum CompSign {
    LESS, MORE, EQUAL, LESS_EQUAL, MORE_EQUAL,
  }

  public string varName;
  public CompSign compSign;
  public float thre;
}

[Serializable]
public class CondData {
  public List<CompareVar> compareVars = new List<CompareVar>();
  public List<string> flags = new List<string>();
}

[Serializable]
public class DlgOption {
  public string optText;
  public string nextId;
  public string condId;
  public string nextChapter;
  public List<string> events = new List<string>();
}

[Serializable]
public class EventData {
  public enum Type {
    PLAY_BGM, PLAY_ENV_BGM, PLAY_SF, STOP_BGM, STOP_ENV_BGM,
  }

  public string eventId;
  public Type type;
  public string param1;
  public string param2;
}

[Serializable]
public class DialogData {
  public string id;
  public string dlgText;
  public string cgId;
  public List<CharSlotData> charDisplays = new();
  public List<DlgOption> options = new();
  public List<string> events = new();
  public List<string> setFlags = new();
  public List<SetVar> setVars = new();
  public List<string> nextIds = new(); /*sorted by priority*/
  public Dictionary<string/*dlgId*/, string/*condId*/> nextConds = new();
  public string nextChapter; /*chapterId to jump to when this dialog ends*/
}

[Serializable]
public class CharSlotData {
  public string charDisplayId;
  public int slotIndex;
  public bool isTalking;
}

[Serializable]
public class CharDisplayData {
  public string charDisplayId;
  public string charName;
  public string charAvatar;
  public string charBody;
  public string charFace;
}

[Serializable]
public class ChapterData {
  public string chapId;
  public string chapName;
  public string startDlgId;
}

[Serializable]
public class AvgDB {
  public Dictionary<string, ChapterData> chapters = new();
  public Dictionary<string, CondData> conditions = new();
  public Dictionary<string, EventData> events = new();
  public Dictionary<string, CharDisplayData> charDisplays = new();
}

[Serializable]
public class ChapterDB {
  public string chapterId;
  public Dictionary<string /*id*/, DialogData> dialogs = new();
}
}
