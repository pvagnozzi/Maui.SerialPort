using System.Diagnostics;
using System.IO.Ports;
using Microsoft.Extensions.Logging;
using Port = System.IO.Ports.SerialPort;

// ReSharper disable once CheckNamespace
namespace Maui.Serial;

public class SerialPort : ISerialPort
{
    public static string[] GetPortNames() => Port.GetPortNames();

    public SerialPort(string portName, SerialPortParameters parameters, ILogger logger = null)
    {
        PortName = portName;
        Parameters = parameters;
        Logger = logger;
    }

    public string PortName { get; }
    public SerialPortParameters Parameters { get; }

    protected internal Port Port { get; private set; }
    protected internal ILogger Logger { get; }

    [DebuggerStepThrough]
    public void Open(SerialPortParameters parameters)
    {
        Close();
        Port = new Port(PortName, parameters.BaudRate)
        {
            Site = null,
            BreakState = false,
            DataBits = 0,
            DiscardNull = false,
            DtrEnable = false,
            Encoding = parameters.Encoding,
            Handshake = Handshake.None,
            NewLine = parameters.NewLine,
            Parity = System.IO.Ports.Parity.None,
            ParityReplace = 0,
            ReadBufferSize = 0,
            ReadTimeout = 0,
            ReceivedBytesThreshold = 0,
            RtsEnable = false,
            StopBits = System.IO.Ports.StopBits.None,
            WriteBufferSize = 0,
            WriteTimeout = 0
        };
        Port.NewLine = parameters.NewLine;
        Port.Open();

        Port.DataReceived += (sender, args) => DataReceived?.Invoke(sender,
            new SerialPortDataReceivedEventArgs(args.EventType == SerialData.Chars
                ? SerialDataEventType.Chars
                : SerialDataEventType.Eof));
        Port.ErrorReceived += (sender, args) =>
            ErrorReceived?.Invoke(sender, new SerialPortErrorEventArgs(SerialPortError.Frame));
    }

    [DebuggerStepThrough]
    public int Read(byte[] data)
    {
        CheckPortOpen();
        return Port.Read(data, 0, data.Length);
    }

    [DebuggerStepThrough]
    public string ReadLine()
    {
        CheckPortOpen();
        return Port.ReadLine();
    }

    [DebuggerStepThrough]
    public string ReadExisting()
    {
        CheckPortOpen();
        return Port.ReadExisting();
    }

    [DebuggerStepThrough]
    public void Write(byte[] data)
    {
        CheckPortOpen();
        Port.Write(data, 0, data.Length);
    }

    [DebuggerStepThrough]
    public void Write(string value)
    {
        CheckPortOpen();
        Port.Write(value);
    }

    [DebuggerStepThrough]
    public void WriteLine(string line)
    {
        CheckPortOpen();
        Port.WriteLine(line);
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

    public event EventHandler<SerialPortDataReceivedEventArgs> DataReceived;
    public event EventHandler<SerialPortErrorEventArgs> ErrorReceived;

    [DebuggerStepThrough]
    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }

    private void CheckPortOpen()
    {
        if (Port is null)
        {
            throw new InvalidOperationException("Port is not open");
        }
    }
}
