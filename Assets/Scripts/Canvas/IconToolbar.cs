using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class IconToolbar : MonoBehaviour
{
    private const float IconSize = 68f;

    public void Initialize(List<Sprite> icons, RectTransform canvasArea)
    {
        var hlg = GetComponent<HorizontalLayoutGroup>();
        if (hlg != null)
        {
            hlg.childAlignment = TextAnchor.MiddleCenter;
        }

        foreach (Transform child in transform)
            Destroy(child.gameObject);

        foreach (var sprite in icons)
        {
            var go = new GameObject(sprite != null ? sprite.name : "Icon");
            go.transform.SetParent(transform, false);

            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(IconSize, IconSize);

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = IconSize;
            le.preferredHeight = IconSize;

            var drag = go.AddComponent<DraggableIcon>();
            drag.Setup(true, canvasArea, sprite, IconSize);
        }
    }
}
