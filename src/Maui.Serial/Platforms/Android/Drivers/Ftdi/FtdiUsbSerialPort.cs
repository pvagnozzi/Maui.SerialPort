using System.Diagnostics;
using Android.Hardware.Usb;
using Java.Lang;
using Microsoft.Extensions.Logging;
using Math = System.Math;
// ReSharper disable InconsistentNaming

namespace Maui.Serial.Platforms.Android.Drivers.Ftdi;

public class FtdiUsbSerialPort : CommonUsbSerialPort
{
    private const int USB_WRITE_TIMEOUT_MILLIS = 5000;
    private const int READ_HEADER_LENGTH = 2;

    private const int REQTYPE_HOST_TO_DEVICE = UsbConstants.UsbTypeVendor | 128;

    private const int RESET_REQUEST = 0;
    private const int MODEM_CONTROL_REQUEST = 1;
    private const int SET_BAUD_RATE_REQUEST = 3;
    private const int SET_DATA_REQUEST = 4;
    private const int GET_MODEM_STATUS_REQUEST = 5;

    private const int MODEM_CONTROL_DTR_ENABLE = 0x0101;
    private const int MODEM_CONTROL_DTR_DISABLE = 0x0100;
    private const int MODEM_CONTROL_RTS_ENABLE = 0x0202;
    private const int MODEM_CONTROL_RTS_DISABLE = 0x0200;
    private const int MODEM_STATUS_CTS = 0x10;
    private const int MODEM_STATUS_DSR = 0x20;
    private const int MODEM_STATUS_RI = 0x40;
    private const int MODEM_STATUS_CD = 0x80;
    private const int RESET_ALL = 0;
    private const int RESET_PURGE_RX = 1;
    private const int RESET_PURGE_TX = 2;

    private bool _dtr;
    private bool _rts;


    public FtdiUsbSerialPort(UsbManager manager, UsbDevice device, int portNumber, ILogger logger) : base(manager,
        device, portNumber, logger)
    {
    }

    [DebuggerStepThrough]
    public void Reset() => ExecuteCommand(RESET_REQUEST, RESET_ALL);

    [DebuggerStepThrough]
    public void Purge()
    {
        ExecuteCommand(RESET_REQUEST, RESET_PURGE_RX);
        ExecuteCommand(RESET_REQUEST, RESET_PURGE_TX);
    }

    protected override int CopyToReadBuffer(byte[] source, byte[] destination)
    {
        var maxPacketSize = ReadEndPoint.MaxPacketSize;
        var destPos = 0;
        var totalBytesRead = source.Length;

        for (var srcPos = 0; srcPos < totalBytesRead; srcPos += maxPacketSize)
        {
            var length = Math.Min(srcPos + maxPacketSize, totalBytesRead) - (srcPos + READ_HEADER_LENGTH);
            if (length < 0)
            {
                throw new IOException($"Expected at least {READ_HEADER_LENGTH} bytes");
            }

            Buffer.BlockCopy(source, srcPos + READ_HEADER_LENGTH, destination, destPos, length);
            destPos += length;
        }

        return destPos;
    }

    protected override void SetInterfaces(UsbDevice device)
    {
        var (usbInterface, index) = FindInterface(device, _ => true);
        SetReadEndPoint(usbInterface.GetEndpoint(0));
        SetWriteEndPoint(usbInterface.GetEndpoint(1));
    }

    protected override void SetParameters(
        UsbDeviceConnection connection,
        SerialPortParameters parameters)
    {

        SetBaudRate(parameters.BaudRate);
        var config = 0;

        config |= parameters.DataBits switch
        {
            DataBits.Bits5 => throw new UnsupportedOperationException($"Unsupported data bits: {parameters.DataBits}"),
            DataBits.Bits6 => throw new UnsupportedOperationException("Unsupported data bits: {parameters.DataBits}"),
            DataBits.Bits7 => DATABITS_7,
            DataBits.Bits8 => DATABITS_8,
            _ => throw new IllegalArgumentException($"Invalid data bits: {parameters.DataBits}")
        };

        switch (parameters.Partity)
        {
            case Parity.None:
                break;
            case Parity.Odd:
                config |= 0x100;
                break;
            case Parity.Even:
                config |= 0x200;
                break;
            case Parity.Mark:
                config |= 0x300;
                break;
            case Parity.Space:
                config |= 0x400;
                break;
            default:
                throw new IllegalArgumentException($"Unknown parity value: {parameters.BaudRate}");
        }

        switch (parameters.StopBits)
        {
            case StopBits.One:
                break;
            case StopBits.OnePointFive:
                throw new UnsupportedOperationException("Unsupported stop bits: 1.5");
            case StopBits.Two:
                config |= 0x1000;
                break;
            case StopBits.NotSet:
                break;
            default:
                throw new IllegalArgumentException($"Unknown stopBits value: {parameters.StopBits}");
        }

        ExecuteCommand(SET_DATA_REQUEST, config);

    }

    public override bool GetCD() => (GetStatus() & MODEM_STATUS_CD) != 0;

    public override bool GetCTS() =>
        (GetStatus() & MODEM_STATUS_CTS) != 0;

    public override bool GetDSR() =>
        (GetStatus() & MODEM_STATUS_DSR) != 0;

    public override bool GetDTR() => _dtr;

    public override void SetDTR(bool value)
    {
        var result = UsbConnection.ControlTransfer((UsbAddressing)REQTYPE_HOST_TO_DEVICE, MODEM_CONTROL_REQUEST,
            value ? MODEM_CONTROL_DTR_ENABLE : MODEM_CONTROL_DTR_DISABLE, PortNumber + 1, null, 0,
            USB_WRITE_TIMEOUT_MILLIS);
        if (result != 0)
        {
            throw new IOException($"Set DTR failed: result={result}");
        }

        _dtr = value;
    }

    public override bool GetRI() =>
        (GetStatus() & MODEM_STATUS_RI) != 0;

    public override bool GetRTS() => _rts;

    public override void SetRTS(bool value)
    {
        var result = UsbConnection.ControlTransfer((UsbAddressing)REQTYPE_HOST_TO_DEVICE, MODEM_CONTROL_REQUEST,
            value ? MODEM_CONTROL_RTS_ENABLE : MODEM_CONTROL_RTS_DISABLE, PortNumber + 1, null, 0,
            USB_WRITE_TIMEOUT_MILLIS);
        if (result != 0)
        {
            throw new IOException($"Set RTS failed: result={result}");
        }

        _rts = value;
    }


    private int GetStatus()
    {
        var data = new byte[2];
        ExecuteCommand(GET_MODEM_STATUS_REQUEST, 0, data, 2);
        return data[0];
    }

    private void ExecuteCommand(int command, int value, byte[] data = null, int expectedResult = 0, int portNumber = -1)
    {
        var result = ControlTransfer(
            (UsbAddressing)REQTYPE_HOST_TO_DEVICE,
            command,
            value,
            portNumber > 0 ? portNumber : PortNumber + 1,
            data,
            data?.Length ?? 0,
            USB_WRITE_TIMEOUT_MILLIS);

        if (result != expectedResult)
        {
            throw new IOException($"Command {command}/{value} failed: {result}");
        }
    }

    private void SetBaudRate(int baudRate)
    {
        if (baudRate <= 0)
        {
            throw new IllegalArgumentException($"Invalid baud rate: {baudRate}");
        }

        int divisor, subdivisor, effectiveBaudRate;

        switch (baudRate)
        {
            case > 3500000:
                throw new UnsupportedOperationException("Baud rate to high");
            case >= 2500000:
                divisor = 0;
                subdivisor = 0;
                effectiveBaudRate = 3000000;
                break;
            case >= 1750000:
                divisor = 1;
                subdivisor = 0;
                effectiveBaudRate = 2000000;
                break;
            default:
                divisor = (24000000 << 1) / baudRate;
                divisor = divisor + 1 >> 1; // round
                subdivisor = divisor & 0x07;
                divisor >>= 3;
                if (divisor > 0x3fff) // exceeds bit 13 at 183 baud
                    throw new UnsupportedOperationException("Baud rate to low");
                effectiveBaudRate = (24000000 << 1) / ((divisor << 3) + subdivisor);
                effectiveBaudRate = effectiveBaudRate + 1 >> 1;
                break;

        }

        var baudRateError = Math.Abs(1.0 - effectiveBaudRate / (double)baudRate);
        if (baudRateError >= 0.031)
        {
            throw new UnsupportedOperationException(
                "Baud rate deviation %.1f%% is higher than allowed 3%%");
        }

        var value = divisor;
        var index = 0;
        switch (subdivisor)
        {
            case 0: break; // 16,15,14 = 000 - sub-integer divisor = 0
            case 4:
                value |= 0x4000;
                break; // 16,15,14 = 001 - sub-integer divisor = 0.5
            case 2:
                value |= 0x8000;
                break; // 16,15,14 = 010 - sub-integer divisor = 0.25
            case 1:
                value |= 0xc000;
                break; // 16,15,14 = 011 - sub-integer divisor = 0.125
            case 3:
                value |= 0x0000;
                index |= 1;
                break; // 16,15,14 = 100 - sub-integer divisor = 0.375
            case 5:
                value |= 0x4000;
                index |= 1;
                break; // 16,15,14 = 101 - sub-integer divisor = 0.625
            case 6:
                value |= 0x8000;
                index |= 1;
                break; // 16,15,14 = 110 - sub-integer divisor = 0.75
            case 7:
                value |= 0xc000;
                index |= 1;
                break; // 16,15,14 = 111 - sub-integer divisor = 0.875
        }


        ExecuteCommand(SET_BAUD_RATE_REQUEST, value, portNumber: index);

    }
}


