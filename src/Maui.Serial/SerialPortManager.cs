namespace Maui.Serial;

public static class SerialPortManager
{
    public static IList<string> GetPortNames() => SerialPortPlatform.GetPortNames();

    public static ISerialPort GetPort(string portName) => SerialPortPlatform.GetPort(portName);
}

