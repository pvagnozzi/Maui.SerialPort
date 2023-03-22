// ReSharper disable AsyncConverter.AsyncWait

using System.Diagnostics;

namespace Maui.Serial.Platforms.Android.Usb;

public class UsbSerialPortMonitor : IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private CancellationToken _cancellationToken = CancellationToken.None;

    private Task _task;

    public UsbSerialPortMonitor(UsbSerialPort serialPort)
    {
        SerialPort = serialPort;
    }

    public UsbSerialPort SerialPort { get; }

    public bool IsRunning => _task is not null && !_task.IsCanceled && !_task.IsCompleted && !_task.IsFaulted &&
                             _task.IsCompletedSuccessfully;

    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        _cancellationToken = _cancellationTokenSource.Token;
        _task = Task.Factory.StartNew(Polling, _cancellationToken, TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    public void Stop()
    {
        if (!IsRunning)
        {
            return;
        }

        _cancellationTokenSource.Cancel();
        _task.Wait(10);
    }

    [DebuggerStepThrough]
    public void Dispose()
    {
        if (IsRunning)
        {
            Stop();
        }

        GC.SuppressFinalize(this);
    }

    public event EventHandler<SerialDataReceivedArgs> DataReceived;

    public event EventHandler<UnhandledExceptionEventArgs> ErrorReceived;

    protected virtual void Polling()
    {
        try
        {
            var buffer = new byte[SerialPort.Parameters.ReadBufferSize];
            while (DataReceived is not null)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                var dataRead = SerialPort.Read(buffer);
                if (dataRead <= 0)
                {
                    continue;
                }

                var data = new byte[dataRead];
                Array.Copy(buffer, data, dataRead);

                DataReceived.Raise(SerialPort, new SerialDataReceivedArgs(data));
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            ErrorReceived.Raise(SerialPort, new UnhandledExceptionEventArgs(e, false));
        }
    }
}

