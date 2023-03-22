using Microsoft.Extensions.Logging;
using System.Diagnostics;

// ReSharper disable once CheckNamespace
namespace Maui.Serial;

public class SerialPort : ISerialPort
{
    [DebuggerStepThrough]
    public static string[] GetPortNames(ILogger logger = null) => Array.Empty<string>();

    public SerialPortParameters Parameters => new();

    [DebuggerStepThrough]
    public void Open(SerialPortParameters parameters) => throw new NotSupportedException();

    [DebuggerStepThrough]
    public int Read(byte[] data, int timeout = -1) => throw new NotSupportedException();

    [DebuggerStepThrough]
    public void Write(byte[] data, int timeout = -1) => throw new NotSupportedException();

    [DebuggerStepThrough]
    public void Close() => throw new NotSupportedException();

    [DebuggerStepThrough]
    public void Dispose() => GC.SuppressFinalize(this);

    public event EventHandler<SerialDataReceivedArgs> DataReceived;
    public event EventHandler<UnhandledExceptionEventArgs> ErrorReceived;
}
