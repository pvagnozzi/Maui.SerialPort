using Android.App;
using Android.Hardware.Usb;
using Maui.Serial.Platforms.Android;
using Maui.Serial.Platforms.Android.Usb;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Maui.Serial;

internal static partial class SerialPortPlatform
{
    private static UsbManager _usbManager;

    private static UsbSerialPort[] _ports = Array.Empty<UsbSerialPort>();

    internal static partial IList<string> GetPortNames() => GetPortNames(Platform.CurrentActivity);

    internal static partial ISerialPort GetPort(string portName) => new SerialPortAndroid(portName);

    internal static string[] GetPortNames(Activity activity, ILogger logger = null)
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

    internal static UsbSerialPort GetUsbPortByName(string portName)
    {
        var port = _ports.FirstOrDefault(x => x.PortName == portName);
        return port ?? throw new ArgumentException($"Port {portName} does not exists");
    }
}
