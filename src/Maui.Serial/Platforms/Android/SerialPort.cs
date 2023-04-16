using System.Diagnostics;
using Android.Hardware.Usb;
using Maui.Serial.Platforms.Android.Usb;
using Microsoft.Extensions.Logging;
using Activity = Android.App.Activity;

// ReSharper disable once CheckNamespace
namespace Maui.Serial;

public class SerialPort : ISerialPort
{
    private static UsbManager _usbManager;

    private static UsbSerialPort[] _ports = Array.Empty<UsbSerialPort>();

    [DebuggerStepThrough]
    public static string[] GetPortNames(ILogger logger = null) =>
        GetPortNames(Platform.CurrentActivity, logger);

    public static string[] GetPortNames(Activity activity, ILogger logger = null)
    {
        if (activity is null)
        {
            throw new ArgumentNullException(nameof(activity));
        }
        _usbManager = activity.GetUsbManager();

        var usbProber = new UsbDeviceProber(logger);
        usbProber.RegisterDriver(typeof(ISerialPort).Assembly);

        if (_usbManager.DeviceList is null)
        {
            return Array.Empty<string>();
        }

        var ports = new List<UsbSerialPort>();

        foreach (var device in _usbManager.DeviceList.Values)
        {
            var driver = usbProber.ProbeDevice(_usbManager, device);

            if (driver is not UsbSerialDriver serilPortDriver)
            {
                continue;
            }

            ports.AddRange(serilPortDriver.GetPorts());
        }

        _ports = ports.ToArray();
        return _ports.Select(x => x.PortName).ToArray();
    }

    private UsbSerialPort GetPortByName(string name)
    {
        var port = _ports.FirstOrDefault(x => x.PortName == name);
        return port ?? throw new ArgumentException($"Port {PortName} does not exists");
    }

    private readonly List<byte> _readBuffer = new();
    private readonly PollingTask _pollingTask;
    private byte[] _pollingBuffer;

    public SerialPort(string portName, SerialPortParameters parameters = null)
    {
        UsbSerialPort = GetPortByName(portName);
        UsbSerialPort.Parameters = parameters ?? new SerialPortParameters();
        _pollingTask = new PollingTask(PollingAction);
    }

    protected internal UsbSerialPort UsbSerialPort { get; }

    public string PortName => UsbSerialPort.PortName;
    public SerialPortParameters Parameters => UsbSerialPort.Parameters;

    public void Open(SerialPortParameters parameters)
    {
        Close();
        UsbSerialPort.Parameters = parameters;
        UsbSerialPort.Open();

        var bufferSize = UsbSerialPort.Parameters.ReadBufferSize;
        _pollingBuffer = new byte[bufferSize];
        _pollingTask.Start();
    }

    [DebuggerStepThrough]
    public int Read(byte[] data) => UsbSerialPort.Read(data);

    public string ReadLine()
    {
        var data = _readBuffer.ToArray();
        var separator = Parameters.Encoding.GetBytes(Parameters.NewLine);

        var index = Array.IndexOf(data, separator[0]);
        if (index < 0)
        {
            return string.Empty;
        }

        var len = index;
        if (separator.Length > 1)
        {
            if (index + 1 >= data.Length || data[index + 1] != separator[1])
            {
                return string.Empty;
            }
            len++;
        }

        var line = Parameters.Encoding.GetString(data, 0, index);
        _readBuffer.RemoveRange(0, len);
        return line;
    }

    public string ReadExisting()
    {
        var data = _readBuffer.ToArray();
        _readBuffer.Clear();
        return Parameters.Encoding.GetString(data);
    }

    [DebuggerStepThrough]
    public void Write(byte[] data) => UsbSerialPort.Write(data);

    [DebuggerStepThrough]
    public void Write(string value) => Write(Parameters.Encoding.GetBytes(value));

    [DebuggerStepThrough]
    public void WriteLine(string line) => Write(line + Parameters.NewLine);

    [DebuggerStepThrough]
    public void Close()
    {
        _readBuffer.Clear();
        _pollingTask.Stop();
        UsbSerialPort.Close();
    }

    [DebuggerStepThrough]
    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }

    public event EventHandler<SerialPortDataReceivedEventArgs> DataReceived;

    public event EventHandler<SerialPortErrorEventArgs> ErrorReceived;

    private void PollingAction(CancellationToken cancellation)
    {
        if (cancellation.IsCancellationRequested)
        {
            throw new TaskCanceledException();
        }

        try
        {
            var len = UsbSerialPort.Read(_pollingBuffer);
            _readBuffer.AddRange(_pollingBuffer.Take(len));
            DataReceived?.Invoke(this, new SerialPortDataReceivedEventArgs());
        }
        catch (Exception)
        {
            ErrorReceived?.Invoke(this, new SerialPortErrorEventArgs(SerialPortError.Frame));
        }

    }
}