using Android.OS;
using Object = Java.Lang.Object;

namespace Maui.Serial.Platforms.Android.Usb;

public sealed class ParcelableCreator : Object, IParcelableCreator
{
    public Object CreateFromParcel(Parcel parcel) => new UsbDeviceInfo(parcel);

    public Object[] NewArray(int size) => new Object[size];
}
