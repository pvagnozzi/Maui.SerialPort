using Android.Runtime;
using Java.Lang;

namespace Maui.Serial.Platforms.Android.Usb;

public class UsbSerialRuntimeException : RuntimeException
{
    public UsbSerialRuntimeException()
    {
    }

    public UsbSerialRuntimeException(Throwable throwable) :
        base(throwable)
    {
    }

    public UsbSerialRuntimeException(string detailMessage) :
        base(detailMessage)
    {
    }

    public UsbSerialRuntimeException(string detailMessage, Throwable throwable) :
        base(detailMessage, throwable)
    {
    }

    protected UsbSerialRuntimeException(nint javaReference, JniHandleOwnership transfer) :
        base(javaReference, transfer)
    {
    }
}