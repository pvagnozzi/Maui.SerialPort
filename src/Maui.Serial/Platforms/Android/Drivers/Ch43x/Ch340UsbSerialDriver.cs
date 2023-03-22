using System.Diagnostics;
using Android.Hardware.Usb;
using Maui.Serial.Platforms.Android.Usb;
using Microsoft.Extensions.Logging;

namespace Maui.Serial.Platforms.Android.Drivers.Ch43x;

[UsbDeviceDriver(UsbId.VENDOR_QINHENG, new[] { UsbId.QINHENG_HL340 })]
public class Ch430UsbSerialDriver : CommonUsbSerialDriver
{
    public Ch430UsbSerialDriver(UsbManager manager, UsbDevice device, ILogger logger) : base(manager, device, logger)
    {
    }

    [DebuggerStepThrough]
    protected override UsbSerialPort GetPort(UsbManager manager, UsbDevice device, int port, ILogger logger) =>
        new Ch340UsbSerialPortDriver(manager, device, port, logger);
}

