using Android.Hardware.Usb;
using Maui.Serial.Platforms.Android.Usb;
using Microsoft.Extensions.Logging;

namespace Maui.Serial.Platforms.Android.Drivers.Ftdi;

[UsbDeviceDriver(UsbId.VENDOR_FTDI,
    new[] { UsbId.FTDI_FT232R, UsbId.FTDI_FT232H, UsbId.FTDI_FT2232H, UsbId.FTDI_FT4232H, UsbId.FTDI_FT231X, })]
public class FtdiUsbSerialDriver : CommonUsbSerialDriver
{
    public FtdiUsbSerialDriver(UsbManager manager, UsbDevice device, ILogger logger) :
        base(manager, device, logger)
    {
    }

    protected override UsbSerialPort GetPort(UsbManager manager, UsbDevice device, int port, ILogger logger) =>
        new FtdiUsbSerialPort(manager, device, port, logger);
}