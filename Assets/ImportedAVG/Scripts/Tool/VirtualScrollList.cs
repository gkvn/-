using UnityEngine;
using UnityEngine.UI;

using System;
using System.Collections;
using System.Collections.Generic;

namespace AVG {
  public class VirtualScrollList : MonoBehaviour {
    [SerializeField] 
    private ScrollRect _scrollRect;
    [SerializeField] 
    private RectTransform _content;
    [SerializeField] 
    private float _defaultItemHeight = 100f;
    [SerializeField]
    private int _bufferCount = 3;
    [SerializeField]
    [Tooltip("item 之间的间距")]
    private float _spacing = 0f;
    [SerializeField]
    [Tooltip("是否输出调试日志")]
    private bool _enableDebugLog = false;

    private GameObject m_itemPrefab;
    private List<GameObject> m_pool = new List<GameObject>();
    private List<object> m_dataList = new List<object>();
    private int m_startIndex = -1;
    private int m_endIndex = -1;
    
    // 动态高度相关
    private float[] m_itemHeights;      // 每个 item 的高度
    private float[] m_itemPositions;    // 每个 item 的 Y 位置（累计）
    private float m_totalHeight;
    private float m_viewportHeight;
    
    // 测量模式
    private bool m_useMeasureMode;

    // 外部注入的更新逻辑
    public Action<GameObject, object, int> OnUpdateItem;
    
    // 外部注入的高度计算逻辑（可选，不设置则使用默认高度）
    public Func<object, int, float> OnGetItemHeight;

    public void Init(GameObject prefab, Action<GameObject, object, int> updateCallback) {
      m_itemPrefab = prefab;
      OnUpdateItem = updateCallback;
      
      m_viewportHeight = _scrollRect.viewport.rect.height;

      _scrollRect.onValueChanged.AddListener(_ => _Refresh());
    }
    
    /// <summary>
    /// 设置高度计算回调
    /// </summary>
    public void SetHeightCallback(Func<object, int, float> getHeightCallback) {
      OnGetItemHeight = getHeightCallback;
    }
    
    /// <summary>
    /// 设置 item 之间的间距
    /// </summary>
    public void SetSpacing(float spacing) {
      _spacing = spacing;
    }
    
    /// <summary>
    /// 启用测量模式
    /// 通过 SetData + ForceRebuildLayoutImmediate + LayoutUtility.GetPreferredHeight 获取高度
    /// 要求：item prefab 的根节点需要有 LayoutGroup 组件（如 VerticalLayoutGroup）
    /// </summary>
    public void EnableMeasureMode() {
      m_useMeasureMode = true;
    }

    public void SetData(List<object> data, Action onComplete = null) {
      m_dataList = data;
      
      if (m_useMeasureMode) {
        StartCoroutine(_SetDataWithMeasure(onComplete));
      } else {
        _CalculateHeights();
        _EnsurePoolSize();
        m_startIndex = -1;
        m_endIndex = -1;
        _Refresh();
        onComplete?.Invoke();
      }
    }
    
    /// <summary>
    /// 泛型版本的SetData，方便使用强类型List
    /// </summary>
    public void SetData<T>(List<T> data, Action onComplete = null) {
      if (data == null) {
        SetData(new List<object>(), onComplete);
        return;
      }
      // 将泛型List转换为object List
      List<object> objectList = new List<object>(data.Count);
      for (int i = 0; i < data.Count; i++) {
        objectList.Add(data[i]);
      }
      SetData(objectList, onComplete);
    }
    
    private IEnumerator _SetDataWithMeasure(Action onComplete = null) {
      yield return StartCoroutine(_MeasureAllHeights());
      _EnsurePoolSize();
      m_startIndex = -1;
      m_endIndex = -1;
      _Refresh();
      onComplete?.Invoke();
    }
    
    /// <summary>
    /// 当某个 item 的高度发生变化时调用，重新计算布局
    /// </summary>
    public void NotifyHeightChanged(int index) {
      if (index < 0 || index >= m_dataList.Count) {
        return;
      }
      
      float oldHeight = m_itemHeights[index];
      float newHeight = _GetItemHeight(index);
      
      if (Mathf.Approximately(oldHeight, newHeight)) {
        return;
      }
      
      m_itemHeights[index] = newHeight;
      _RecalculatePositions(index);
      _UpdateContentSize();
      
      // 强制刷新
      m_startIndex = -1;
      m_endIndex = -1;
      _Refresh();
    }
    
    /// <summary>
    /// 使用测量模式重新测量指定 item 的高度
    /// </summary>
    public void RemeasureHeight(int index) {
      if (!m_useMeasureMode || index < 0 || index >= m_dataList.Count) {
        return;
      }
      
      _RemeasureHeightInternal(index);
    }
    
    private void _RemeasureHeightInternal(int index) {
      float newHeight = _MeasureItemHeightSync(m_dataList[index], index);
      
      if (!Mathf.Approximately(m_itemHeights[index], newHeight)) {
        m_itemHeights[index] = newHeight;
        _RecalculatePositions(index);
        _UpdateContentSize();
        
        m_startIndex = -1;
        m_endIndex = -1;
        _Refresh();
      }
    }
    
    /// <summary>
    /// 重新计算所有高度（当数据内容变化时调用）
    /// </summary>
    public void RecalculateAllHeights() {
      if (m_useMeasureMode) {
        StartCoroutine(_SetDataWithMeasure());
      } else {
        _CalculateHeights();
        m_startIndex = -1;
        m_endIndex = -1;
        _Refresh();
      }
    }
    
    private IEnumerator _MeasureAllHeights() {
      int count = m_dataList.Count;
      m_itemHeights = new float[count];
      m_itemPositions = new float[count];
      
      m_totalHeight = 0f;
      
      // 分批测量，避免卡顿
      int batchSize = 10;
      for (int i = 0; i < count; i++) {
        float height = _MeasureItemHeightSync(m_dataList[i], i);
        
        m_itemHeights[i] = height;
        m_itemPositions[i] = m_totalHeight;
        m_totalHeight += height + _spacing;
        
        // 每批次后让出一帧，避免卡顿
        if ((i + 1) % batchSize == 0) {
          _UpdateContentSize();
          yield return null;
        }
      }
      
      // 移除最后一个 spacing
      if (count > 0) {
        m_totalHeight -= _spacing;
      }
      
      _UpdateContentSize();
    }
    
    private float _MeasureItemHeightSync(object data, int index) {
      // 确保 pool 中至少有一个 item 用于测量
      if (m_pool.Count == 0) {
        var item = Instantiate(m_itemPrefab, _content);
        _SetupPoolItem(item);
        item.SetActive(false);
        m_pool.Add(item);
      }
      
      // 使用 pool 中的第一个 item 进行测量
      var measureItem = m_pool[0];
      var rt = measureItem.GetComponent<RectTransform>();
      
      // 激活 item
      measureItem.SetActive(true);
      
      // 渲染数据（这会改变 Text 内容等，item 内部会调用 ForceRebuildLayoutImmediate）
      OnUpdateItem?.Invoke(measureItem, data, index);
      
      // 强制重建布局
      LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
      
      // 使用 LayoutUtility 获取首选高度
      float height = LayoutUtility.GetPreferredHeight(rt);
      if (height <= 0) {
        // 如果没有 LayoutGroup，尝试直接读取 rect.height
        height = rt.rect.height;
      }
      if (height <= 0) {
        height = _defaultItemHeight;
      }
      
      // 隐藏 item
      measureItem.SetActive(false);
      
      return height;
    }
    
    private void _CalculateHeights() {
      int count = m_dataList.Count;
      m_itemHeights = new float[count];
      m_itemPositions = new float[count];
      
      m_totalHeight = 0f;
      for (int i = 0; i < count; i++) {
        float height = _GetItemHeight(i);
        m_itemHeights[i] = height;
        m_itemPositions[i] = m_totalHeight;
        m_totalHeight += height + _spacing;
      }
      
      // 移除最后一个 spacing
      if (count > 0) {
        m_totalHeight -= _spacing;
      }
      
      _UpdateContentSize();
    }
    
    private void _RecalculatePositions(int fromIndex) {
      if (fromIndex > 0) {
        m_totalHeight = m_itemPositions[fromIndex - 1] + m_itemHeights[fromIndex - 1] + _spacing;
      } else {
        m_totalHeight = 0f;
      }
      
      for (int i = fromIndex; i < m_dataList.Count; i++) {
        m_itemPositions[i] = m_totalHeight;
        m_totalHeight += m_itemHeights[i] + _spacing;
      }
      
      // 移除最后一个 spacing
      if (m_dataList.Count > 0) {
        m_totalHeight -= _spacing;
      }
    }
    
    private void _UpdateContentSize() {
      _content.sizeDelta = new Vector2(_content.sizeDelta.x, m_totalHeight);
    }
    
    private float _GetItemHeight(int index) {
      if (OnGetItemHeight != null && index < m_dataList.Count) {
        return OnGetItemHeight(m_dataList[index], index);
      }
      return _defaultItemHeight;
    }
    
    private void _EnsurePoolSize() {
      // 估算可见数量：使用默认高度估算最大可能的可见数量
      int estimatedVisible = Mathf.CeilToInt(m_viewportHeight / _defaultItemHeight) + _bufferCount * 2;
      
      // 确保池中有足够的 item
      while (m_pool.Count < estimatedVisible) {
        var item = Instantiate(m_itemPrefab, _content);
        _SetupPoolItem(item);
        item.SetActive(false);
        m_pool.Add(item);
      }
    }
    
    private void _SetupPoolItem(GameObject item) {
      var rt = item.GetComponent<RectTransform>();
      // 设置 item 宽度撑满 content（确保测量和显示时宽度一致）
      rt.anchorMin = new Vector2(0, 1);
      rt.anchorMax = new Vector2(1, 1);
      rt.pivot = new Vector2(0.5f, 1);
      rt.sizeDelta = new Vector2(0, rt.sizeDelta.y);
    }

    private void _Refresh() {
      if (m_dataList.Count == 0 || m_itemPositions == null || m_itemPositions.Length == 0) {
        _HideAllItems();
        return;
      }
      
      float scrollY = _content.anchoredPosition.y;
      
      // 二分查找起始索引
      int newStartIndex = _FindStartIndex(scrollY);
      int newEndIndex = _FindEndIndex(scrollY + m_viewportHeight);
      
      // 添加缓冲
      newStartIndex = Mathf.Max(0, newStartIndex - _bufferCount);
      newEndIndex = Mathf.Min(m_dataList.Count - 1, newEndIndex + _bufferCount);
      
      if (newStartIndex == m_startIndex && newEndIndex == m_endIndex) {
        return;
      }
      
      m_startIndex = newStartIndex;
      m_endIndex = newEndIndex;
      
      // 确保池大小足够
      int visibleCount = m_endIndex - m_startIndex + 1;
      while (m_pool.Count < visibleCount) {
        var item = Instantiate(m_itemPrefab, _content);
        _SetupPoolItem(item);
        item.SetActive(false);
        m_pool.Add(item);
      }
      
      // 更新可见 items
      int poolIndex = 0;
      for (int dataIndex = m_startIndex; dataIndex <= m_endIndex; dataIndex++) {
        if (poolIndex >= m_pool.Count) break;
        
        var item = m_pool[poolIndex];
        item.SetActive(true);
        
        // 先渲染数据
        OnUpdateItem?.Invoke(item, m_dataList[dataIndex], dataIndex);
        
        var rt = item.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, -m_itemPositions[dataIndex]);
        
        // 使用预先计算好的高度设置 sizeDelta（x=0 表示宽度撑满 content）
        rt.sizeDelta = new Vector2(0, m_itemHeights[dataIndex]);
        
        poolIndex++;
      }
      
      // 隐藏多余的 items
      for (int i = poolIndex; i < m_pool.Count; i++) {
        m_pool[i].SetActive(false);
      }
      
      if (_enableDebugLog) {
        Debug.Log($"[VirtualScrollList] 对象池大小: {m_pool.Count}, 显示中: {poolIndex}, 数据总数: {m_dataList.Count}, 范围: [{m_startIndex}-{m_endIndex}]");
      }
    }
    
    private int _FindStartIndex(float scrollY) {
      if (m_dataList.Count == 0) return 0;
      
      // 二分查找第一个位置 >= scrollY 的 item
      int left = 0;
      int right = m_dataList.Count - 1;
      
      while (left < right) {
        int mid = (left + right) / 2;
        float itemBottom = m_itemPositions[mid] + m_itemHeights[mid];
        
        if (itemBottom <= scrollY) {
          left = mid + 1;
        } else {
          right = mid;
        }
      }
      
      return left;
    }
    
    private int _FindEndIndex(float scrollBottom) {
      if (m_dataList.Count == 0) return 0;
      
      // 二分查找最后一个位置 <= scrollBottom 的 item
      int left = 0;
      int right = m_dataList.Count - 1;
      
      while (left < right) {
        int mid = (left + right + 1) / 2;
        
        if (m_itemPositions[mid] >= scrollBottom) {
          right = mid - 1;
        } else {
          left = mid;
        }
      }
      
      return left;
    }
    
    private void _HideAllItems() {
      foreach (var item in m_pool) {
        item.SetActive(false);
      }
    }
    
  }
}
