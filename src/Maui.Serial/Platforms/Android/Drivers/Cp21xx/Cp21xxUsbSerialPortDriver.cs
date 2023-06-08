using Android.Hardware.Usb;
using Maui.Serial.Platforms.Android.Usb;
using Microsoft.Extensions.Logging;
// ReSharper disable InconsistentNaming

namespace Maui.Serial.Platforms.Android.Drivers.Cp21xx;

public class Cp21xxUsbSerialPort : CommonUsbSerialPort
{
    private const int DEFAULT_BAUD_RATE = 9600;
    private const int USB_WRITE_TIMEOUT_MILLIS = 5000;
    private const int REQTYPE_HOST_TO_DEVICE = 0x41;
    private const int REQTYPE_DEVICE_TO_HOST = 0xc1;
    private const int SILABSER_IFC_ENABLE_REQUEST_CODE = 0x00;
    private const int SILABSER_SET_BAUDDIV_REQUEST_CODE = 0x01;
    private const int SILABSER_SET_LINE_CTL_REQUEST_CODE = 0x03;
    private const int SILABSER_SET_MHS_REQUEST_CODE = 0x07;
    private const int SILABSER_SET_BAUDRATE = 0x1E;
    private const int GET_MODEM_STATUS_REQUEST = 0x08;
    private const int MODEM_STATUS_CTS = 0x10;
    private const int MODEM_STATUS_DSR = 0x20;
    private const int MODEM_STATUS_RI = 0x40;
    private const int MODEM_STATUS_CD = 0x80;
    private const int UART_ENABLE = 0x0001;
    private const int BAUD_RATE_GEN_FREQ = 0x384000;
    private const int MCR_DTR = 0x0001;
    private const int MCR_RTS = 0x0002;
    private const int MCR_ALL = 0x0003;
    private const int CONTROL_WRITE_DTR = 0x0100;
    private const int CONTROL_WRITE_RTS = 0x0200;

    public Cp21xxUsbSerialPort(UsbManager manager, UsbDevice device, int portNumber, ILogger logger) : base(manager, device, portNumber, logger)
    {
    }

    protected override void SetInterfaces(UsbDevice device)
    {
        var dataIface = GetInterfaceByIndex(device,device.InterfaceCount - 1);

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
            }
            else
            {
                SetWriteEndPoint(ep);
            }
        }

        SetConfigSingle(SILABSER_IFC_ENABLE_REQUEST_CODE, UART_ENABLE);
        SetConfigSingle(SILABSER_SET_MHS_REQUEST_CODE, MCR_ALL | CONTROL_WRITE_DTR | CONTROL_WRITE_RTS);
        SetConfigSingle(SILABSER_SET_BAUDDIV_REQUEST_CODE, BAUD_RATE_GEN_FREQ / DEFAULT_BAUD_RATE);
    }

    protected override void SetParameters(UsbDeviceConnection connection, SerialPortParameters parameters)
    {
        SetBaudRate(parameters.BaudRate);

        var configDataBits = 0;
        configDataBits |= parameters.DataBits switch
        {
            DataBits.Bits5 => 0x0500,
            DataBits.Bits6 => 0x0600,
            DataBits.Bits7 => 0x0700,
            DataBits.Bits8 => 0x0800,
            _ => 0x0800
        };

        switch (parameters.Partity)
        {
            case Parity.Odd:
                configDataBits |= 0x0010;
                break;
            case Parity.Even:
                configDataBits |= 0x0020;
                break;
            case Parity.None:
                break;
            case Parity.Mark:
                break;
            case Parity.Space:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(parameters.Partity), parameters.Partity, null);
        }

        switch (parameters.StopBits)
        {
            case StopBits.One:
                configDataBits |= 0;
                break;
            case StopBits.Two:
                configDataBits |= 2;
                break;
            case StopBits.OnePointFive:
                break;
            case StopBits.NotSet:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(parameters.StopBits), parameters.StopBits, null);
        }
        SetConfigSingle(SILABSER_SET_LINE_CTL_REQUEST_CODE, configDataBits);
    }


    public override bool GetCD() => (GetStatus() & MODEM_STATUS_CD) != 0;
    public override bool GetCTS() => (GetStatus() & MODEM_STATUS_CTS) != 0;
    public override bool GetDSR() => (GetStatus() & MODEM_STATUS_DSR) != 0;
    public override bool GetDTR() => (GetStatus() & MCR_DTR) != 0;
    public override void SetDTR(bool value) =>
        SetConfigSingle(SILABSER_SET_MHS_REQUEST_CODE, (value ? MCR_DTR : 0) | CONTROL_WRITE_DTR);

    public override bool GetRI() => (GetStatus() & MODEM_STATUS_RI) != 0;

    public override bool GetRTS() => (GetStatus() & MCR_RTS) != 0;

    public override void SetRTS(bool value) =>
        SetConfigSingle(SILABSER_SET_MHS_REQUEST_CODE, (value ? MCR_RTS : 0) | CONTROL_WRITE_RTS);
    

    private int GetStatus()
    {
        var data = new byte[1];
        var result = UsbConnection.ControlTransfer((UsbAddressing)REQTYPE_DEVICE_TO_HOST, GET_MODEM_STATUS_REQUEST,
            0, 0, data, data.Length, USB_WRITE_TIMEOUT_MILLIS);
        if (result != 1)
        {
            throw new IOException("Get modem status failed: result=" + result);
        }

        return data[0];
    }

    private void SetBaudRate(int baudRate)
    {
        var data = new[]
        {
            (byte)(baudRate & 0xff),
            (byte)((baudRate >> 8) & 0xff),
            (byte)((baudRate >> 16) & 0xff),
            (byte)((baudRate >> 24) & 0xff)
        };
        var ret = UsbConnection.ControlTransfer((UsbAddressing)REQTYPE_HOST_TO_DEVICE, SILABSER_SET_BAUDRATE,
            0, 0, data, 4, USB_WRITE_TIMEOUT_MILLIS);
        if (ret < 0)
        {
            throw new IOException("Error setting baud rate.");
        }
    }


    private void SetConfigSingle(int request, int value) =>
        UsbConnection.ControlTransfer((UsbAddressing)REQTYPE_HOST_TO_DEVICE, request, value,
            0, null, 0, USB_WRITE_TIMEOUT_MILLIS);
}
