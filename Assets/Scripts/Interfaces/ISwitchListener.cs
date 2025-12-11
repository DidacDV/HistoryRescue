public interface ISwitchSource
{

}

public interface ISwitchListener
{
    void RegisterSwitch(ISwitchSource source);
    void RemoveSwitch(ISwitchSource source);
}