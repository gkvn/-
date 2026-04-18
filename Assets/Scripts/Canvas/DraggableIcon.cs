using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DraggableIcon : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private bool isToolbarSource;
    private RectTransform canvasArea;
    private Sprite sourceSprite;
    private float iconSize;
    private DraggableIcon activeDrag;

    public bool IsToolbarSource => isToolbarSource;

    public void Setup(bool isSource, RectTransform area, Sprite sprite, float size)
    {
        isToolbarSource = isSource;
        canvasArea = area;
        sourceSprite = sprite;
        iconSize = size;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isToolbarSource)
        {
            var go = new GameObject("PlacedIcon");
            go.transform.SetParent(canvasArea, false);

            var img = go.AddComponent<Image>();
            img.sprite = sourceSprite;
            img.preserveAspect = true;
            img.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(iconSize, iconSize);

            activeDrag = go.AddComponent<DraggableIcon>();
            activeDrag.Setup(false, canvasArea, sourceSprite, iconSize);

            MoveToPointer(activeDrag.GetComponent<RectTransform>(), eventData);
        }
        else
        {
            GetComponent<Image>().raycastTarget = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isToolbarSource && activeDrag != null)
            MoveToPointer(activeDrag.GetComponent<RectTransform>(), eventData);
        else if (!isToolbarSource)
            MoveToPointer(GetComponent<RectTransform>(), eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isToolbarSource)
        {
            if (activeDrag != null)
            {
                bool inside = RectTransformUtility.RectangleContainsScreenPoint(
                    canvasArea, eventData.position, eventData.pressEventCamera);
                if (!inside)
                    Destroy(activeDrag.gameObject);
                else
                    activeDrag.GetComponent<Image>().raycastTarget = true;
                activeDrag = null;
            }
        }
        else
        {
            bool inside = RectTransformUtility.RectangleContainsScreenPoint(
                canvasArea, eventData.position, eventData.pressEventCamera);
            if (!inside)
                Destroy(gameObject);
            else
                GetComponent<Image>().raycastTarget = true;
        }
    }

    private void MoveToPointer(RectTransform rt, PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasArea, eventData.position, eventData.pressEventCamera, out var local);
        rt.anchoredPosition = local;
    }
}
