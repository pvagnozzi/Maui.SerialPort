namespace Maui.Serial;

public class SerialDataReceivedArgs : EventArgs
{
    public SerialDataReceivedArgs(string data = null)
    {
        Data = data ?? string.Empty;
    }

    public string Data { get; }
}
