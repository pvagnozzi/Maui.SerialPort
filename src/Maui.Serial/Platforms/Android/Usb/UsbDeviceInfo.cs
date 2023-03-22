using Android.OS;
using Java.Interop;

namespace Maui.Serial.Platforms.Android.Usb;

public class UsbDeviceInfo : Java.Lang.Object, IParcelable
{
    private static readonly IParcelableCreator _creator = new ParcelableCreator();

    [ExportField("CREATOR")]
    public static IParcelableCreator GetCreator()
    {
        return _creator;
    }

    public UsbDeviceInfo()
    {

    }
    public UsbDeviceInfo(UsbDeviceDriver port)
    {
        var device = port.UsbDevice;
        VendorId = device.VendorId;
        DeviceId = device.DeviceId;
    }

    internal UsbDeviceInfo(Parcel parcel)
    {
        VendorId = parcel.ReadInt();
        DeviceId = parcel.ReadInt();
    }

    public int VendorId { get; }
    public int DeviceId { get; }
    public int DescribeContents() => 0;

    public void WriteToParcel(Parcel dest, ParcelableWriteFlags flags)
    {
        dest.WriteInt(VendorId);
        dest.WriteInt(DeviceId);
    }
}
