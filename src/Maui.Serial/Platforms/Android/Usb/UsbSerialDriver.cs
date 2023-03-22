using Android.Hardware.Usb;
using Microsoft.Extensions.Logging;

namespace Maui.Serial.Platforms.Android.Usb;

public abstract class UsbSerialDriver : UsbDeviceDriver
{
    protected UsbSerialDriver(
        UsbManager manager,
        UsbDevice device,
        ILogger logger) :
        base(manager, device, logger)
    {
    }

    public abstract IEnumerable<UsbSerialPort> GetPorts();

    protected abstract UsbSerialPort GetPort(UsbManager manager, UsbDevice device, int port, ILogger logger);
}
