using System.Diagnostics;
using Android.Hardware.Usb;
using Maui.Serial.Platforms.Android.Usb;
using Microsoft.Extensions.Logging;

namespace Maui.Serial.Platforms.Android.Drivers;

public abstract class CommonUsbSerialDriver : UsbSerialDriver
{
    protected CommonUsbSerialDriver(
        UsbManager manager,
        UsbDevice device,
        ILogger logger) :
        base(manager, device, logger)
    {
    }

    [DebuggerStepThrough]
    public override IEnumerable<UsbSerialPort> GetPorts() => new[] { GetPort(UsbManager, UsbDevice, 0, Logger) };
}
