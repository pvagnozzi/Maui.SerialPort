using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Android.Hardware.Usb;
using Java.Lang;
using Java.Lang.Reflect;
using Microsoft.Extensions.Logging;

namespace Maui.Serial.Platforms.Android.Usb;

internal class UsbDeviceProber
{
    public UsbDeviceProber(ILogger logger = null)
    {
        Logger = logger;
    }

    public ILogger Logger { get; }

    public IDictionary<UsbDeviceDriverId, Type> Drivers { get; } =
        new ConcurrentDictionary<UsbDeviceDriverId, Type>();

    public void RegisterDriver(UsbDeviceDriverId driverId, Type driverType)
    {
        if (Drivers.ContainsKey(driverId))
        {
            return;
        }

        Drivers.Add(driverId, driverType);
    }
    
    [DebuggerStepThrough]
    public void RegisterDriver(IEnumerable<UsbDeviceDriverId> driverIds, Type driverType)
    {
        foreach (var driverId in driverIds)
        {
            RegisterDriver(driverId, driverType);
        }
    }

    public void RegisterDriver(Type driverType)
    {
        var attributes = driverType.GetCustomAttributes<UsbDeviceDriverAttribute>().ToArray();
        if (!attributes.Any())
        {
            throw new ArgumentException($"{driverType.FullName} has no UsbDeviceDriverAttribute");
        }

        foreach (var attribute in attributes)
        {
            RegisterDriver(attribute.DriverIds, driverType);
        }
    }

    public void RegisterDriver(Assembly assembly)
    {
        var driverTypes = assembly.GetTypes()
            .Where(x => x.IsSubclassOf(typeof(UsbDeviceDriver)) && !x.IsAbstract).ToArray();

        foreach (var driverType in driverTypes)
        {
            RegisterDriver(driverType);
        }
    }

    [DebuggerStepThrough]
    public IEnumerable<UsbDeviceDriver> Scan(UsbManager usbManager) => 
        usbManager.DeviceList?.Select(device => ProbeDevice(usbManager, device.Value)).Where(driver => driver is not null);

    public UsbDeviceDriver ProbeDevice(UsbManager usbManager, UsbDevice usbDevice)
    {
        usbManager.RequestPermission(usbDevice, null);
        var vendorId = usbDevice.VendorId;
        var productId = usbDevice.ProductId;

        var driverKey = new UsbDeviceDriverId(vendorId, productId);

        if (!Drivers.TryGetValue(driverKey, out var driverType))
        {
            throw new NotSupportedException($"UsbDevice Driver {vendorId}/{productId} not found");
        }

        try
        {
            return (UsbDeviceDriver)Activator.CreateInstance(driverType, usbManager, usbDevice, Logger);
        }
        catch (NoSuchMethodException e)
        {
            throw new RuntimeException(e);
        }
        catch (IllegalArgumentException e)
        {
            throw new RuntimeException(e);
        }
        catch (InstantiationException e)
        {
            throw new RuntimeException(e);
        }
        catch (IllegalAccessException e)
        {
            throw new RuntimeException(e);
        }
        catch (InvocationTargetException e)
        {
            throw new RuntimeException(e);
        }
    }

}
