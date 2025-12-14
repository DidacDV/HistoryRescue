using System.Collections.Generic;
using UnityEngine;

public abstract class BaseSwitch : MonoBehaviour, ISwitchSource
{
    protected bool currentState = false;
    protected List<ISwitchListener> listeners = new List<ISwitchListener>();

    public void AddListener(ISwitchListener listener)
    {
        if (!listeners.Contains(listener))
        {
            listeners.Add(listener);
        }
    }

    public void RemoveListener(ISwitchListener listener)
    {
        listeners.Remove(listener);
    }

    protected void Toggle()
    {
        currentState = !currentState;
        NotifyListeners();
    }

    protected void NotifyListeners()
    {
        foreach (var listener in listeners)
        {
            listener.OnSwitchToggled(this, currentState);
        }
    }

    public bool GetState()
    {
        return currentState;
    }
}