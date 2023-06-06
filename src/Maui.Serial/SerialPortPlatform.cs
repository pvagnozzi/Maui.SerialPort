namespace Maui.Serial;

internal static partial class SerialPortPlatform
{
    internal static partial IList<string> GetPortNames();

    internal static partial ISerialPort GetPort(string portName);
}