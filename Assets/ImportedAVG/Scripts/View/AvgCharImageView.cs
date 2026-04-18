using UnityEngine;
using UnityEngine.UI;

namespace AVG {
  public class AvgCharImageView : MonoBehaviour {
	  [SerializeField] 
	  private CanvasGroup _canvasGroup;
		[SerializeField] 
		private Image _charBody;
		[Tooltip("同一物体上可挂载 LayoutElement；仅在 VisualRoot SetActive 时同步 ignoreLayout（关/隐藏时为 true）。")]
		[SerializeField]
		private LayoutElement _layoutElement;
		[SerializeField] 
		private Image _charFace;
		[SerializeField]
		private Color _normalColor = Color.white;
		[SerializeField]
		private Color _talkingColor = Color.white;

		private bool m_isInited;
		private bool m_cachedIsTalking;
		private string m_cachedCharBody;
		private string m_cachedCharFace;
		
		private FadeAnimation m_slotFadeAnimation;

		/// <summary>
		/// 带 CanvasGroup 的立绘根（可与脚本所在物体分离，使根物体保持 active 以便协程运行）。
		/// </summary>
		private GameObject _VisualRoot => _canvasGroup != null ? _canvasGroup.gameObject : gameObject;
		
		private void _InitIfNot() {
			if (m_isInited) {
				return;
			}
			m_isInited = true;
			_EnsureLayoutElement();
			m_slotFadeAnimation = new FadeAnimation(_canvasGroup);
		}

		private void _EnsureLayoutElement() {
			if (_layoutElement == null) {
				_layoutElement = GetComponent<LayoutElement>();
			}
		}

		/// <summary>根据当前 VisualRoot 是否 active 同步 ignoreLayout（仅与 SetActive 一致，与淡入淡出无关）。</summary>
		private void _SyncLayoutIgnoreWithVisualRootActive() {
			_EnsureLayoutElement();
			if (_layoutElement != null) {
				_layoutElement.ignoreLayout = !_VisualRoot.activeSelf;
			}
		}

		private void _SetVisualRootActive(bool active) {
			_VisualRoot.SetActive(active);
			_SyncLayoutIgnoreWithVisualRootActive();
		}
		
		public void Render(AvgCharImageModel model) {
			_InitIfNot();
			
			var mode = AvgController.Instance != null ? AvgController.Instance.avgMode : AvgMode.DEFAULT;
			switch (mode) {
				case AvgMode.DEFAULT:
				case AvgMode.AUTO:
					_RenderDefaultMode(model);
					break;
				case AvgMode.FAST:
					_RenderFastMode(model);
					break;
			}
			
			m_cachedCharFace = model?.charFace;
			m_cachedCharBody = model?.charBody;
			m_cachedIsTalking = model?.isTalking ?? false;
		}

		private void _RenderDefaultMode(AvgCharImageModel model) {
			var isEmpty = string.IsNullOrEmpty(m_cachedCharBody);
			bool willBeEmpty = model == null || string.IsNullOrEmpty(model.charBody);
			
			if (isEmpty && !willBeEmpty) {
				m_slotFadeAnimation.FadeIn();
				_UpdateImages(model);
				_SyncLayoutIgnoreWithVisualRootActive();
				
			} else if (!isEmpty && willBeEmpty) {
				m_slotFadeAnimation.FadeOut(_SyncLayoutIgnoreWithVisualRootActive);
				
			} else if (!willBeEmpty && m_cachedCharBody != model.charBody) {
				m_slotFadeAnimation.FadeOutThenIn(
					onFadeOutComplete: () => {
						_SetVisualRootActive(true);
						_UpdateImages(model);
					}
				);
				
			} else if (!willBeEmpty) {
				_SetVisualRootActive(true);
				_UpdateCharFace(model.charFace);
				_UpdateTalkingState(model.isTalking);
				
			} else {
				_SetVisualRootActive(false);
			}
		}

		private void _RenderFastMode(AvgCharImageModel model) {
			m_slotFadeAnimation.ResetShow(true);
			
			var isEmpty = string.IsNullOrEmpty(m_cachedCharBody);
			bool willBeEmpty = model == null || string.IsNullOrEmpty(model.charBody);
			
			if (isEmpty && !willBeEmpty) {
				_SetVisualRootActive(true);
				_UpdateImages(model);
				
			} else if (!isEmpty && willBeEmpty) {
				_SetVisualRootActive(false);
				
			} else if (!willBeEmpty && m_cachedCharBody != model.charBody) {
				_SetVisualRootActive(true);
				_UpdateImages(model);
				
			} else if (!willBeEmpty) {
				_SetVisualRootActive(true);
				_UpdateCharFace(model.charFace);
				_UpdateTalkingState(model.isTalking);
				
			} else {
				_SetVisualRootActive(false);
			}
		}

		private void _UpdateImages(AvgCharImageModel model) {
			_UpdateCharBody(model.charBody);
			_UpdateCharFace(model.charFace);
			_UpdateTalkingState(model.isTalking);
		}
		
		private void _UpdateCharFace(string charFace) {
			if (_charFace == null) {
				return;
			}
			_charFace.gameObject.SetActive(false);
			if (m_cachedCharFace == charFace) {
				return;
			}
			if (!string.IsNullOrEmpty(charFace)) {
				StartCoroutine(AvgUtil.LoadSpriteAsync(charFace, AvgUtil.ResourceType.CharFace, (sprite) => {
					if (sprite != null) {
						_charFace.sprite = sprite;
						_charFace.gameObject.SetActive(true);
					}
				}));
			}
		}

		private void _UpdateCharBody(string charBody) {
			StartCoroutine(AvgUtil.LoadSpriteAsync(charBody, AvgUtil.ResourceType.CharBody, (sprite) => {
				if (sprite != null) {
					_charBody.sprite = sprite;
				}
			}));
		}

		private void _UpdateTalkingState(bool isTalking) {
			if (m_cachedIsTalking == isTalking) {
				return;
			}
			Color targetColor = isTalking ? _talkingColor : _normalColor;
			_charBody.color = targetColor;
			if (_charFace != null) {
				_charFace.color = targetColor;
			}
		}

		/// <summary>章节/段落结束时的立即清空（打断渐变，隐藏立绘）。</summary>
		public void ClearImmediate() {
			_InitIfNot();
			m_slotFadeAnimation?.Kill();
			m_slotFadeAnimation?.ResetShow(false);
			m_cachedCharBody = null;
			m_cachedCharFace = null;
			m_cachedIsTalking = false;
			if (_charBody != null) {
				_charBody.sprite = null;
				_charBody.color = _normalColor;
			}
			if (_charFace != null) {
				_charFace.sprite = null;
				_charFace.gameObject.SetActive(false);
				_charFace.color = _normalColor;
			}
			_SetVisualRootActive(false);
		}
	}
}