using Android.Hardware.Usb;
using Maui.Serial.Platforms.Android.Usb;
using Microsoft.Extensions.Logging;
// ReSharper disable InconsistentNaming

namespace Maui.Serial.Platforms.Android.Drivers.Ch43x;

public class Ch340UsbSerialPortDriver : CommonUsbSerialPort
{
    #region Consts
    private const int USB_TIMEOUT_MILLIS = 5000;
    private const int DEFAULT_BAUD_RATE = 9600;

    private const int SCL_DTR = 0x20;
    private const int SCL_RTS = 0x40;
    private const int LCR_ENABLE_RX = 0x80;
    private const int LCR_ENABLE_TX = 0x40;
    private const int LCR_STOP_BITS_2 = 0x04;
    private const int LCR_CS8 = 0x03;
    private const int LCR_CS7 = 0x02;
    private const int LCR_CS6 = 0x01;
    private const int LCR_CS5 = 0x00;

    private const int LCR_MARK_SPACE = 0x20;
    private const int LCR_PAR_EVEN = 0x10;
    private const int LCR_ENABLE_PAR = 0x08;

    private static readonly int[] _baudCodes = {
        2400, 0xd901, 0x0038, 4800, 0x6402,
        0x001f, 9600, 0xb202, 0x0013, 19200, 0xd902, 0x000d, 38400,
        0x6403, 0x000a, 115200, 0xcc03, 0x0008
    };
    #endregion

    #region Fields
    private bool _dtr;
    private bool _rts;
    #endregion

    public Ch340UsbSerialPortDriver(UsbManager manager, UsbDevice device, int portNumber, ILogger logger) : base(manager, device, portNumber, logger)
    {
    }

    public override bool GetCD() => false;
    public override bool GetCTS() => false;
    public override bool GetDSR() => false;
    public override bool GetDTR() => _dtr;
    public override void SetDTR(bool value)
    {
        _dtr = value;
        SetControlLines();
    }
    public override bool GetRI() => false;
    public override bool GetRTS() => _rts;
    public override void SetRTS(bool value)
    {
        _rts = value;
        SetControlLines();
    }

    protected override void SetInterfaces(UsbDevice device)
    {
        var dataIface = UsbDevice.GetInterface(UsbDevice.InterfaceCount - 1);

        for (var i = 0; i < dataIface.EndpointCount; i++)
        {
            var ep = dataIface.GetEndpoint(i);
            if (ep?.Type != (UsbAddressing)UsbSupport.UsbEndpointXferBulk)
            {
                continue;
            }

            if (ep.Direction == (UsbAddressing)UsbSupport.UsbDirIn)
            {
                SetReadEndPoint(ep);
                continue;
            }
            SetWriteEndPoint(ep);
        }
    }

    protected override void SetParameters(UsbDeviceConnection connection, SerialPortParameters parameters)
    {
        Initialize();
        SetBaudRate(parameters.BaudRate);

        var lcr = LCR_ENABLE_RX | LCR_ENABLE_TX;

        lcr |= parameters.DataBits switch
        {
            DataBits.Bits5 => LCR_CS5,
            DataBits.Bits6 => LCR_CS6,
            DataBits.Bits7 => LCR_CS7,
            DataBits.Bits8 => LCR_CS8,
            _ => throw new Java.Lang.IllegalArgumentException($"Invalid data bits: {parameters.DataBits}"),
        };

        lcr |= parameters.Partity switch
        {
            Parity.None => lcr,
            Parity.Odd => LCR_ENABLE_PAR,
            Parity.Even => LCR_ENABLE_PAR | LCR_PAR_EVEN,
            Parity.Mark => LCR_ENABLE_PAR | LCR_MARK_SPACE,
            Parity.Space => LCR_ENABLE_PAR | LCR_MARK_SPACE | LCR_PAR_EVEN,
            _ => throw new Java.Lang.IllegalArgumentException($"Invalid parity: {parameters.Partity}"),
        };

        lcr |= parameters.StopBits switch
        {
            StopBits.One => lcr,
            StopBits.OnePointFive => throw new Java.Lang.UnsupportedOperationException("Unsupported stop bits: 1.5"),
            StopBits.Two => LCR_STOP_BITS_2,
            _ => throw new Java.Lang.IllegalArgumentException($"Invalid stop bits: {parameters.StopBits}")
        };

        var ret = ControlOut(0x9a, 0x2518, lcr);
        if (ret < 0)
        {
            throw new IOException("Error setting control byte");
        }
    }
    private int ControlOut(int request, int value, int index)
    {
        const int REQTYPE_HOST_TO_DEVICE = UsbConstants.UsbTypeVendor | UsbSupport.UsbDirOut;
        return UsbConnection.ControlTransfer((UsbAddressing)REQTYPE_HOST_TO_DEVICE, request,
            value, index, null, 0, USB_TIMEOUT_MILLIS);
    }

    private int ControlIn(int request, int value, int index, byte[] buffer)
    {
        const int REQTYPE_HOST_TO_DEVICE = UsbConstants.UsbTypeVendor | UsbSupport.UsbDirIn;
        return UsbConnection.ControlTransfer((UsbAddressing)REQTYPE_HOST_TO_DEVICE, request,
            value, index, buffer, buffer.Length, USB_TIMEOUT_MILLIS);
    }

    private void CheckState(string msg, int request, int value, IReadOnlyList<int> expected)
    {
        var buffer = new byte[expected.Count];
        var ret = ControlIn(request, value, 0, buffer);

        if (ret < 0)
        {
            throw new IOException($"Failed send cmd [{msg}]");
        }

        if (ret != expected.Count)
        {
            throw new IOException($"Expected {expected.Count} bytes, but get {ret} [{msg}]");
        }

        for (var i = 0; i < expected.Count; i++)
        {
            if (expected[i] == -1)
            {
                continue;
            }

            var current = buffer[i] & 0xff;
            if (expected[i] != current)
            {
                throw new IOException($"Expected 0x{expected[i]:X} bytes, but get 0x{current:X} [ {msg} ]");
            }
        }
    }

    private void SetControlLines()
    {
        if (ControlOut(0xa4, ~((_dtr ? SCL_DTR : 0) | (_rts ? SCL_RTS : 0)), 0) < 0)
        {
            throw new IOException("Failed to set control lines");
        }
    }

    private void Initialize()
    {
        CheckState("init #1", 0x5f, 0, new[] { -1 /* 0x27, 0x30 */, 0x00 });

        if (ControlOut(0xa1, 0, 0) < 0)
        {
            throw new IOException("init failed! #2");
        }

        SetBaudRate(DEFAULT_BAUD_RATE);

        CheckState("init #4", 0x95, 0x2518, new[] { -1 /* 0x56, c3*/, 0x00 });

        if (ControlOut(0x9a, 0x2518, 0x0050) < 0)
        {
            throw new IOException("init failed! #5");
        }

        CheckState("init #6", 0x95, 0x0706, new[] { -1 /*0xf?*/, -1 /*0xec,0xee*/});

        if (ControlOut(0xa1, 0x501f, 0xd90a) < 0)
        {
            throw new IOException("init failed! #7");
        }

        SetBaudRate(DEFAULT_BAUD_RATE);
        SetControlLines();
        CheckState("init #10", 0x95, 0x0706, new[] { -1 /* 0x9f, 0xff*/, 0xee });
    }

    private void SetBaudRate(int baudRate)
    {

        for (var i = 0; i < _baudCodes.Length / 3; i++)
        {
            if (_baudCodes[i * 3] != baudRate)
            {
                continue;
            }

            var ret = ControlOut(0x9a, 0x1312, _baudCodes[i * 3 + 1]);
            if (ret < 0)
            {
                throw new IOException("Error setting baud rate. #1");
            }

            ret = ControlOut(0x9a, 0x0f2c, _baudCodes[i * 3 + 2]);
            if (ret < 0)
            {
                throw new IOException("Error setting baud rate. #1");
            }

            return;
        }


        throw new IOException("Baud rate " + baudRate + " currently not supported");
    }
}