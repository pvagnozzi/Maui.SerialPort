namespace Maui.Serial;

public enum SerialDataEventType
{
    Chars,
    Eof
}

public class SerialPortDataReceivedEventArgs : EventArgs
{
    public SerialPortDataReceivedEventArgs(SerialDataEventType eventType = SerialDataEventType.Chars)
    {
        EventType = eventType;
    }

    public SerialDataEventType EventType { get; }
}
