using System.Diagnostics;
using Android.Hardware.Usb;
using Maui.Serial.Platforms.Android.Usb;
using Microsoft.Extensions.Logging;

namespace Maui.Serial.Platforms.Android.Drivers.Cp21xx;

[UsbDeviceDriver(UsbId.VENDOR_SILABS,
    new[] { UsbId.SILABS_CP2102, UsbId.SILABS_CP2105, UsbId.SILABS_CP2108, UsbId.SILABS_CP2110 })]
// ReSharper disable once InconsistentNaming
public class Cp21xxUsbSerialDriver : CommonUsbSerialDriver
{
    public Cp21xxUsbSerialDriver(UsbManager manager, UsbDevice device, ILogger logger) :
        base(manager, device, logger)
    {
    }

    [DebuggerStepThrough]
    protected override UsbSerialPort GetPort(UsbManager manager, UsbDevice device, int port, ILogger logger) =>
        new Cp21xxUsbSerialPort(manager, device, port, logger);
}