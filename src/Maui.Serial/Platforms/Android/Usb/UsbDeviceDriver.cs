using System.Diagnostics;
using Android.Hardware.Usb;
using Microsoft.Extensions.Logging;

namespace Maui.Serial.Platforms.Android.Usb;

public abstract class UsbDeviceDriver
{
    protected UsbDeviceDriver(
        UsbManager manager,
        UsbDevice device,
        ILogger logger)
    {
        UsbManager = manager;
        UsbDevice = device;
        Logger = logger;
    }

    public UsbManager UsbManager { get; }
    public UsbDevice UsbDevice { get; }
    public ILogger Logger { get; }
}
