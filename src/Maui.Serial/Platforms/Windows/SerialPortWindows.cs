using System.IO.Ports;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Maui.Serial.Platforms.Windows;

public class SerialPortWindows : SerialPortBase
{
    public SerialPortWindows(string portName, SerialPortParameters parameters = null, ILogger logger = null) : base(portName,
        parameters, logger)
    {
    }

    protected SerialPort Port { get; private set; }

    protected override void OpenPort(string portName, SerialPortParameters parameters)
    {
        Port = new SerialPort(PortName, parameters!.BaudRate, ConvertParity(parameters.Partity))
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

    private static System.IO.Ports.Parity ConvertParity(Parity parity) => parity switch
    {
        Parity.Even => System.IO.Ports.Parity.Even,
        Parity.Mark => System.IO.Ports.Parity.Mark,
        Parity.None => System.IO.Ports.Parity.None,
        Parity.Odd => System.IO.Ports.Parity.Odd,
        Parity.Space => System.IO.Ports.Parity.Space,
        _ => throw new ArgumentOutOfRangeException(nameof(parity), parity, null)
    };

    private static System.IO.Ports.StopBits ConvertStopBits(StopBits stopBits) => stopBits switch
    {
        StopBits.NotSet => System.IO.Ports.StopBits.None,
        StopBits.One => System.IO.Ports.StopBits.One,
        StopBits.OnePointFive => System.IO.Ports.StopBits.OnePointFive,
        StopBits.Two => System.IO.Ports.StopBits.Two,
        _ => throw new ArgumentOutOfRangeException(nameof(stopBits), stopBits, null)
    };
}