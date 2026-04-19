using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using AVG.Sound;

namespace AVG {

  [Serializable]
  public class AvgStringEvent : UnityEvent<string> { }

  [Serializable]
  public class AvgTwoStringEvent : UnityEvent<string, string> { }

  public enum AvgState {
    IDLE,    // 空闲
    TYPING,  // 打字中
    WAITING,  // 等待中
  }
  
  public enum AvgMode {
    DEFAULT, //  手动
    AUTO,    // 自动播放
    FAST,   // 快进
  }

  /// <summary>尽量早于关卡逻辑 Awake，保证 Instance 在 LevelConfig 等访问前可用。</summary>
  [DefaultExecutionOrder(-200)]
  public class AvgController : MonoBehaviour {

    /// <summary>当前场景中的 AVG 控制器（原工程依赖 GameManager，ImportedAVG 使用单例便于子系统访问）。</summary>
    public static AvgController Instance { get; private set; }

    [SerializeField] 
    private AvgImageView _imageView;
    [SerializeField]
    private AvgMenuView _menuView;
    [SerializeField]
    private AvgLogView _logView;
    [SerializeField]
    private string _dialogViewPrefabName = "Prefabs/AvgDialogView";
    [SerializeField]
    private Transform _dialogViewParent;
    [SerializeField] 
    private bool _enableDebugLogs;
    [Tooltip("若为 true，在 Start 时调用 InitIfNot（原工程由 GameManager 调用，迁入 signal 后默认自动初始化）。")]
    [SerializeField]
    private bool _initOnStart = true;
    [Header("界面显隐")]
    [Tooltip("包住对白、立绘/CG、菜单等游戏内 AVG 画布的根物体；可不指定，则改为分别开关 image/menu/dialog。")]
    [SerializeField]
    private GameObject _avgUiRoot;
    [Tooltip("整页淡入淡出用；不指定则从 Avg Ui Root 上 GetComponent/自动添加 CanvasGroup。")]
    [SerializeField]
    private CanvasGroup _avgUiCanvasGroup;
    [SerializeField]
    private float _avgUiFadeInDuration = 0.35f;
    [SerializeField]
    private float _avgUiFadeOutDuration = 0.35f;

    private FadeAnimation _avgUiFadeAnim;

    [Header("Events（代码或 Inspector 均可订阅）")]
    [Tooltip("每一句对白文字展示完毕（进入可点击继续的 WAITING），参数：chapterId, dialogId")]
    [SerializeField]
    private AvgTwoStringEvent _onDialogLineRevealComplete = new AvgTwoStringEvent();
    [Tooltip("当前句为本章最后一句（展示完毕且无后续对白/选项），紧接在同一次流程于上一事件之后；用于与 OnDialogLineRevealComplete 区分")]
    [SerializeField]
    private AvgTwoStringEvent _onFinalDialogLineRevealComplete = new AvgTwoStringEvent();
    [Tooltip("当前章节已无可继续推进的对白（进入 IDLE），参数：章节 id")]
    [SerializeField]
    private AvgStringEvent _onChapterPlaybackEnded = new AvgStringEvent();

    /// <summary>每一句打字/展示完毕，进入 WAITING。</summary>
    public event Action<string, string> DialogLineRevealComplete;
    /// <summary>该句为本章最后一句对白（展示完毕即无后续内容）。</summary>
    public event Action<string, string> FinalDialogLineRevealComplete;
    /// <summary>章节播完，无法再 Next（将进入 IDLE）。</summary>
    public event Action<string> ChapterPlaybackEnded;

    private AvgDialogView m_dialogView;
    private AvgModel m_model;
    private bool m_isInited;
    /// <summary>上一帧玩家视角下主界面是否为隐藏；用于区分「重新打开」与「一直开着」。</summary>
    private bool _avgUiWasHidden = true;
    private AvgState m_currentState = AvgState.IDLE;
    private AvgMode m_currentMode = AvgMode.DEFAULT;
    private Coroutine m_typingCoroutine;
    private Coroutine m_waitCoroutine;

    #region Modules
    private AvgDataManager m_dataManager;
    private AvgLog m_log;
    private AvgSoundManager m_soundManager;
    
    public AvgDataManager dataManager => m_dataManager;
    public AvgSoundManager soundManager => m_soundManager;
    #endregion
    
    public string currChapterId => m_model?.currentChapter?.chapterId;
    public string currDialogId => m_model?.currDlg?.dialogData?.id;
    public AvgMode avgMode {
      get {
        return m_currentMode;
      }
      set {
        if (_enableDebugLogs) {
          Debug.Log($"[AvgController] avgMode changed: {m_currentMode} -> {value}");
        }
        m_currentMode = value;
        _menuView.RenderBtns();
        _OnModeChanged();
      }
    } 

    private void Awake() {
      if (Instance != null) {
        if (Instance != this) {
          Debug.LogWarning("[ImportedAVG] 场景中存在多个 AvgController，仅第一个实例有效。");
        }
        return;
      }
      Instance = this;
    }

    private void OnDestroy() {
      if (Instance == this) {
        Instance = null;
      }
    }

    private void Start() {
      if (_initOnStart) {
        InitIfNot();
      }
    }

    #region Public
    public void InitIfNot() {
      if (m_isInited) {
        return;
      }
      m_isInited = true;
      
      //modules
      m_dataManager = new AvgDataManager();
      m_dataManager.InitIfNot();
      m_log = new AvgLog();
      m_log.InitIfNot();
      m_soundManager = new AvgSoundManager();
      m_model = new AvgModel();
      
      //views
      _LoadDialogView();
      _menuView.InitIfNot();
      _logView.gameObject.SetActive(false);
      
      //initial state
      _ChangeState(AvgState.IDLE);
      _EnsureAvgUiCanvasGroup();
      _ApplyAvgUiHiddenImmediate();
    }

    /// <summary>
    /// 显示/隐藏游戏内 AVG 界面（对白、立绘、菜单等）。有 CanvasGroup 时做淡入/淡出。
    /// </summary>
    public void SetAvgUiVisible(bool visible) {
      InitIfNot();
      _EnsureAvgUiCanvasGroup();
      _KillAvgUiFadeTween();
      if (_HasFadeTarget()) {
        if (visible) {
          _ResetAvgModeWhenReopeningUi();
          _RestoreAvgUiCanvasInteraction();
          _GetAvgUiFadeAnim().FadeIn();
        } else {
          _GetAvgUiFadeAnim().FadeOut(() => {
            _MarkAvgUiHidden();
            _ClearSegmentViews();
          });
        }
        return;
      }
      if (visible) {
        _ResetAvgModeWhenReopeningUi();
        _ApplyAvgUiShownImmediate();
      } else {
        _ApplyAvgUiHiddenImmediate();
        _ClearSegmentViews();
      }
    }
    
    /// <returns>是否成功载入章节并已开始播放</returns>
    public bool TryStartChapter(string chapterId) {
      InitIfNot();
      _EnsureAvgUiCanvasGroup();
      if (!m_model.NextChapter(chapterId)) {
        Debug.LogError($"无法开始章节: {chapterId}");
        return false;
      }
      _ResetAvgModeWhenReopeningUi();
      if (_HasFadeTarget()) {
        _PrepareAvgUiCanvasForContentBeforeFadeIn();
        _UpdateViews();
        _GetAvgUiFadeAnim().FadeIn();
      } else {
        _ApplyAvgUiShownImmediate();
        _UpdateViews();
      }
      return true;
    }

    public void StartChapter(string chapterId) {
      TryStartChapter(chapterId);
    }

    public void OnNextButtonClicked() {
      _OnNextButtonClicked();
    }
    
    public void OnOptionSelected(int optionIndex) {
      _OnOptionSelected(optionIndex);
    }
    
    public void OnLogToggled(bool isOn) {
      if (_enableDebugLogs) {
        Debug.Log($"[AvgController] OnLogToggled: isOn ={ isOn}");
      }
      _logView.gameObject.SetActive(isOn);
      if (isOn) {
        avgMode = AvgMode.DEFAULT;
        _logView.Render(m_log);
      }
    }
    
    public void JumpToLog(AvgLogLineModel logLineData) {
      InitIfNot();
      if (logLineData == null) {
        Debug.LogWarning("跳转的log数据为空");
        return;
      }
      
      string chapterId = logLineData.chapterId;
      string dialogId = logLineData.dialogId;
      
      if (string.IsNullOrEmpty(dialogId)) {
        Debug.LogWarning("跳转的dialogId为空");
        return;
      }
      
      // 跳转章节
      if (!string.IsNullOrEmpty(chapterId) && currChapterId != chapterId) {
        if (!m_model.NextChapter(chapterId)) {
          Debug.LogError($"无法加载章节: {chapterId}");
          return;
        }
      }

      // 跳转对话
      int targetIndex = m_log.FindIndex(chapterId, dialogId);

      if (m_model.JumpToDialog(dialogId)) {
        _StopAutoPlayWaitCoroutine();
        _StopTypingCoroutine();

        if (targetIndex >= 0) {
          m_log.ClearAfter(targetIndex);
        }

        avgMode = AvgMode.DEFAULT;

        _EnsureAvgUiCanvasGroup();
        if (_HasFadeTarget()) {
          // 全不透明：直接换内容，不必淡入
          if (_IsAvgUiCanvasAlreadyOpaque()) {
            _UpdateViews();
            _logView.gameObject.SetActive(false);
          } else {
            _PrepareAvgUiCanvasForContentBeforeFadeIn();
            _UpdateViews();
            _logView.gameObject.SetActive(false);
            _GetAvgUiFadeAnim().FadeIn();
          }
        } else {
          _ApplyAvgUiShownImmediate();
          _UpdateViews();
          _logView.gameObject.SetActive(false);
        }
        _avgUiWasHidden = false;
      }
    }
    #endregion
    
    private void _OnNextButtonClicked() {
      switch (m_currentState) {
        case AvgState.TYPING:
          _FillTextImmediately();
          return;
        
        case AvgState.WAITING:
          _StopAutoPlayWaitCoroutine();
          _PlayNextDialog();
          return;
      }
    }

    private void _OnOptionSelected(int optionIndex) {
      if (m_currentState != AvgState.WAITING) {
        return;
      }

      _UpdateLastLogOption(optionIndex);
      _ExecuteOptionEvents(optionIndex);
      
      if (m_model.SelectOption(optionIndex)) {
        _UpdateViews();
      } else {
        _EndChapterPlaybackUi();
      }
    }
    
    private void _LoadDialogView() {
      AvgDialogView prefab = Resources.Load<AvgDialogView>(_dialogViewPrefabName);
      if (prefab == null) {
        Debug.LogError($"无法加载DialogView Prefab: {_dialogViewPrefabName}，请确保Prefab在Resources文件夹中");
        return;
      }
      m_dialogView = Instantiate(prefab, _dialogViewParent);
      m_dialogView.InitIfNot();
    }
    
    private void _ChangeState(AvgState newState) {
      if (m_currentState == newState) {
        return;
      }

      if (_enableDebugLogs) {
        Debug.Log($"[AvgController] State changed: {m_currentState} -> {newState}");
      }
      m_currentState = newState;
      
      _StopTypingCoroutine();
      _StopAutoPlayWaitCoroutine();
      
      if (newState == AvgState.WAITING) {
        _StartAutoPlayIfNeeded();
      } 
    }
    
    private void _OnModeChanged() {
      if (m_currentState == AvgState.WAITING) {
        _StopAutoPlayWaitCoroutine(); // 先停止旧的等待协程，再根据新模式决定是否启动新的
        _StartAutoPlayIfNeeded();
        return;
      }

      if (m_currentState == AvgState.TYPING) {
        if (avgMode == AvgMode.FAST) {
          _FillTextImmediately();
        }
      }
    }
    
    private void _UpdateViews() {
      if (m_model?.currDlg != null) {
        _AddCurrentDialogToLog();
        _ExecuteDialogActions(m_model.currDlg);
        _AddCurrDialogToReadHistory();
        
        m_dialogView.Render(m_model.currDlg);
        _imageView.Render(m_model.currDlg);
        
        string dialogText = AvgLocale.Pick(
            m_model.currDlg.dialogData.dlgText,
            m_model.currDlg.dialogData.dlgTextEN);
        _ChangeState(AvgState.TYPING);
        m_typingCoroutine = StartCoroutine(_TypeText(dialogText));
      } else {
        _ChangeState(AvgState.IDLE);
      }
    }

    private void _PlayNextDialog() {
      if (m_model.NextDialog()) {
        _UpdateViews();
      } else {
        _EndChapterPlaybackUi();
      }
    }

    /// <summary>一段 AVG 播完：先发事件、再淡出并在结束后清空立绘/CG/对白等。</summary>
    private void _EndChapterPlaybackUi() {
      _NotifyChapterPlaybackEnded();
      _ChangeState(AvgState.IDLE);
      _KillAvgUiFadeTween();
      if (_HasFadeTarget()) {
        _GetAvgUiFadeAnim().FadeOut(() => {
          _MarkAvgUiHidden();
          _ClearSegmentViews();
        });
      } else {
        _ApplyAvgUiHiddenImmediate();
        _ClearSegmentViews();
      }
    }

    private void _MarkAvgUiHidden() {
      _avgUiWasHidden = true;
    }

    /// <summary>主界面从隐藏再次显示时，自动/快放恢复为手动（不改变已打开的日志面板状态）。</summary>
    private void _ResetAvgModeWhenReopeningUi() {
      if (!_avgUiWasHidden) {
        return;
      }
      _avgUiWasHidden = false;
      if (m_currentMode == AvgMode.AUTO || m_currentMode == AvgMode.FAST) {
        avgMode = AvgMode.DEFAULT;
      }
    }

    private void _EnsureAvgUiCanvasGroup() {
      if (_avgUiRoot == null) {
        return;
      }
      if (_avgUiCanvasGroup == null) {
        _avgUiCanvasGroup = _avgUiRoot.GetComponent<CanvasGroup>();
        if (_avgUiCanvasGroup == null) {
          _avgUiCanvasGroup = _avgUiRoot.AddComponent<CanvasGroup>();
        }
      }
    }

    private bool _HasFadeTarget() {
      return _avgUiCanvasGroup != null;
    }

    /// <summary>主面板已在场景中且透明度已满时，跳过 FadeIn（避免先把 alpha 置 0 再淡入造成闪烁）。</summary>
    private bool _IsAvgUiCanvasAlreadyOpaque() {
      if (_avgUiCanvasGroup == null) {
        return false;
      }
      if (!_avgUiCanvasGroup.gameObject.activeInHierarchy) {
        return false;
      }
      return _avgUiCanvasGroup.alpha >= 0.99f;
    }

    /// <summary>
    /// 淡入前先刷新对白/立绘时调用：保证根激活、alpha=0、可交互，避免「先淡入再 _UpdateViews」导致淡入过程中画面跳变。
    /// </summary>
    private void _PrepareAvgUiCanvasForContentBeforeFadeIn() {
      _KillAvgUiFadeTween();
      if (_avgUiCanvasGroup == null) {
        return;
      }
      if (_avgUiRoot != null) {
        _avgUiRoot.SetActive(true);
      }
      _avgUiCanvasGroup.alpha = 0f;
      _RestoreAvgUiCanvasInteraction();
    }

    private FadeAnimation _GetAvgUiFadeAnim() {
      if (_avgUiCanvasGroup == null) {
        return null;
      }
      if (_avgUiFadeAnim == null) {
        _avgUiFadeAnim = new FadeAnimation(_avgUiCanvasGroup, _avgUiFadeInDuration, _avgUiFadeOutDuration,
          DG.Tweening.Ease.OutQuad, DG.Tweening.Ease.InQuad, visibleAlphaWhenShown: 1f);
      }
      return _avgUiFadeAnim;
    }

    private void _KillAvgUiFadeTween() {
      _avgUiFadeAnim?.Kill();
    }

    /// <summary>隐藏 AVG 时会把 CanvasGroup 设为不可交互；淡入只动 alpha，故在 FadeIn 前须恢复，否则无法点击。</summary>
    private void _RestoreAvgUiCanvasInteraction() {
      if (_avgUiCanvasGroup == null) {
        return;
      }
      _avgUiCanvasGroup.interactable = true;
      _avgUiCanvasGroup.blocksRaycasts = true;
    }

    private void _ApplyAvgUiShownImmediate() {
      if (_avgUiCanvasGroup != null) {
        _avgUiCanvasGroup.alpha = 1f;
        _RestoreAvgUiCanvasInteraction();
      }
      if (_avgUiRoot != null) {
        _avgUiRoot.SetActive(true);
        return;
      }
      if (_imageView != null) {
        _imageView.gameObject.SetActive(true);
      }
      if (_menuView != null) {
        _menuView.gameObject.SetActive(true);
      }
      if (m_dialogView != null) {
        m_dialogView.gameObject.SetActive(true);
      }
    }

    private void _ApplyAvgUiHiddenImmediate() {
      if (_avgUiCanvasGroup != null) {
        _avgUiCanvasGroup.alpha = 0f;
        _avgUiCanvasGroup.interactable = false;
        _avgUiCanvasGroup.blocksRaycasts = false;
      }
      if (_avgUiRoot != null) {
        _avgUiRoot.SetActive(false);
      } else {
        if (_imageView != null) {
          _imageView.gameObject.SetActive(false);
        }
        if (_menuView != null) {
          _menuView.gameObject.SetActive(false);
        }
        if (m_dialogView != null) {
          m_dialogView.gameObject.SetActive(false);
        }
      }
      if (_logView != null) {
        _logView.gameObject.SetActive(false);
      }
      _MarkAvgUiHidden();
    }

    private void _ClearSegmentViews() {
      if (_imageView != null) {
        _imageView.ClearAllVisuals();
      }
      if (m_dialogView != null) {
        m_dialogView.ClearAfterSegment();
      }
    }
    
    private void _StartAutoPlayIfNeeded() {
      bool hasOptions = m_model.currDlg?.dialogData?.options?.Count > 0;
      bool isInReadHistory = dataManager?.readHistory?.Contains(currDialogId) ?? false;
      if (hasOptions || !isInReadHistory) {
        if (avgMode == AvgMode.FAST) {
          avgMode = AvgMode.DEFAULT;
        }
        return;
      }
      
      if (avgMode == AvgMode.AUTO) {
        m_waitCoroutine = StartCoroutine(_AutoPlayWait(dataManager.globalSave.settings.autoWaitTime));
      } else if (avgMode == AvgMode.FAST) {
        m_waitCoroutine = StartCoroutine(_AutoPlayWait(dataManager.globalSave.settings.fastModeWaitTime));
      }
    }

    private void _FillTextImmediately() {
      _StopTypingCoroutine();
      string fullText = m_model?.currDlg?.dialogData != null
          ? AvgLocale.Pick(m_model.currDlg.dialogData.dlgText, m_model.currDlg.dialogData.dlgTextEN)
          : "";
      m_dialogView?.SetDialogText(fullText);
        
      _ChangeState(AvgState.WAITING);
      _NotifyDialogLineRevealComplete();
    }

    private void _NotifyDialogLineRevealComplete() {
      string c = currChapterId ?? "";
      string d = currDialogId ?? "";
      DialogLineRevealComplete?.Invoke(c, d);
      if (_onDialogLineRevealComplete != null)
        _onDialogLineRevealComplete.Invoke(c, d);
      if (m_model != null && !m_model.HasFollowingContent()) {
        FinalDialogLineRevealComplete?.Invoke(c, d);
        if (_onFinalDialogLineRevealComplete != null)
          _onFinalDialogLineRevealComplete.Invoke(c, d);
      }
    }

    private void _NotifyChapterPlaybackEnded() {
      string ch = currChapterId ?? "";
      ChapterPlaybackEnded?.Invoke(ch);
      if (_onChapterPlaybackEnded != null)
        _onChapterPlaybackEnded.Invoke(ch);
    }

    #region IEnumerator
    private IEnumerator _AutoPlayWait(float waitTime) {
      yield return new WaitForSeconds(waitTime);
      if (m_currentState == AvgState.WAITING) {
        _PlayNextDialog();
      }
    }
    
    private IEnumerator _TypeText(string fullText) {
      m_dialogView.SetDialogText("");
      
      if (avgMode == AvgMode.FAST) {
        m_dialogView.SetDialogText(fullText);
        _ChangeState(AvgState.WAITING);
        m_typingCoroutine = null;
        _NotifyDialogLineRevealComplete();
        yield break;
      }

      for (int i = 0; i < fullText.Length; i++) {
        m_dialogView.SetDialogText(fullText.Substring(0, i + 1));
        yield return new WaitForSeconds(dataManager.globalSave.settings.typingSpeed);
      }
      
      _ChangeState(AvgState.WAITING);
      _NotifyDialogLineRevealComplete();
    }
    
    private void _StopAutoPlayWaitCoroutine() {
      if (m_waitCoroutine != null) {
        StopCoroutine(m_waitCoroutine);
        m_waitCoroutine = null;
      }
    }
    
    private void _StopTypingCoroutine() {
      if (m_typingCoroutine != null) {
        StopCoroutine(m_typingCoroutine);
        m_typingCoroutine = null;
      }
    }
    #endregion

    #region Log
    private void _AddCurrentDialogToLog() {
      AvgLogLineModel logLine = new AvgLogLineModel();
      logLine.Fill(currChapterId, m_model.currDlg);
      m_log.AddLog(logLine);
    }
    
    private void _UpdateLastLogOption(int selectedOptionIndex) {
      AvgLogLineModel lastLog = m_log.GetLast();
      if (lastLog == null || lastLog.options == null) {
        return;
      }
      
      if (selectedOptionIndex >= 0 && selectedOptionIndex < lastLog.options.Count) {
        lastLog.options[selectedOptionIndex].selected = true;
      }
    }
    #endregion

    #region Vars, Flags, Events
    private void _ExecuteOptionEvents(int optionIndex) {
      if (m_model.currDlg == null || m_model.currDlg.optionEvents == null) {
        return;
      }
      
      if (m_model.currDlg.optionEvents.TryGetValue(optionIndex, out List<EventData> events)) {
        foreach (EventData eventData in events) {
          AvgUtil.OnAvgEvent(eventData);
        }
      }
    }
    
    private void _ExecuteDialogActions(AvgDialogModel dlgModel) {
      DialogData dlgData = dlgModel?.dialogData;
      
      // 设置变量
      if (dlgData?.setVars != null) {
        foreach (SetVar setVar in dlgData.setVars) {
          AvgDataUtil.SetVariable(setVar);
        }
      }
      
      // 设置flag
      if (dlgData?.setFlags != null) {
        foreach (string flagName in dlgData.setFlags) {
          AvgDataUtil.AddFlag(flagName);
        }
      }
      
      // 执行事件
      if (dlgModel?.events != null) {
        foreach (EventData eventData in dlgModel.events) {
          AvgUtil.OnAvgEvent(eventData);
        }
      }
    }
    #endregion

    private void _AddCurrDialogToReadHistory() {
      var readHistory = dataManager.globalSave.readHistory;
      readHistory.Add(currDialogId);
    }
  }
}
