using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickForwader : MonoBehaviour, IPointerClickHandler
{
    public Action<PointerEventData> OnClick;
    public void OnPointerClick(PointerEventData eventData)
    {
        OnClick?.Invoke(eventData);
    }
}
