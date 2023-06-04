using System.Diagnostics;

namespace Maui.Serial.Platforms.Android;

internal class PollingTask : IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Action<CancellationToken> _pollingAction;
    private readonly TimeSpan _pollingInterval;
    private CancellationToken _cancellationToken = CancellationToken.None;
    private Task _task;

    public PollingTask(Action<CancellationToken> action, TimeSpan? interval = null)
    {
        _pollingAction = action;
        _pollingInterval = interval ?? TimeSpan.Zero;
    }

    public bool IsRunning => _task is not null && !_task.IsCanceled && !_task.IsCompleted && !_task.IsFaulted && _task.IsCompletedSuccessfully;

    public virtual void Start()
    {
        if (IsRunning)
        {
            return;
        }

        _cancellationToken = _cancellationTokenSource.Token;
        _task = Task.Factory.StartNew(Polling, _cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public virtual void Stop()
    {
        if (!IsRunning)
        {
            return;
        }

        _cancellationTokenSource.Cancel();
        _task.Wait(10);
    }

    [DebuggerStepThrough]
    public virtual void Dispose()
    {
        if (IsRunning)
        {
            Stop();
        }

        GC.SuppressFinalize(this);
    }

    protected virtual void Polling()
    {
        try
        {
            while (true)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                var startTime = DateTime.Now;
                _pollingAction(_cancellationToken);
                var duration = DateTime.Now - startTime;
                var delay = _pollingInterval - duration;

                if (delay < TimeSpan.Zero)
                {
                    continue;
                }

                Task.Delay(delay, _cancellationToken).Wait(_cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}

