using System.Text;

namespace Maui.Serial;

public record SerialPortParameters(int BaudRate = 115200, DataBits DataBits = DataBits.Bits8,
    StopBits StopBits = StopBits.One, Parity Partity = Parity.None, FlowControl FlowControl = FlowControl.None,
    int ReadBufferSize = 16 * 1024, int WriteBufferSize = 16 * 1024, int ReadTimeout = 2000, int WriteTimeout = 2000,
    string NewLine = null, Encoding Encoding = null)
{
    public Encoding Encoding { get; } = Encoding ?? Encoding.UTF8;

    public string NewLine { get; } = NewLine ?? "\n";
}
