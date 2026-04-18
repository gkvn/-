using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class IconToolbar : MonoBehaviour
{
    [SerializeField] private float iconSize = 48f;

    public void Initialize(List<Sprite> icons, RectTransform canvasArea)
    {
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
            rt.sizeDelta = new Vector2(iconSize, iconSize);

            var le = go.AddComponent<UnityEngine.UI.LayoutElement>();
            le.preferredWidth = iconSize;
            le.preferredHeight = iconSize;

            var drag = go.AddComponent<DraggableIcon>();
            drag.Setup(true, canvasArea, sprite, iconSize);
        }
    }
}
