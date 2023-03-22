using System.Diagnostics;
using Android.Hardware.Usb;
using Maui.Serial.Platforms.Android.Usb;
using Microsoft.Extensions.Logging;

namespace Maui.Serial.Platforms.Android.Drivers.Prolific;

[UsbDeviceDriver(UsbId.VENDOR_PROLIFIC,
    new[]
    {
        UsbId.PROLIFIC_PL2303, UsbId.PROLIFIC_PL2303GC, UsbId.PROLIFIC_PL2303GB, UsbId.PROLIFIC_PL2303GT,
        UsbId.PROLIFIC_PL2303GL, UsbId.PROLIFIC_PL2303GE, UsbId.PROLIFIC_PL2303GS
    })]
public class ProlificUsbSerialDriver : CommonUsbSerialDriver
{
    public ProlificUsbSerialDriver(UsbManager manager, UsbDevice device, ILogger logger) :
        base(manager, device, logger)
    {
    }

    [DebuggerStepThrough]
    protected override UsbSerialPort GetPort(UsbManager manager, UsbDevice device, int port, ILogger logger) =>
        new ProlificUsbSerialPortDriver(manager, device, port, logger);
}

