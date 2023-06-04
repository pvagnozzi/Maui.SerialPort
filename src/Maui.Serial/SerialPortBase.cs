using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Maui.Serial;

public abstract class SerialPortBase : ISerialPort
{
    protected SerialPortBase(string portName, SerialPortParameters parameters = null, ILogger logger = null)
    {
        PortName = portName;
        Parameters = parameters ?? new SerialPortParameters();
        Logger = logger;
        IsOpen = false;
    }

    protected ILogger Logger { get; }

    public string PortName { get; }

    public SerialPortParameters Parameters { get; private set; }

    public bool IsOpen { get; private set; }

    public virtual void Open(SerialPortParameters parameters)
    {
        if (IsOpen)
        {
            return;
        }

        if (parameters is not null)
        {
            Parameters = parameters;
        }

        OpenPort(PortName, Parameters);
        IsOpen = true;
    }

    public virtual int Read(byte[] data)
    {
        EnsurePortIsOpen();
        return ReadFromPort(data);
    }

    public virtual string ReadLine()
    {
        EnsurePortIsOpen();
        return ReadLineFromPort();
    }

    public virtual string ReadExisting()
    {
        EnsurePortIsOpen();
        return ReadExistingFromPort();
    }

    public virtual void Write(byte[] data)
    {
        EnsurePortIsOpen();
        WriteToPort(data);
    }

    public virtual void Write(string value)
    {
        EnsurePortIsOpen();
        WriteToPort(value);
    }

    public virtual void WriteLine(string line)
    {
        EnsurePortIsOpen();
        WriteLineToPort(line);
    }

    public virtual void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        ClosePort();
        IsOpen = false;
    }

    public virtual void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }

    public event EventHandler<SerialPortDataReceivedEventArgs> DataReceived;

    public event EventHandler<SerialPortErrorEventArgs> ErrorReceived;

    protected virtual void OpenPort(string portName, SerialPortParameters parameters)
    {
        throw new NotImplementedException();
    }

    protected virtual void ClosePort()
    {
        throw new NotImplementedException();
    }

    protected virtual void EnsurePortIsOpen()
    {
        if (!IsOpen)
        {
            throw new InvalidOperationException("Serial port is not open.");
        }
    }

    [DebuggerStepThrough]
    protected virtual int ReadFromPort(byte[] data) => throw new NotSupportedException();

    [DebuggerStepThrough]
    protected virtual string ReadLineFromPort() => throw new NotSupportedException();


    [DebuggerStepThrough]
    protected virtual string ReadExistingFromPort() => throw new NotSupportedException();

    [DebuggerStepThrough]
    protected virtual void WriteToPort(byte[] data) => throw new NotSupportedException();

    [DebuggerStepThrough]
    protected virtual void WriteToPort(string value) => throw new NotSupportedException();

    [DebuggerStepThrough]
    protected virtual void WriteLineToPort(string line) => throw new NotSupportedException();

    protected void OnDataReceived(SerialDataEventType eventType = SerialDataEventType.Chars) =>
        DataReceived?.Invoke(this, new SerialPortDataReceivedEventArgs(eventType));

    protected void OnErrorReceived(SerialPortError error = SerialPortError.Frame) =>
        ErrorReceived?.Invoke(this, new SerialPortErrorEventArgs(error));
}