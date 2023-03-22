using System.Diagnostics;
using Android.Content;
using Android.Hardware.Usb;

namespace Maui.Serial.Platforms.Android.Usb;

public class UsbPermissionReceiver : BroadcastReceiver
{
    private TaskCompletionSource<bool> CompletionSource { get; }

    [DebuggerStepThrough]
    public UsbPermissionReceiver(TaskCompletionSource<bool> completionSource) =>
        CompletionSource = completionSource;

    public override void OnReceive(Context context, Intent intent)
    {
        intent.GetParcelableExtra(UsbManager.ExtraDevice);
        var permissionGranted = intent.GetBooleanExtra(UsbManager.ExtraPermissionGranted, false);
        context.UnregisterReceiver(this);
        CompletionSource.TrySetResult(permissionGranted);
    }
}
