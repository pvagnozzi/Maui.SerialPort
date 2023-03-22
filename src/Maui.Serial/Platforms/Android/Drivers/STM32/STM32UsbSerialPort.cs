using System.Diagnostics;
using Android.Hardware.Usb;
using Microsoft.Extensions.Logging;
// ReSharper disable InconsistentNaming

namespace Maui.Serial.Platforms.Android.Drivers.STM32;

public class STM32UsbSerialPortDriver : CommonUsbSerialPort
{
    private const int USB_WRITE_TIMEOUT_MILLIS = 5000;
    private const int USB_RECIP_INTERFACE = 0x01;
    private const int USB_RT_AM = UsbConstants.UsbTypeClass | USB_RECIP_INTERFACE;
    private const int SET_LINE_CODING = 0x20;
    private const int SET_CONTROL_LINE_STATE = 0x22;

    private int _controlInterfaceIndex;
    private bool _rts;
    private bool _dtr;

    public STM32UsbSerialPortDriver(UsbManager manager, UsbDevice device, int portNumber, ILogger logger) :
        base(manager, device, portNumber, logger)
    {
    }

    public override bool GetCD() => false;
    public override bool GetCTS() => false;
    public override bool GetDSR() => false;
    public override bool GetDTR() => _dtr;
    public override void SetDTR(bool value)
    {
        _dtr = value;
        SetDtrRts();
    }
    public override bool GetRI() => false;
    public override bool GetRTS() => _rts;
    public override void SetRTS(bool value)
    {
        _rts = value;
        SetDtrRts();
    }

    protected override void SetInterfaces(UsbDevice device)
    {
        var (_, ctrlIndex) = FindInterface(device, x => x.InterfaceClass == UsbClass.Comm);
        var (dataInterface, _) = FindInterface(device, x => x.InterfaceClass == UsbClass.CdcData);
        SetReadEndPoint(dataInterface.GetEndpoint(1));
        SetWriteEndPoint(dataInterface.GetEndpoint(0));

        _controlInterfaceIndex = ctrlIndex;
    }

    protected override void SetParameters(UsbDeviceConnection connection, SerialPortParameters parameters)
    {
        byte stopBitsBytes = parameters.StopBits switch
        {
            StopBits.One => 0,
            StopBits.OnePointFive => 1,
            StopBits.Two => 2,
            _ => throw new ArgumentException($"Bad value for stopBits: {parameters.StopBits}")
        };

        byte parityBitesBytes = parameters.Partity switch
        {
            Parity.None => 0,
            Parity.Odd => 1,
            Parity.Even => 2,
            Parity.Mark => 3,
            Parity.Space => 4,
            _ => throw new ArgumentException($"Bad value for parity: {parameters.Partity}")
        };

        var baudRate = parameters.BaudRate;

        byte[] msg =
        {
            (byte)(baudRate & 0xff),
            (byte)(baudRate >> 8 & 0xff),
            (byte)(baudRate >> 16 & 0xff),
            (byte)(baudRate >> 24 & 0xff),
            stopBitsBytes,
            parityBitesBytes,
            (byte)parameters.DataBits
        };
        SendAcmControlMessage(SET_LINE_CODING, 0, msg);
    }

    [DebuggerStepThrough]
    private void SetDtrRts()
    {
        var value = (_rts ? 0x2 : 0) | (_dtr ? 0x1 : 0);
        SendAcmControlMessage(SET_CONTROL_LINE_STATE, value, null);
    }

    [DebuggerStepThrough]
    private void SendAcmControlMessage(int request, int value, byte[] buf) =>
        UsbConnection.ControlTransfer((UsbAddressing)USB_RT_AM, request, value, _controlInterfaceIndex, buf, buf?.Length ?? 0, USB_WRITE_TIMEOUT_MILLIS);
}

