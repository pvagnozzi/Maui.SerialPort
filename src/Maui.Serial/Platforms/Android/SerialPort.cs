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
        GetPortNames(Platform.CurrentActivity);

    public static string[] GetPortNames(Activity activity, ILogger logger = null)
    {
        if (activity is null)
        {
            throw new ArgumentNullException(nameof(activity));
        }
        _usbManager = activity.GetUsbManager();

        var usbProber = new UsbDeviceProber(logger);
        usbProber.RegisterDriver(typeof(SerialPort).Assembly);

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
        if (port is null)
        {
            throw new ArgumentException($"Port {PortName} does not exists");
        }

        return port;
    }

    public SerialPort(string portName, SerialPortParameters parameters = null)
    {
        UsbSerialPort = GetPortByName(portName);
        UsbSerialPort.Parameters = parameters ?? new SerialPortParameters();
        UsbSerialPortMonitor = new UsbSerialPortMonitor(UsbSerialPort);
    }

    protected internal UsbSerialPort UsbSerialPort { get; }

    protected internal UsbSerialPortMonitor UsbSerialPortMonitor { get; }

    public string PortName => UsbSerialPort.PortName;

    public SerialPortParameters Parameters => UsbSerialPort.Parameters;

    public void Open(SerialPortParameters parameters)
    {
        Close();
        UsbSerialPort.Parameters = parameters;
        UsbSerialPort.Open();
        UsbSerialPortMonitor.Start();
    }

    [DebuggerStepThrough]
    public int Read(byte[] data, int timeout = -1) =>
        UsbSerialPort.Read(data, timeout);

    [DebuggerStepThrough]
    public void Write(byte[] data, int timeout = -1) =>
        UsbSerialPort.Write(data, timeout);

    [DebuggerStepThrough]
    public void Close()
    {
        UsbSerialPortMonitor.Stop();
        UsbSerialPort.Close();
    }

    [DebuggerStepThrough]
    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }

    public event EventHandler<SerialDataReceivedArgs> DataReceived
    {
        add
        {
            UsbSerialPortMonitor.DataReceived += value;
            UsbSerialPortMonitor.Start();
        }
            
        remove => UsbSerialPortMonitor.DataReceived -= value;
    }

    public event EventHandler<UnhandledExceptionEventArgs> ErrorReceived
    {
        add => UsbSerialPortMonitor.ErrorReceived += value;
        remove => UsbSerialPortMonitor.ErrorReceived -= value;
    }
}