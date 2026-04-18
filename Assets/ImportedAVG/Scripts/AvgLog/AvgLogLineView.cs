using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace AVG {
  public class AvgLogLineView : MonoBehaviour {
    [SerializeField] 
    private Text _charName;
    [SerializeField] 
    private Text _text;
    [SerializeField] 
    private Transform _optionsContainer;
    [SerializeField] 
    private Text _optionPrefab;
    [SerializeField] 
    private LayoutGroup[] _layouts;
    [SerializeField]
    private Button _btnJump;
    
    private bool m_isInited;
    private List<Text> m_optionViews = new List<Text>();
    private AvgLogLineModel m_currentLineData;
    
    public void Render(AvgLogLineModel lineData) {
      _InitIfNot();
      
      if (lineData == null) {
        Hide();
        return;
      }
      
      m_currentLineData = lineData;
      
      bool hasCharName = lineData.isCharTalk && !string.IsNullOrEmpty(lineData.charName);
      if (_charName != null) {
        _charName.gameObject.SetActive(hasCharName);
        if (hasCharName) {
          _charName.text = lineData.charName;
        }
      }

      if (_text != null) {
        _text.text = lineData.text ?? "";
      }
      
      if (lineData.options != null && lineData.options.Count > 0) {
        _RenderOptions(lineData.options);
      } else {
        _HideOptions();
      }
      
      gameObject.SetActive(true);

      foreach (var layout in _layouts) {
        if (layout != null) {
          LayoutRebuilder.ForceRebuildLayoutImmediate(layout.GetComponent<RectTransform>());
        }
      }
    }
    
    public void Hide() {
      gameObject.SetActive(false);
    }
    
    private void _InitIfNot() {
      if (m_isInited) {
        return;
      }
      m_isInited = true;
      
      if (_btnJump != null) {
        _btnJump.onClick.AddListener(_OnJumpButtonClicked);
      }
    }
    
    private void _OnJumpButtonClicked() {
      AvgController.Instance?.JumpToLog(m_currentLineData);
    }
    
    private void _RenderOptions(List<AvgLogOptionModel> options) {
      if (options == null || options.Count == 0) {
        _HideOptions();
        return;
      }
      
      if (_optionsContainer == null || _optionPrefab == null) {
        return;
      }
      
      while (m_optionViews.Count < options.Count) {
        Text optionView = Instantiate(_optionPrefab, _optionsContainer);
        m_optionViews.Add(optionView);
      }
      
      for (int i = 0; i < options.Count; i++) {
        if (i < m_optionViews.Count) {
          AvgLogOptionModel option = options[i];
          Text optionText = m_optionViews[i];
          string prefix = option.selected ? "✓ " : "  ";
          optionText.text = prefix + (option.text ?? "");
          optionText.gameObject.SetActive(true);
        }
      }
      
      for (int i = options.Count; i < m_optionViews.Count; i++) {
        m_optionViews[i].gameObject.SetActive(false);
      }
    }
    
    private void _HideOptions() {
      foreach (var optionView in m_optionViews) {
        if (optionView != null) {
          optionView.gameObject.SetActive(false);
        }
      }
    }
  }
}