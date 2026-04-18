using UnityEngine;
using UnityEngine.UI;

using System.Collections;
using System.Collections.Generic;

namespace AVG {
  public class AvgLogView : MonoBehaviour {
    [SerializeField] 
    private AvgLogLineView _linePrefab;
    [SerializeField] 
    private Button _btnBack;
    [SerializeField]
    private VirtualScrollList _virtualScrollList;
    [SerializeField]
    private ScrollRect _scrollRect;
    
    private bool m_isInited;
    private List<AvgLogLineModel> m_logDataList = new List<AvgLogLineModel>();
    
    public void Render(AvgLog log) {
      _InitIfNot();
      
      if (log == null) {
        _ClearAll();
        return;
      }
      
      // 复制 log 数据到列表
      m_logDataList.Clear();
      m_logDataList.AddRange(log.GetAll());
      
      // 使用虚拟滚动列表设置数据，完成后滚动到底部
      _virtualScrollList.SetData(m_logDataList, _ScrollToBottom);
    }
    
    public void Clear() {
      _ClearAll();
    }
    
    private void _InitIfNot() {
      if (m_isInited) {
        return;
      }
      m_isInited = true;
      
      _btnBack.onClick.AddListener(_OnBtnBackClicked);
      
      // 初始化虚拟滚动列表
      _virtualScrollList.Init(_linePrefab.gameObject, _OnUpdateLogLineItem);
      _virtualScrollList.EnableMeasureMode();
    }
    
    private void _OnUpdateLogLineItem(GameObject itemGo, object logDataObj, int index) {
      // 类型转换
      if (logDataObj is AvgLogLineModel logData) {
        AvgLogLineView lineView = itemGo.GetComponent<AvgLogLineView>();
        if (lineView != null) {
          lineView.Render(logData);
        }
      }
    }
    
    private void _ClearAll() {
      m_logDataList.Clear();
      if (_virtualScrollList != null) {
        _virtualScrollList.SetData(m_logDataList);
      }
    }
    
    private void _ScrollToBottom() {
      if (_scrollRect != null) {
        _scrollRect.verticalNormalizedPosition = 0f; // 0 表示底部
      }
    }
    
    private void _OnBtnBackClicked() {
      AvgController.Instance?.OnLogToggled(false);
    }
  }
}
