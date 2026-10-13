using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ClickEventTrigger : MonoBehaviour, IPointerClickHandler,
    IEventTrigger<UIEventListener.VoidDelegate>
{
    public UIEventListener.VoidDelegate onClick;
    public float clickInterval = 0.1f;

    private float _lastClickTime;

    public static IEventTrigger<UIEventListener.VoidDelegate> Get(GameObject go)
    {
        ClickEventTrigger listener = null;
        listener = go.GetComponent<ClickEventTrigger>();
        if (listener == null) 
            listener = go.AddComponent<ClickEventTrigger>();
        return listener;
        
        // if (listener == null)
        // {
        //     var cmpt = go.GetComponent(typeof(ClickEventTrigger));
        //     listener = cmpt as ClickEventTrigger;
        // }
        //
        // List<MonoBehaviour> results = new();
        // go.GetComponents<MonoBehaviour>(results);
        // foreach (var c in results)
        // {
        //     //Debug.LogError($"MonoBehaviour = {c.GetType().FullName}");
        //     if (c is ClickEventTrigger)
        //     {
        //         Debug.LogError($"c is ClickEventTrigger");
        //     }
        //     if (c.GetType().FullName == typeof(ClickEventTrigger).FullName)
        //     {
        //         listener = c as ClickEventTrigger;
        //     }
        //     else
        //     {
        //         Debug.LogError($"[{c.GetType().Name}] != [{typeof(ClickEventTrigger).Name}]");
        //     }
        // }
        //
        // List<Component> results2 = new();
        // go.GetComponents(results2);
        // foreach (var c in results2)
        // {
        //     //Debug.LogError($"Component = {c.GetType().FullName}");
        //     if (c is ClickEventTrigger)
        //     {
        //         Debug.LogError($"c is ClickEventTrigger");
        //     }
        // }
        //
        // return listener;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Time.realtimeSinceStartup - _lastClickTime < clickInterval)
        {
            return;
        }
        if (onClick != null)
        {
            _lastClickTime = Time.realtimeSinceStartup;
            onClick(gameObject);
        }
    }

    public void AddListener(UIEventListener.VoidDelegate t)
    {
        onClick += t;
    }

    public void RemoveListener(UIEventListener.VoidDelegate t)
    {
        onClick -= t;
    }

    public void RemoveListener()
    {
        onClick = null;
    }
}