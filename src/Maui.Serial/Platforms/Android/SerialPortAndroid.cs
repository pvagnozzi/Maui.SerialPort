using Android.App;
using Android.Hardware.Usb;
using Maui.Serial.Platforms.Android.Usb;
using Microsoft.Extensions.Logging;

namespace Maui.Serial.Platforms.Android;

public class SerialPortAndroid : SerialPortBase
{
    public SerialPortAndroid(string portName, SerialPortParameters parameters = null, ILogger logger = null) : base(portName, parameters, logger)
    {
        UsbSerialPort = SerialPortPlatform.GetUsbPortByName(portName);
        UsbSerialPort.Parameters = parameters ?? new SerialPortParameters();
        _pollingTask = new PollingTask(PollingAction);
    }


    private readonly List<byte> _readBuffer = new();
    private readonly PollingTask _pollingTask;
    private byte[] _pollingBuffer;


    protected internal UsbSerialPort UsbSerialPort { get; }

    protected override void OpenPort(string portName, SerialPortParameters parameters)
    {
        Close();
        UsbSerialPort.Parameters = parameters;
        UsbSerialPort.Open();

        var bufferSize = UsbSerialPort.Parameters.ReadBufferSize;
        _pollingBuffer = new byte[bufferSize];
        _pollingTask.Start();
    }

    protected override int ReadFromPort(byte[] data) => UsbSerialPort.Read(data);

    protected override string ReadLineFromPort()
    {
        var data = _readBuffer.ToArray();
        var separator = Parameters.Encoding.GetBytes(Parameters.NewLine);

        var index = Array.IndexOf(data, separator[0]);
        if (index < 0)
        {
            return string.Empty;
        }

        var len = index;
        if (separator.Length > 1)
        {
            if (index + 1 >= data.Length || data[index + 1] != separator[1])
            {
                return string.Empty;
            }
            len++;
        }

        var line = Parameters.Encoding.GetString(data, 0, index);
        _readBuffer.RemoveRange(0, len);
        return line;
    }

    protected override string ReadExistingFromPort()
    {
        ReadToBuffer();
        var data = _readBuffer.ToArray();
        _readBuffer.Clear();
        return Parameters.Encoding.GetString(data);
    }

    protected override void WriteToPort(byte[] data) => UsbSerialPort.Write(data);

    protected override void WriteToPort(string value) => WriteToPort(Parameters.Encoding.GetBytes(value));

    protected override void WriteLineToPort(string line) => WriteToPort(line + Parameters.NewLine);

    protected override void ClosePort()
    {
        _readBuffer.Clear();
        _pollingTask.Stop();
        UsbSerialPort.Close();
    }

    protected void ReadToBuffer()
    {
        try
        {
            var len = UsbSerialPort.Read(_pollingBuffer);
            if (len > 0)
            {
                _readBuffer.AddRange(_pollingBuffer.Take(len));
                OnDataReceived();
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
            OnErrorReceived();
        }
    }

    private void PollingAction(CancellationToken cancellation)
    {
        if (cancellation.IsCancellationRequested)
        {
            throw new TaskCanceledException();
        }

        ReadToBuffer();

    }
}