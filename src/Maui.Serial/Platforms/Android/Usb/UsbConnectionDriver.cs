using System.Diagnostics;
using Android.Hardware.Usb;
using Microsoft.Extensions.Logging;

namespace Maui.Serial.Platforms.Android.Usb;

public abstract class UsbConnectionDriver
{
    protected UsbConnectionDriver(
        UsbManager manager,
        UsbDevice device,
        ILogger logger)
    {
        UsbManager = manager;
        UsbDevice = device;
        Logger = logger;
    }

    public ILogger Logger { get; }
    public UsbManager UsbManager { get; }
    public UsbDevice UsbDevice { get; }
    public UsbDeviceConnection UsbConnection { get; protected set; }
    public virtual bool IsOpen => UsbConnection is not null;

    public virtual void Open()
    {
        if (IsOpen)
        {
            return;
        }
        UsbConnection = OpenConnection();
    }

    public virtual void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        UsbConnection.Close();
        UsbConnection.Dispose();
        UsbConnection = null;
    }

    [DebuggerStepThrough]
    public virtual void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }

    [DebuggerStepThrough]
    protected virtual UsbDeviceConnection OpenConnection() => UsbManager.OpenDevice(UsbDevice);
}
