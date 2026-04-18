using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace AVG {
  public class AvgImageView : MonoBehaviour {
  [SerializeField] 
  private Image _cg;
  [SerializeField] 
  private AvgCharImageView[] _charSlots;
  
  
  public void Render(AvgDialogModel model) {
    if (model == null || model.dialogData == null) {
      _ClearAllCharacters();
      return;
    }
    DialogData dialog = model.dialogData;
    
    _UpdateCGImage(dialog.cgId);
    
    for (int i = 0; i < _charSlots.Length; i++) {
      if (_charSlots[i] != null) {
        AvgCharImageModel charImageModel = null;
        model.charImageModels?.TryGetValue(i, out charImageModel);
        _charSlots[i].Render(charImageModel);
      }
    }
  }

  private void _UpdateCGImage(string cgId) {
    if (_cg == null) return;
    
    if (!string.IsNullOrEmpty(cgId)) {
      StartCoroutine(AvgUtil.LoadSpriteAsync(cgId, AvgUtil.ResourceType.CG, (sprite) => {
        if (sprite != null) {
          _cg.sprite = sprite;
          _cg.gameObject.SetActive(true);
        }
      }));
    }
  }
  
  private void _ClearAllCharacters() {
    for (int i = 0, n = _charSlots.Length; i < n; i++) {
      if (_charSlots[i] != null) {
        _charSlots[i].Render(null);
      }
    }
  }

  /// <summary>一段 AVG 结束后清空 CG 与所有角色立绘（立即，不播 slot 渐变）。</summary>
  public void ClearAllVisuals() {
    _ClearCg();
    for (int i = 0, n = _charSlots.Length; i < n; i++) {
      if (_charSlots[i] != null) {
        _charSlots[i].ClearImmediate();
      }
    }
  }

  private void _ClearCg() {
    if (_cg == null) {
      return;
    }
    _cg.sprite = null;
    _cg.gameObject.SetActive(false);
  }
}
}
