namespace Maui.Serial.Platforms.Android.Usb;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class UsbDeviceDriverAttribute : Attribute
{
    public UsbDeviceDriverAttribute(int vendorId, int[] deviceIds) =>
        DriverSupport = new UsbDeviceDriverSupport(vendorId, deviceIds);

    public UsbDeviceDriverSupport DriverSupport { get; }

    public UsbDeviceDriverId[] DriverIds =>
        DriverSupport.DeviceIds.Select(x => new UsbDeviceDriverId(DriverSupport.VendorId, x)).ToArray();
}

