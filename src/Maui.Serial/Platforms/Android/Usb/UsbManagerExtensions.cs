using Android.App;
using Android.Content;
using Android.Hardware.Usb;

namespace Maui.Serial.Platforms.Android.Usb;

public static class UsbManagerExtensions
{
    public static void RequestPermission(this UsbManager usbManager, UsbDevice usbDevice)
    {
        if (usbManager.HasPermission(usbDevice))
        {
            return;
        }

        usbManager.RequestPermission(usbDevice, null);
    }

    public static UsbManager GetUsbManager(this Activity activity) =>

        (UsbManager)activity.GetSystemService(Context.UsbService);
}
