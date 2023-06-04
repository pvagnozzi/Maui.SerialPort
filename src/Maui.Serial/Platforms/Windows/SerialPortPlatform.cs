using System.IO.Ports;
using Maui.Serial.Platforms.Windows;

// ReSharper disable once CheckNamespace
namespace Maui.Serial;

internal static partial class SerialPortPlatform
{
    internal static partial IList<string> GetPortNames() => SerialPort.GetPortNames();

    internal static partial ISerialPort GetPort(string portName) => new SerialPortWindows(portName);
}