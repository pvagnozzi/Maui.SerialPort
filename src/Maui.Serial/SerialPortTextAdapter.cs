using System.Diagnostics;
using System.Text;

namespace Maui.Serial;

public class SerialPortTextAdapter : IDisposable
{
    private string _buffer = string.Empty;

    public SerialPortTextAdapter(ISerialPort serialPort, Encoding encoding = null, string newLine = null)
    {
        SerialPort = serialPort;
        Encoding = encoding ?? Encoding.Default;
        NewLine = newLine ?? Environment.NewLine;
    }

    public ISerialPort SerialPort { get; }
    public Encoding Encoding { get; }
    public string NewLine { get; }

    public string Read(int timeout = -1)
    {
        var buffer = new byte[SerialPort.Parameters.ReadBufferSize];
        var length = SerialPort.Read(buffer, timeout);
        var result = length > 0 ? Encoding.GetString(buffer, 0, length) : string.Empty;

        if (string.IsNullOrEmpty(_buffer))
        {
            return result;
        }

        result = _buffer + result;
        _buffer = string.Empty;
        return result;
    }

    public string ReadLine(int timeout = -1)
    {
        var sb = new StringBuilder();
        
        while (true)
        {
            var token = Read(timeout);
            if (string.IsNullOrEmpty(token))
            {
                _buffer = sb.ToString();
                return string.Empty;
            }

            if (token.EndsWith(NewLine))
            {
                sb.Append(token[..^NewLine.Length]);
                var result = sb.ToString();
                return result;
            }

            var index = token.IndexOf(NewLine, StringComparison.Ordinal);
            if (index < 0)
            {
                sb.Append(token);
                continue;
            }

            sb.Append(token[..index]);
            _buffer = token[(index + NewLine.Length)..];
            return sb.ToString();
        }
    }

    [DebuggerStepThrough]
    public virtual void Write(string value, int timeout = -1) =>
        SerialPort.Write(Encoding.GetBytes(value), timeout);

    [DebuggerStepThrough]
    public virtual void WriteLine(string value, int timeout = -1) =>
        Write(value + NewLine, timeout);

    public virtual void Dispose()
    {
        SerialPort?.Dispose();
        GC.SuppressFinalize(this);
    }
}

