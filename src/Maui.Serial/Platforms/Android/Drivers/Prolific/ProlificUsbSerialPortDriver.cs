using System.Diagnostics;
using Android.Hardware.Usb;
using Java.Lang;
using Maui.Serial.Platforms.Android.Usb;
using Microsoft.Extensions.Logging;
// ReSharper disable InconsistentNaming

namespace Maui.Serial.Platforms.Android.Drivers.Prolific;

[UsbDeviceDriver(UsbId.VENDOR_PROLIFIC,
    new[]
    {
        UsbId.PROLIFIC_PL2303, UsbId.PROLIFIC_PL2303GC, UsbId.PROLIFIC_PL2303GB, UsbId.PROLIFIC_PL2303GT,
        UsbId.PROLIFIC_PL2303GL, UsbId.PROLIFIC_PL2303GE, UsbId.PROLIFIC_PL2303GS
    })]
public class ProlificUsbSerialPortDriver : CommonUsbSerialPort
{
    private enum DeviceType
    {
        DEVICE_TYPE_01,
        DEVICE_TYPE_T,
        DEVICE_TYPE_HX,
        DEVICE_TYPE_HXN
    }

    private const int USB_READ_TIMEOUT_MILLIS = 1000;
    private const int USB_WRITE_TIMEOUT_MILLIS = 5000;

    private const int USB_RECIP_INTERFACE = 0x01;

    private const int VENDOR_READ_REQUEST = 0x01;
    private const int VENDOR_WRITE_REQUEST = 0x01;
    private const int VENDOR_READ_HXN_REQUEST = 0x81;
    private const int VENDOR_WRITE_HXN_REQUEST = 0x80;

    private const int VENDOR_OUT_REQTYPE = UsbSupport.UsbDirOut | UsbConstants.UsbTypeVendor;
    private const int VENDOR_IN_REQTYPE = UsbSupport.UsbDirIn | UsbConstants.UsbTypeVendor;
    private const int CTRL_OUT_REQTYPE = UsbSupport.UsbDirOut | UsbConstants.UsbTypeClass | USB_RECIP_INTERFACE;

    private const int WRITE_ENDPOINT = 0x02;
    private const int READ_ENDPOINT = 0x83;
    private const int INTERRUPT_ENDPOINT = 0x81;

    private const int RESET_HXN_REQUEST = 0x07;
    private const int FLUSH_RX_REQUEST = 0x08;
    private const int FLUSH_TX_REQUEST = 0x09;

    private const int SET_LINE_REQUEST = 0x20;
    private const int SET_CONTROL_REQUEST = 0x22;
    private const int GET_CONTROL_HXN_REQUEST = 0x80;
    private const int GET_CONTROL_REQUEST = 0x87;

    private const int RESET_HXN_RX_PIPE = 1;
    private const int RESET_HXN_TX_PIPE = 2;

    private const int CONTROL_DTR = 0x01;
    private const int CONTROL_RTS = 0x02;

    private const int GET_CONTROL_FLAG_CD = 0x02;
    private const int GET_CONTROL_FLAG_DSR = 0x04;
    private const int GET_CONTROL_FLAG_RI = 0x01;
    private const int GET_CONTROL_FLAG_CTS = 0x08;

    private const int GET_CONTROL_HXN_FLAG_CD = 0x40;
    private const int GET_CONTROL_HXN_FLAG_DSR = 0x20;
    private const int GET_CONTROL_HXN_FLAG_RI = 0x80;
    private const int GET_CONTROL_HXN_FLAG_CTS = 0x08;

    private const int STATUS_FLAG_CD = 0x01;
    private const int STATUS_FLAG_DSR = 0x02;
    private const int STATUS_FLAG_RI = 0x08;
    private const int STATUS_FLAG_CTS = 0x80;

    private const int STATUS_BUFFER_SIZE = 10;
    private const int STATUS_BYTE_IDX = 8;

    private DeviceType _deviceType = DeviceType.DEVICE_TYPE_HX;
    private UsbEndpoint _interruptEndpoint;
    private int _controlLinesValue;

    public ProlificUsbSerialPortDriver(UsbManager manager, UsbDevice device, int portNumber, ILogger logger)
        : base(manager, device, portNumber, logger)
    {
    }


    // ReSharper disable once CyclomaticComplexity
    protected override void SetInterfaces(UsbDevice device)
    {
        var usbInterface = GetInterfaceByIndex(device, 0);


        for (var i = 0; i < usbInterface.EndpointCount; ++i)
        {
            var currentEndpoint = usbInterface.GetEndpoint(i);

            switch (currentEndpoint?.Address)
            {
                case (UsbAddressing)READ_ENDPOINT:
                    SetReadEndPoint(currentEndpoint);
                    break;

                case (UsbAddressing)WRITE_ENDPOINT:
                    SetWriteEndPoint(currentEndpoint);
                    break;

                case (UsbAddressing)INTERRUPT_ENDPOINT:
                    _interruptEndpoint = currentEndpoint;
                    break;
                case UsbAddressing.In:
                    break;
                case UsbAddressing.Out:
                    break;
                case UsbAddressing.NumberMask:
                    break;
                case UsbAddressing.XferInterrupt:
                    break;
                case UsbAddressing.XferIsochronous:
                    break;
                case null:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(currentEndpoint?.Address.ToString());
            }
        }

        var rawDescriptors = UsbConnection.GetRawDescriptors();
        if (rawDescriptors == null || rawDescriptors.Length < 14)
        {
            throw new IOException("Could not get device descriptors");
        }

        var usbVersion = (rawDescriptors[3] << 8) + rawDescriptors[2];
        var deviceVersion = (rawDescriptors[13] << 8) + rawDescriptors[12];
        var maxPacketSize0 = rawDescriptors[7];

        if (UsbDevice.DeviceClass == UsbClass.Comm || maxPacketSize0 != 64)
        {
            _deviceType = DeviceType.DEVICE_TYPE_01;
        }
        else
            switch (deviceVersion)
            {
                case 0x300 when usbVersion == 0x200:
                case 0x500:
                    _deviceType = DeviceType.DEVICE_TYPE_T;
                    break;
                default:
                    if (usbVersion == 0x200 && !TestHxStatus())
                    {
                        _deviceType = DeviceType.DEVICE_TYPE_HXN;
                    }
                    else
                    {
                        _deviceType = DeviceType.DEVICE_TYPE_HX;
                    }

                    break;
            }

        SetControlLines(_controlLinesValue);
        Reset();
        DoBlackMagic();

    }

    protected override void SetParameters(UsbDeviceConnection connection, SerialPortParameters parameters)
    {
        var baudRate = parameters.BaudRate;
        var lineRequestData = new byte[]
        {
            (byte)(baudRate & 0xff),
            (byte)((baudRate >> 8) & 0xff),
            (byte)((baudRate >> 16) & 0xff),
            (byte)((baudRate >> 24) & 0xff),
            parameters.StopBits switch
            {
                StopBits.NotSet => throw new IllegalArgumentException($"Unknown stopBits value: {parameters.StopBits}"),
                StopBits.One => 0,
                StopBits.OnePointFive => 1,
                StopBits.Two => 2,
                _ => throw new IllegalArgumentException($"Unknown stopBits value: {parameters.StopBits}")
            },
            parameters.Partity switch
            {
                Parity.None => 0,
                Parity.Odd => 1,
                Parity.Even => 2,
                Parity.Mark => 3,
                Parity.Space => 4,
                _ => throw new IllegalArgumentException($"Unknown parity value: {parameters.Partity}")
            },
            (byte)parameters.DataBits
        };

        CtrlOut(SET_LINE_REQUEST, 0, 0, lineRequestData);
        Reset();
    }

    public override bool GetCD() => TestStatusFlag(STATUS_FLAG_CD);
    public override bool GetCTS() => TestStatusFlag(STATUS_FLAG_CTS);
    public override bool GetDSR() => TestStatusFlag(STATUS_FLAG_DSR);
    public override bool GetDTR() => (_controlLinesValue & CONTROL_DTR) == CONTROL_DTR;

    public override void SetDTR(bool value)
    {
        int newControlLinesValue;
        if (value)
        {
            newControlLinesValue = _controlLinesValue | CONTROL_DTR;
        }
        else
        {
            newControlLinesValue = _controlLinesValue & ~CONTROL_DTR;
        }

        SetControlLines(newControlLinesValue);
    }


    public override bool GetRI() => TestStatusFlag(STATUS_FLAG_RI);

    public override bool GetRTS() => (_controlLinesValue & CONTROL_RTS) == CONTROL_RTS;

    public override void SetRTS(bool value)
    {
        int newControlLinesValue;
        if (value)
        {
            newControlLinesValue = _controlLinesValue | CONTROL_RTS;
        }
        else
        {
            newControlLinesValue = _controlLinesValue & ~CONTROL_RTS;
        }

        SetControlLines(newControlLinesValue);
    }

    private byte[] InControlTransfer(int requestType, int request,
        int value, int index, int length)
    {
        var buffer = new byte[length];
        var result = UsbConnection.ControlTransfer((UsbAddressing)requestType, request, value,
            index, buffer, length, USB_READ_TIMEOUT_MILLIS);
        if (result != length)
        {
            throw new IOException($"ControlTransfer with value {value} failed: {result}");
        }

        return buffer;
    }

    [DebuggerStepThrough]
    private void CtrlOut(int request, int value, int index, byte[] data) =>
        OutControlTransfer(CTRL_OUT_REQTYPE, request, value, index, data);

    private bool TestHxStatus()
    {
        try
        {
            InControlTransfer(VENDOR_IN_REQTYPE, VENDOR_READ_REQUEST, 0x8080, 0, 1);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private void DoBlackMagic()
    {
        if (_deviceType == DeviceType.DEVICE_TYPE_HXN)
        {
            return;
        }

        VendorIn(0x8484, 0, 1);
        VendorOut(0x0404, 0, null);
        VendorIn(0x8484, 0, 1);
        VendorIn(0x8383, 0, 1);
        VendorIn(0x8484, 0, 1);
        VendorOut(0x0404, 1, null);
        VendorIn(0x8484, 0, 1);
        VendorIn(0x8383, 0, 1);
        VendorOut(0, 1, null);
        VendorOut(1, 0, null);
        VendorOut(2, (_deviceType == DeviceType.DEVICE_TYPE_HX) ? 0x44 : 0x24, null);
    }

    private void SetControlLines(int newControlLinesValue)
    {
        CtrlOut(SET_CONTROL_REQUEST, newControlLinesValue, 0, null);
        _controlLinesValue = newControlLinesValue;
    }

    private int GetStatus()
    {
        var buffer = new byte[STATUS_BUFFER_SIZE];
        var readBytesCount = UsbConnection.BulkTransfer(_interruptEndpoint, buffer, STATUS_BUFFER_SIZE, 500);
        var status = 0;

        switch (readBytesCount)
        {
            case <= 0:
                break;
            case STATUS_BUFFER_SIZE:
                status = buffer[STATUS_BYTE_IDX] & 0xff;
                break;
            default:
                throw new IOException(
                    $"Invalid CTS / DSR / CD / RI status buffer received, expected {STATUS_BUFFER_SIZE} bytes, but received {readBytesCount}");
        }

        if (_deviceType == DeviceType.DEVICE_TYPE_HXN)
        {
            var data = VendorIn(GET_CONTROL_HXN_REQUEST, 0, 1);
            if ((data[0] & GET_CONTROL_HXN_FLAG_CTS) == 0) status |= STATUS_FLAG_CTS;
            if ((data[0] & GET_CONTROL_HXN_FLAG_DSR) == 0) status |= STATUS_FLAG_DSR;
            if ((data[0] & GET_CONTROL_HXN_FLAG_CD) == 0) status |= STATUS_FLAG_CD;
            if ((data[0] & GET_CONTROL_HXN_FLAG_RI) == 0) status |= STATUS_FLAG_RI;
        }
        else
        {
            var data = VendorIn(GET_CONTROL_REQUEST, 0, 1);
            if ((data[0] & GET_CONTROL_FLAG_CTS) == 0) status |= STATUS_FLAG_CTS;
            if ((data[0] & GET_CONTROL_FLAG_DSR) == 0) status |= STATUS_FLAG_DSR;
            if ((data[0] & GET_CONTROL_FLAG_CD) == 0) status |= STATUS_FLAG_CD;
            if ((data[0] & GET_CONTROL_FLAG_RI) == 0) status |= STATUS_FLAG_RI;
        }

        return status;
    }

    private void Reset()
    {
        if (_deviceType == DeviceType.DEVICE_TYPE_HXN)
        {
            const int index = RESET_HXN_RX_PIPE | RESET_HXN_TX_PIPE;
            VendorOut(RESET_HXN_REQUEST, index, null);
        }
        else
        {

            VendorOut(FLUSH_RX_REQUEST, 0, null);
            VendorOut(FLUSH_TX_REQUEST, 0, null);
        }
    }

    private void OutControlTransfer(int requestType, int request,
        int value, int index, byte[] data)
    {
        var length = data?.Length ?? 0;
        var result = UsbConnection.ControlTransfer((UsbAddressing)requestType, request, value,
            index, data, length, USB_WRITE_TIMEOUT_MILLIS);
        if (result != length)
        {
            throw new IOException($"ControlTransfer with value {value} failed: {result}");
        }
    }

    [DebuggerStepThrough]
    private byte[] VendorIn(int value, int index, int length)
    {
        var request = (_deviceType == DeviceType.DEVICE_TYPE_HXN) ? VENDOR_READ_HXN_REQUEST : VENDOR_READ_REQUEST;
        return InControlTransfer(VENDOR_IN_REQTYPE, request, value, index, length);
    }

    [DebuggerStepThrough]
    private void VendorOut(int value, int index, byte[] data)
    {
        var request = (_deviceType == DeviceType.DEVICE_TYPE_HXN) ? VENDOR_WRITE_HXN_REQUEST : VENDOR_WRITE_REQUEST;
        OutControlTransfer(VENDOR_OUT_REQTYPE, request, value, index, data);
    }

    [DebuggerStepThrough]
    private bool TestStatusFlag(int flag) => (GetStatus() & flag) == flag;
}