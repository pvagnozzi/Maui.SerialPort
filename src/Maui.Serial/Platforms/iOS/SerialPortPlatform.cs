// ReSharper disable once CheckNamespace
namespace Maui.Serial;

internal static partial class SerialPortPlatform
{
    internal static partial IList<string> GetPortNames() => throw new NotSupportedException();

    internal static partial ISerialPort GetPort(string portName) => throw new NotSupportedException();
}