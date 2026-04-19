using UnityEngine;
using UnityEngine.UI;

using System;

namespace AVG {
  public class AvgOptionView : MonoBehaviour {
    [SerializeField] 
    private Text _optionText;
    [SerializeField]
    private Button _optionButton;
    
    private bool m_isInited;
    private int m_optionIndex;
    
    public void Render(DlgOption option, int index) {
      _InitIfNot();
      m_optionIndex = index;
      
      if (option != null) {
        _optionText.text = AvgLocale.Pick(option.optText, option.optTextEN);
      }
      
      gameObject.SetActive(option != null);
    }
    
    public void Hide() {
      gameObject.SetActive(false);
    }
    
    private void _InitIfNot() {
      if (m_isInited) {
        return;
      }
      m_isInited = true;
      _optionButton.onClick.AddListener(_OnButtonClicked);
    }
    
    private void _OnButtonClicked() {
      AvgController.Instance?.OnOptionSelected(m_optionIndex);
    }
  }
}
