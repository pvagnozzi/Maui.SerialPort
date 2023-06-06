using Android.Hardware.Usb;
using Microsoft.Extensions.Logging;
// ReSharper disable InconsistentNaming

namespace Maui.Serial.Platforms.Android.Usb;

public abstract class UsbSerialPort : UsbConnectionDriver
{
    protected const int DATABITS_5 = 5;
    protected const int DATABITS_6 = 6;
    protected const int DATABITS_7 = 7;
    protected const int DATABITS_8 = 8;

    protected const int FLOWCONTROL_NONE = 0;
    protected const int FLOWCONTROL_RTSCTS_IN = 1;
    protected const int FLOWCONTROL_RTSCTS_OUT = 2;
    protected const int FLOWCONTROL_XONXOFF_IN = 4;
    protected const int FLOWCONTROL_XONXOFF_OUT = 8;

    protected const int PARITY_NONE = 0;
    protected const int PARITY_ODD = 1;
    protected const int PARITY_EVEN = 2;
    protected const int PARITY_MARK = 3;
    protected const int PARITY_SPACE = 4;

    protected const int STOPBITS_1 = 1;
    protected const int STOPBITS_1_5 = 3;
    protected const int STOPBITS_2 = 2;

    protected UsbSerialPort(
        UsbManager manager,
        UsbDevice device,
        int portNumber,
        ILogger logger) :
        base(manager, device, logger)
    {
        PortNumber = portNumber;
        Parameters = new SerialPortParameters();
    }
    public int PortNumber { get; }
    public string PortName => $"{UsbDevice.DeviceName}/{PortNumber}";
    public SerialPortParameters Parameters { get; set; }

    public abstract int Read(byte[] dest);
    public abstract int Write(byte[] src);
    public abstract bool GetCD();
    public abstract bool GetCTS();
    public abstract bool GetDSR();
    public abstract bool GetDTR();
    public abstract void SetDTR(bool value);
    public abstract bool GetRI();
    public abstract bool GetRTS();
    public abstract void SetRTS(bool value);
}
