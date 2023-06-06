using System.IO.Ports;
using Microsoft.Extensions.Logging;
using IOPort = System.IO.Ports.SerialPort;

// ReSharper disable once CheckNamespace
namespace Maui.Serial.Platforms.Windows;

public class SerialPortWindows : SerialPortBase
{
    public SerialPortWindows(string portName, SerialPortParameters parameters = null, ILogger logger = null) : base(portName,
        parameters, logger)
    {
    }

    protected IOPort Port { get; private set; }

    protected override void OpenPort(string portName, SerialPortParameters parameters)
    {
        Port = new IOPort(PortName, parameters!.BaudRate, System.IO.Ports.Parity.None)
        {
            ReadBufferSize = Parameters.ReadBufferSize,
            ReadTimeout = Parameters.ReadTimeout,
            StopBits = System.IO.Ports.StopBits.One,
            WriteBufferSize = parameters.WriteBufferSize,
            WriteTimeout = parameters.WriteTimeout
        };

        Port.Open();
        Port.Encoding = parameters.Encoding;
        Port.Handshake = Handshake.None;
        Port.NewLine = parameters.NewLine;
        Port.DataReceived += (_, args) =>
            OnDataReceived(args.EventType == SerialData.Chars ? SerialDataEventType.Chars : SerialDataEventType.Eof);
        Port.ErrorReceived += (_, _) => OnErrorReceived();
    }

    protected override int ReadFromPort(byte[] data) => Port.Read(data, 0, data.Length);

    protected override string ReadLineFromPort()
    {
        try
        {
            return Port.ReadLine();
        }
        catch (TimeoutException)
        {
            return string.Empty;
        }
    }

    protected override string ReadExistingFromPort() => Port.ReadExisting();

    protected override void WriteToPort(byte[] data) => Port.Write(data, 0, data.Length);

    protected override void WriteToPort(string value) => Port.Write(value);

    protected override void WriteLineToPort(string line) => Port.WriteLine(line);

    protected override void ClosePort()
    {
        if (Port is null)
        {
            return;
        }

        Port.Close();
        Port.Dispose();
        Port = null;
    }
}