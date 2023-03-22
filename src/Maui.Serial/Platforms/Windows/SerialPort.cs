using System.Diagnostics;
using Microsoft.Extensions.Logging;

using System.IO.Ports;
using Port = System.IO.Ports.SerialPort;

// ReSharper disable once CheckNamespace
namespace Maui.Serial;

public class SerialPort : ISerialPort
{
    [DebuggerStepThrough]
    public static string[] GetPortNames(ILogger logger = null) => Port.GetPortNames();

    public SerialPort(string portName, SerialPortParameters parameters)
    {
        PortName = portName;
        Parameters = parameters;
    }

    protected Port Port { get; private set; }
    public string PortName { get; }

    public SerialPortParameters Parameters { get; }

    [DebuggerStepThrough]
    public void Open(SerialPortParameters parameters)
    {
        Close();
        Port = new Port(PortName, parameters.BaudRate);

        Port.Open();
    }

    [DebuggerStepThrough]
    public int Read(byte[] data, int timeout = -1)
    {
        if (Port is null)
        {
            throw new InvalidOperationException("Port is not open");
        }
        
        return  Port.Read(data, 0, data.Length);
    }

    [DebuggerStepThrough]
    public void Write(byte[] data, int timeout = -1)
    {
        if (Port is null)
        {
            throw new InvalidOperationException("Port is not open");
        }
        
        Port.Write(data, 0, data.Length);
    }

    [DebuggerStepThrough]
    public void Close()
    {
        if (Port is null)
        {
            return;
        }

        Port.Close();
        Port.Dispose();
        Port = null;
    }

    [DebuggerStepThrough]
    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    } 

    private event EventHandler<SerialDataReceivedArgs> _dataReceived;
    private event EventHandler<UnhandledExceptionEventArgs> _errorReceived;
    
    public event EventHandler<SerialDataReceivedArgs> DataReceived
    {
        add => _dataReceived += value;
        remove => _dataReceived -= value;
    }

    public event EventHandler<UnhandledExceptionEventArgs> ErrorReceived
    {
        add => _errorReceived += value;
        remove => _errorReceived -= value;
    }
}
