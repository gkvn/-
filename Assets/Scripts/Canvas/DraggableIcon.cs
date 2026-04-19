using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DraggableIcon : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private static readonly Color DimColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private bool isToolbarSource;
    private RectTransform canvasArea;
    private Sprite sourceSprite;
    private float iconSize;
    private DraggableIcon activeDrag;
    private Image cachedImage;

    public bool IsToolbarSource => isToolbarSource;

    private Image CachedImage
    {
        get
        {
            if (cachedImage == null)
                cachedImage = GetComponent<Image>();
            return cachedImage;
        }
    }

    public void Setup(bool isSource, RectTransform area, Sprite sprite, float size)
    {
        isToolbarSource = isSource;
        canvasArea = area;
        sourceSprite = sprite;
        iconSize = size;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        CachedImage.color = DimColor;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        CachedImage.color = Color.white;
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
            img.color = DimColor;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(iconSize, iconSize);

            activeDrag = go.AddComponent<DraggableIcon>();
            activeDrag.Setup(false, canvasArea, sourceSprite, iconSize);

            MoveToPointer(activeDrag.GetComponent<RectTransform>(), eventData);
        }
        else
        {
            CachedImage.raycastTarget = false;
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
            CachedImage.color = Color.white;

            if (activeDrag != null)
            {
                bool inside = RectTransformUtility.RectangleContainsScreenPoint(
                    canvasArea, eventData.position, eventData.pressEventCamera);
                if (!inside)
                {
                    Destroy(activeDrag.gameObject);
                }
                else
                {
                    var img = activeDrag.CachedImage;
                    img.raycastTarget = true;
                    img.color = Color.white;
                }
                activeDrag = null;
            }
        }
        else
        {
            bool inside = RectTransformUtility.RectangleContainsScreenPoint(
                canvasArea, eventData.position, eventData.pressEventCamera);
            if (!inside)
            {
                Destroy(gameObject);
            }
            else
            {
                CachedImage.raycastTarget = true;
                CachedImage.color = Color.white;
            }
        }
    }

    private void MoveToPointer(RectTransform rt, PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasArea, eventData.position, eventData.pressEventCamera, out var local);
        rt.anchoredPosition = local;
    }
}
