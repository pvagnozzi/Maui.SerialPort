using System.Diagnostics;
using Android.Hardware.Usb;
using Maui.Serial.Platforms.Android.Usb;
using Microsoft.Extensions.Logging;
// ReSharper disable InconsistentNaming

namespace Maui.Serial.Platforms.Android.Drivers.STM32;

[UsbDeviceDriver(UsbId.VENDOR_STM, new[] { UsbId.STM32_STLINK, UsbId.STM32_VCOM })]
public class STM32UsbSerialDriver : CommonUsbSerialDriver
{
    public STM32UsbSerialDriver(UsbManager manager, UsbDevice device, ILogger logger) :
        base(manager, device, logger)
    {
    }

    [DebuggerStepThrough]
    protected override UsbSerialPort GetPort(UsbManager manager, UsbDevice device, int port, ILogger logger) =>
        new STM32UsbSerialPortDriver(manager, device, port, logger);
}

