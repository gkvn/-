using UnityEngine;
using UnityEngine.UI;
using System;

namespace AVG {
  public class AvgMenuView : MonoBehaviour {
    [SerializeField] 
    private TwoStateButtonView _btnAutoPlay;
    [SerializeField] 
    private TwoStateButtonView _btnFastForward;
    [SerializeField] 
    private Button _btnLog;
    
    private bool m_isInited;
    
    public void InitIfNot() {
      if (m_isInited) {
        return;
      }
      
      m_isInited = true;
      
      if (_btnAutoPlay != null) {
        _btnAutoPlay.SetBtnAction(_OnAutoPlayBtnClicked);
      }
      
      if (_btnFastForward != null) {
        _btnFastForward.SetBtnAction(_OnFastForwardBtnClicked);
      }
      
      if (_btnLog != null) {
        _btnLog.onClick.AddListener(_OnLogBtnClicked);
      }

      RenderBtns();
    }

    public void RenderBtns() {
      var controller = AvgController.Instance;
      if (controller == null) {
        return;
      }
      _btnAutoPlay.Render(controller.avgMode == AvgMode.AUTO);
      _btnFastForward.Render(controller.avgMode == AvgMode.FAST);
    }

    private void _OnAutoPlayBtnClicked() {
      var controller = AvgController.Instance;
      if (controller == null) {
        return;
      }
      if (controller.avgMode == AvgMode.DEFAULT) {
        controller.avgMode = AvgMode.AUTO;
      } else if (controller.avgMode == AvgMode.AUTO) {
        controller.avgMode = AvgMode.DEFAULT;
      }
    }
    
    private void _OnFastForwardBtnClicked() {
      var controller = AvgController.Instance;
      if (controller == null) {
        return;
      }
      if (controller.avgMode == AvgMode.FAST) {
        controller.avgMode = _btnAutoPlay.isOn ? AvgMode.AUTO : AvgMode.DEFAULT;
      } else {
        controller.avgMode = AvgMode.FAST;
      }
    }

    private void _OnLogBtnClicked() {
      AvgController.Instance?.OnLogToggled(true);
    }
  }
}
