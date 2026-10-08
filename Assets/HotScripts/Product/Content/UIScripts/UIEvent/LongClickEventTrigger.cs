using UnityEngine;
using UnityEngine.EventSystems;

public class LongClickEventTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    , IEventTrigger<UIEventListener.BoolDelegate>
{
    public static IEventTrigger<UIEventListener.BoolDelegate> Get(GameObject go)
    {
        LongClickEventTrigger listener = go.GetComponent<LongClickEventTrigger>();
        if (listener == null) listener = go.AddComponent<LongClickEventTrigger>();
        return listener;
    }

    public UIEventListener.BoolDelegate onClick;

    public void AddListener(UIEventListener.BoolDelegate t)
    {
        onClick += t;
    }

    public void RemoveListener()
    {
        onClick = null;
    }

    public void RemoveListener(UIEventListener.BoolDelegate t)
    {
        onClick -= t;
    }

    const float LongTime = 0.5f;

    private bool _isPress;
    private float _timer;

    public void OnPointerDown(PointerEventData eventData)
    {
        _timer = 0f;
        _isPress = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        //有滑动根据条件判断松手后算不算普通点击
        if (!eventData.dragging
            && _isPress)
        {
            if (onClick != null)
                onClick(gameObject, false);
        }

        _isPress = false;
        _timer = 0;
    }

    private void Update()
    {
        if (_isPress)
        {
            _timer += Time.unscaledDeltaTime;
            if (_timer >= LongTime)
            {
                if (onClick != null)
                    onClick(gameObject, true);

                _timer = 0;
            }
        }
    }
}