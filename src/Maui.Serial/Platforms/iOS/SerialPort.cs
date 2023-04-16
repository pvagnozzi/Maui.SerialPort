using System.Diagnostics;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Maui.Serial;

public class SerialPort : ISerialPort
{
    [DebuggerStepThrough]
    public static string[] GetPortNames(ILogger logger = null) => Array.Empty<string>();

    public SerialPortParameters Parameters => new();

    [DebuggerStepThrough]
    public void Open(SerialPortParameters parameters) => throw new NotSupportedException();

    public int Read(byte[] data)
    {
        throw new NotImplementedException();
    }

    public string ReadLine()
    {
        throw new NotImplementedException();
    }

    public string ReadExisting()
    {
        throw new NotImplementedException();
    }

    public void Write(byte[] data)
    {
        throw new NotImplementedException();
    }

    public void Write(string value)
    {
        throw new NotImplementedException();
    }

    public void WriteLine(string line)
    {
        throw new NotImplementedException();
    }

    [DebuggerStepThrough]
    public void Close() => throw new NotSupportedException();

    [DebuggerStepThrough]
    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }

    public event EventHandler<SerialPortDataReceivedEventArgs> DataReceived;

    public event EventHandler<SerialPortErrorEventArgs> ErrorReceived;
}
