using System.Diagnostics;

namespace Maui.Serial.Platforms.Android;

public static class EventHandlerExtensions
{
    [DebuggerStepThrough]
    public static void Raise(this EventHandler handler, object sender, EventArgs e) =>
        Volatile.Read(ref handler)?.Invoke(sender, e);

    [DebuggerStepThrough]
    public static void Raise<T>(this EventHandler<T> handler, object sender, T e) 
        where T : EventArgs => Volatile.Read(ref handler)?.Invoke(sender, e);
}
