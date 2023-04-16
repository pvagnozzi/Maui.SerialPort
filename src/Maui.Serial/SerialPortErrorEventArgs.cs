namespace Maui.Serial;

public enum SerialPortError
{
    Overrun = 2,
    RxParity = 4,
    Frame = 8,
    TxFull = 256,
}

public class SerialPortErrorEventArgs : EventArgs
{
    public SerialPortErrorEventArgs(SerialPortError eventType)
    {
        EventType = eventType;
    }

    public SerialPortError EventType { get; }
}
