using System.Text;

namespace Maui.Serial;

public record SerialPortParameters(int BaudRate = 115200, DataBits DataBits = DataBits.Bits8,
    StopBits StopBits = StopBits.One, Parity Partity = Parity.None, FlowControl FlowControl = FlowControl.None,
    int ReadBufferSize = 16 * 1024, int WriteBufferSize = 16 * 1024, int ReadTimeout = 10, int WriteTimeout = 10,
    string NewLine = null, Encoding Encoding = null);
