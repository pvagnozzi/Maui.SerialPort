namespace Maui.Serial;

public interface ISerialPort : IDisposable
{
    bool IsOpen { get; }
    string PortName { get; }
    SerialPortParameters Parameters { get; }
    void Open(SerialPortParameters parameters = null);
    int Read(byte[] data);
    string ReadLine();
    string ReadExisting();
    void Write(byte[] data);
    void Write(string value);
    void WriteLine(string line);
    void Close();

    event EventHandler<SerialPortDataReceivedEventArgs> DataReceived;
    event EventHandler<SerialPortErrorEventArgs> ErrorReceived;
}
