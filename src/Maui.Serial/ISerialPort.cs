namespace Maui.Serial;

public interface ISerialPort : IDisposable
{
    SerialPortParameters Parameters { get; }
    void Open(SerialPortParameters parameters);
    int Read(byte[] data);
    string ReadLine();
    string ReadExisting();
    void Write(byte[] data);
    void Write(string value);
    void WriteLine(string line);
    void Close();

    event EventHandler<SerialDataReceivedArgs> DataReceived;
    event EventHandler<UnhandledExceptionEventArgs> ErrorReceived;
}
