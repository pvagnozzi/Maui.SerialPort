using Android.App;
using Android.Content;
using Android.Hardware.Usb;

namespace Maui.Serial.Platforms.Android.Usb;

public static class UsbManagerExtensions
{
    public static Task<bool> RequestPermissionAsync(this UsbManager manager, UsbDevice device, Context context,
        string permission, TaskCompletionSource<bool> completionSource = null)
    {
        completionSource ??= new TaskCompletionSource<bool>();
        var usbPermissionReceiver = new UsbPermissionReceiver(completionSource);
        context.RegisterReceiver(usbPermissionReceiver, new IntentFilter(permission));
        var intent = PendingIntent.GetBroadcast(context, 0, new Intent(permission), 0);
        manager.RequestPermission(device, intent);
        return completionSource.Task;
    }

    public static UsbManager GetUsbManager(this Activity activity) =>

        (UsbManager)activity.GetSystemService(Context.UsbService);
    
}
