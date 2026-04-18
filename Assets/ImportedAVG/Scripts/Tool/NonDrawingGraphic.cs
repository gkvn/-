using UnityEngine;
using UnityEngine.UI;

namespace AVG.Tool {
	[RequireComponent(typeof(CanvasRenderer))]
	public class NonDrawingGraphic : Graphic {
		public override void SetMaterialDirty() {
		}

		public override void SetVerticesDirty() {
		}

		protected override void OnPopulateMesh(VertexHelper vh) {
			vh.Clear();
		}
	}
}