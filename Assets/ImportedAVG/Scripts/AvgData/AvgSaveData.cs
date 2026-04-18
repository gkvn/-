using UnityEngine;
using System;
using System.Collections.Generic;

namespace AVG {

  [Serializable]
  public class AvgSetting {
    public AvgMode mode = AvgMode.DEFAULT;
    public float typingSpeed = AvgUtil.DEFAULT_TYPING_SPEED;
    public float fastModeWaitTime = AvgUtil.DEFAULT_FAST_WAIT_TIME;
    public float autoWaitTime = AvgUtil.DEFAULT_AUTO_WAIT_TIME;

    public float masterVolume = 1.0f;
    public float bgmVolume = 0.8f;
    public float envVolume = 0.8f;
    public float seVolume = 1.0f;
    public bool isMuted = false;

    public int resolutionIndex = 0;
    public bool isFullScreen = true;
  }

  /// <summary>
  /// 会话内设置与阅读历史（不持久化到磁盘）。
  /// </summary>
  [Serializable]
  public class AvgGlobalSave {
    public AvgSetting settings = new();
    public List<string> readHistory = new();
  }
}
