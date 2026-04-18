using UnityEngine;
using UnityEngine.UI;

using System;
using System.Collections.Generic;

namespace AVG {
  public class AvgDialogView : MonoBehaviour {
    [SerializeField] private Text _charName;
    [SerializeField] private Image _charAvatar;
    [SerializeField] private Text _dialogText;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Transform _optionsContainer;
    [SerializeField] private AvgOptionView _optionPrefab;

    private bool m_isInited;
    private List<AvgOptionView> m_optionViews = new List<AvgOptionView>();

    public void InitIfNot() {
      if (m_isInited) {
        return;
      }

      m_isInited = true;
      _nextButton.onClick.AddListener(_OnNextButtonClicked);
    }

    public void Render(AvgDialogModel model) {
      if (model == null || model.dialogData == null) {
        _HideAll();
        return;
      }

      _dialogText.text = "";

      bool hasOptions = model.dialogData.options != null && model.dialogData.options.Count > 0;
      _nextButton.interactable = !hasOptions;

      if (hasOptions) {
        _RenderOptions(model.dialogData.options);
      }
      else {
        _HideOptions();
      }
      
      bool hasCharTalking = !string.IsNullOrEmpty(model.talkingCharName);
      _charName.gameObject.SetActive(hasCharTalking);
      _charAvatar.gameObject.SetActive(hasCharTalking);
      if (hasCharTalking) {
        _charName.text = model.talkingCharName ?? "";
        Sprite avatarSprite = AvgUtil.LoadSprite(model.talkingCharAvatar, AvgUtil.ResourceType.Avatar);
        if (avatarSprite == null && model.charImageModels != null) {
          foreach (var kv in model.charImageModels) {
            AvgCharImageModel slot = kv.Value;
            if (slot != null && slot.isTalking && !string.IsNullOrEmpty(slot.charBody)) {
              avatarSprite = AvgUtil.LoadSprite(slot.charBody, AvgUtil.ResourceType.Avatar);
              break;
            }
          }
        }
        if (avatarSprite == null) {
          _charAvatar.gameObject.SetActive(false);
        }
        else {
          _charAvatar.sprite = avatarSprite;
        }
      }
    }
    
    public void SetDialogText(string text) {
      if (_dialogText != null) {
        _dialogText.text = text ?? "";
      }
    }

    private void _RenderOptions(List<DlgOption> options) {
      if (options == null || options.Count == 0) {
        _HideOptions();
        return;
      }

      while (m_optionViews.Count < options.Count) {
        AvgOptionView optionView = Instantiate(_optionPrefab, _optionsContainer);
        m_optionViews.Add(optionView);
      }

      for (int i = 0; i < options.Count; i++) {
        if (i < m_optionViews.Count) {
          m_optionViews[i].Render(options[i], i);
        }
      }

      for (int i = options.Count; i < m_optionViews.Count; i++) {
        m_optionViews[i].Hide();
      }
    }

    private void _HideOptions() {
      foreach (var optionView in m_optionViews) {
        optionView.Hide();
      }
    }

    private void _HideAll() {
      gameObject.SetActive(false);
      _charName.text = "";
      _dialogText.text = "";
      _HideOptions();
    }

    /// <summary>一段 AVG 结束后清空对白区与头像，不切换本物体 active（由上层 Canvas 显隐）。</summary>
    public void ClearAfterSegment() {
      SetDialogText("");
      if (_charName != null) {
        _charName.text = "";
        _charName.gameObject.SetActive(false);
      }
      if (_charAvatar != null) {
        _charAvatar.sprite = null;
        _charAvatar.gameObject.SetActive(false);
      }
      _HideOptions();
      if (_nextButton != null) {
        _nextButton.interactable = true;
      }
    }

    private void _OnNextButtonClicked() {
      AvgController.Instance?.OnNextButtonClicked();
    }
  }
}

