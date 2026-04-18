using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

namespace AVG {

  public class FadeAnimation {
    private Graphic m_targetGraphic;
    private CanvasGroup m_targetCanvasGroup;
    private float m_fadeInDuration;
    private float m_fadeOutDuration;
    private Ease m_fadeInEase;
    private Ease m_fadeOutEase;
    private float m_originalAlpha;
    private Tween m_currentTween;
    private bool m_useCanvasGroup;
    
    /// <param name="visibleAlphaWhenShown">
    /// 完全显示时的目标 alpha。&lt;0 时按构造时刻 CanvasGroup 的 alpha 取值（兼容立绘槽等）。
    /// 整页 AVG 等在初始化时往往会先把 alpha 设为 0，此时应传入 1，否则淡入目标仍为 0 导致看不见。
    /// </param>
    public FadeAnimation(CanvasGroup canvasGroup, 
                         float fadeInDuration = AvgUtil.DEFAULT_FADE_TIME, 
                         float fadeOutDuration = AvgUtil.DEFAULT_FADE_TIME, 
                         Ease fadeInEase = Ease.OutQuad, Ease fadeOutEase = Ease.InQuad,
                         float visibleAlphaWhenShown = -1f) {
      if (canvasGroup == null) {
        throw new ArgumentNullException(nameof(canvasGroup), "CanvasGroup cannot be null");
      }
      
      m_targetCanvasGroup = canvasGroup;
      m_useCanvasGroup = true;
      m_originalAlpha = visibleAlphaWhenShown >= 0f ? visibleAlphaWhenShown : canvasGroup.alpha;
      m_fadeInDuration = fadeInDuration;
      m_fadeOutDuration = fadeOutDuration;
      m_fadeInEase = fadeInEase;
      m_fadeOutEase = fadeOutEase;
    }
    
    public FadeAnimation(Graphic graphic, 
                         float fadeInDuration = AvgUtil.DEFAULT_FADE_TIME, 
                         float fadeOutDuration = AvgUtil.DEFAULT_FADE_TIME,
                         Ease fadeInEase = Ease.OutQuad, Ease fadeOutEase = Ease.InQuad) {
      if (graphic == null) {
        throw new ArgumentNullException(nameof(graphic), "Graphic cannot be null");
      }
      
      m_targetGraphic = graphic;
      m_useCanvasGroup = false;
      m_originalAlpha = graphic.color.a;
      m_fadeInDuration = fadeInDuration;
      m_fadeOutDuration = fadeOutDuration;
      m_fadeInEase = fadeInEase;
      m_fadeOutEase = fadeOutEase;
    }

    public void Kill() {
      _KillCurrentTween();
    }

    public void ResetShow(bool isShow) {
      _KillCurrentTween();
      if (m_useCanvasGroup) {
        m_targetCanvasGroup.alpha = isShow ? 1f : 0f;
      } else {
        Color color = m_targetGraphic.color;
        color.a = isShow ? 1f : 0f;
        m_targetGraphic.color = color;
      }
    }
    
    public Tween FadeIn(Action onComplete = null) {
      if (!_ValidateTarget()) {
        onComplete?.Invoke();
        return null;
      }
      
      _KillCurrentTween();
      _SetTargetGameObjectActive(true);
      
      if (m_useCanvasGroup) {
        m_targetCanvasGroup.alpha = 0f;
      } else {
        Color color = m_targetGraphic.color;
        color.a = 0f;
        m_targetGraphic.color = color;
      }
      
      if (m_useCanvasGroup) {
        m_currentTween = m_targetCanvasGroup.DOFade(m_originalAlpha, m_fadeInDuration)
          .SetEase(m_fadeInEase)
          .OnComplete(() => {
            m_currentTween = null;
            onComplete?.Invoke();
          });
      } else {
        m_currentTween = m_targetGraphic.DOFade(m_originalAlpha, m_fadeInDuration)
          .SetEase(m_fadeInEase)
          .OnComplete(() => {
            m_currentTween = null;
            onComplete?.Invoke();
          });
      }
      
      return m_currentTween;
    }
    
    public Tween FadeOut(Action onComplete = null) {
      if (!_ValidateTarget()) {
        onComplete?.Invoke();
        return null;
      }
      
      _KillCurrentTween();
      
      if (m_useCanvasGroup) {
        m_currentTween = m_targetCanvasGroup.DOFade(0f, m_fadeOutDuration)
          .SetEase(m_fadeOutEase)
          .OnComplete(() => {
            m_currentTween = null;
            _SetTargetGameObjectActive(false);
            onComplete?.Invoke();
          });
      } else {
        m_currentTween = m_targetGraphic.DOFade(0f, m_fadeOutDuration)
          .SetEase(m_fadeOutEase)
          .OnComplete(() => {
            m_currentTween = null;
            _SetTargetGameObjectActive(false);
            onComplete?.Invoke();
          });
      }
      
      return m_currentTween;
    }
    
    public Sequence FadeOutThenIn(Action onFadeOutComplete = null, Action onFadeInComplete = null) {
      if (!_ValidateTarget()) {
        onFadeOutComplete?.Invoke();
        onFadeInComplete?.Invoke();
        return null;
      }
      
      _KillCurrentTween();
      
      Sequence sequence = DOTween.Sequence();
      
      if (m_useCanvasGroup) {
        sequence.Append(m_targetCanvasGroup.DOFade(0f, m_fadeOutDuration).SetEase(m_fadeOutEase));
      } else {
        sequence.Append(m_targetGraphic.DOFade(0f, m_fadeOutDuration).SetEase(m_fadeOutEase));
      }
      
      sequence.AppendCallback(() => {
        onFadeOutComplete?.Invoke();
      });
      
      if (m_useCanvasGroup) {
        sequence.Append(m_targetCanvasGroup.DOFade(m_originalAlpha, m_fadeInDuration).SetEase(m_fadeInEase));
      } else {
        sequence.Append(m_targetGraphic.DOFade(m_originalAlpha, m_fadeInDuration).SetEase(m_fadeInEase));
      }
      
      sequence.OnComplete(() => {
        m_currentTween = null;
        onFadeInComplete?.Invoke();
      });
      
      m_currentTween = sequence;
      return sequence;
    }

    private void _SetTargetGameObjectActive(bool active) {
      GameObject targetGameObject = m_useCanvasGroup ? m_targetCanvasGroup.gameObject : m_targetGraphic.gameObject;
      targetGameObject.SetActive(active);
    }
    
    private bool _ValidateTarget() {
      if (m_useCanvasGroup) {
        if (m_targetCanvasGroup == null) {
          Debug.LogWarning("FadeAnimation: Target CanvasGroup is null");
          return false;
        }
      } else {
        if (m_targetGraphic == null) {
          Debug.LogWarning("FadeAnimation: Target Graphic is null");
          return false;
        }
      }
      return true;
    }
    
    private void _KillCurrentTween() {
      if (m_currentTween != null && m_currentTween.IsActive()) {
        m_currentTween.Kill();
      }
      m_currentTween = null;
    }
  }
}

