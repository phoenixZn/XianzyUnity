using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DoubleClickEventTrigger : MonoBehaviour, IPointerClickHandler,
    IEventTrigger<UIEventListener.IntDelegate>
{
    public UIEventListener.IntDelegate onDoubleClick;
    public float checkInterval = 0.5f;

    private float _checkTime;

    public static IEventTrigger<UIEventListener.IntDelegate> Get(GameObject go)
    {
        DoubleClickEventTrigger listener = go.GetComponent<DoubleClickEventTrigger>();
        if (listener == null) listener = go.AddComponent<DoubleClickEventTrigger>();
        return listener;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_checkTime <= 0)
        {
            _checkTime = checkInterval;
            onDoubleClick(gameObject, 1);
        }
        else
        {
            if (onDoubleClick != null)
            {
                onDoubleClick(gameObject, 2);
                _checkTime = 0f;
            }
        }
    }

    private void Update()
    {
        if (_checkTime >= 0f)
        {
            _checkTime -= Time.unscaledDeltaTime;
        }
    }

    public void AddListener(UIEventListener.IntDelegate t)
    {
        onDoubleClick += t;
    }

    public void RemoveListener(UIEventListener.IntDelegate t)
    {
        onDoubleClick -= t;
    }

    public void RemoveListener()
    {
        onDoubleClick = null;
    }
}
