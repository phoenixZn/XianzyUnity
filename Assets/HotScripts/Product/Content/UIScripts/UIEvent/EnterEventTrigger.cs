using UnityEngine;
using UnityEngine.EventSystems;

public class EnterEventTrigger : MonoBehaviour, IPointerDownHandler
    , IEventTrigger<UIEventListener.PointerEventDataDelegate>
{
    public static IEventTrigger<UIEventListener.PointerEventDataDelegate> Get(GameObject go)
    {
        EnterEventTrigger listener = go.GetComponent<EnterEventTrigger>();
        if (listener == null) listener = go.AddComponent<EnterEventTrigger>();
        return listener;
    }

    public UIEventListener.PointerEventDataDelegate onClick;

    public void AddListener(UIEventListener.PointerEventDataDelegate t)
    {
        onClick += t;
    }

    public void RemoveListener()
    {
        onClick = null;
    }

    public void RemoveListener(UIEventListener.PointerEventDataDelegate t)
    {
        onClick -= t;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (onClick != null)
        {
            onClick(gameObject, eventData);
        }
    }
}