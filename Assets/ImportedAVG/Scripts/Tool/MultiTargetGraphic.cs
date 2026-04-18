using UnityEngine;
using UnityEngine.UI;

namespace AVG.Tool {
	/// <summary>
	/// 多目标Graphic组件，可挂载到Button的Target Graphic上，
	/// 使按钮的点击效果（颜色变化）同时应用于多个UI元素（Image、Text等）
	/// </summary>
	[RequireComponent(typeof(CanvasRenderer))]
	public class MultiTargetGraphic : Graphic {
		[Tooltip("需要同步颜色变化的目标Graphic列表")]
		[SerializeField]
		private Graphic[] _targetGraphics;

		/// <summary>
		/// 目标Graphic数组
		/// </summary>
		public Graphic[] TargetGraphics {
			get => _targetGraphics;
			set => _targetGraphics = value;
		}

		// 记录每个目标的原始颜色，用于正确应用颜色叠加
		private Color[] _originalColors;

		protected override void Awake() {
			base.Awake();
			CacheOriginalColors();
		}

		/// <summary>
		/// 缓存所有目标Graphic的原始颜色
		/// </summary>
		public void CacheOriginalColors() {
			if (_targetGraphics == null || _targetGraphics.Length == 0) {
				_originalColors = null;
				return;
			}

			_originalColors = new Color[_targetGraphics.Length];
			for (int i = 0; i < _targetGraphics.Length; i++) {
				if (_targetGraphics[i] != null) {
					_originalColors[i] = _targetGraphics[i].color;
				}
			}
		}

		/// <summary>
		/// Button通过此方法实现颜色渐变效果
		/// </summary>
		public override void CrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha) {
			base.CrossFadeColor(targetColor, duration, ignoreTimeScale, useAlpha);

			if (_targetGraphics == null) return;

			for (int i = 0; i < _targetGraphics.Length; i++) {
				if (_targetGraphics[i] != null) {
					// 计算目标颜色（原始颜色 * 按钮状态颜色）
					Color finalColor = _originalColors != null && i < _originalColors.Length
						? _originalColors[i] * targetColor
						: targetColor;

					_targetGraphics[i].CrossFadeColor(finalColor, duration, ignoreTimeScale, useAlpha);
				}
			}
		}

		/// <summary>
		/// Button通过此方法实现颜色渐变效果（带alpha乘数）
		/// </summary>
		public override void CrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha, bool useRGB) {
			base.CrossFadeColor(targetColor, duration, ignoreTimeScale, useAlpha, useRGB);

			if (_targetGraphics == null) return;

			for (int i = 0; i < _targetGraphics.Length; i++) {
				if (_targetGraphics[i] != null) {
					Color finalColor = _originalColors != null && i < _originalColors.Length
						? _originalColors[i] * targetColor
						: targetColor;

					_targetGraphics[i].CrossFadeColor(finalColor, duration, ignoreTimeScale, useAlpha, useRGB);
				}
			}
		}

		/// <summary>
		/// Button通过此方法实现Alpha渐变效果
		/// </summary>
		public override void CrossFadeAlpha(float alpha, float duration, bool ignoreTimeScale) {
			base.CrossFadeAlpha(alpha, duration, ignoreTimeScale);

			if (_targetGraphics == null) return;

			foreach (var graphic in _targetGraphics) {
				if (graphic != null) {
					graphic.CrossFadeAlpha(alpha, duration, ignoreTimeScale);
				}
			}
		}

		#region 不绘制任何内容（类似NonDrawingGraphic）

		public override void SetMaterialDirty() {
		}

		public override void SetVerticesDirty() {
		}

		protected override void OnPopulateMesh(VertexHelper vh) {
			vh.Clear();
		}

		#endregion

#if UNITY_EDITOR
		protected override void OnValidate() {
			base.OnValidate();
			// 编辑器中修改目标列表时重新缓存原始颜色
			CacheOriginalColors();
		}
#endif
	}
}
