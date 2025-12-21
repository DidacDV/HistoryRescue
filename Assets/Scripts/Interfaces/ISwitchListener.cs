public interface ISwitchSource
{
    void AddListener(ISwitchListener listener);
    void RemoveListener(ISwitchListener listener);
}

public interface ISwitchListener
{
    void OnSwitchToggled(ISwitchSource source, bool state);
}