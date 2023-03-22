using System.Diagnostics;
using Android.Hardware.Usb;
using Java.Lang;
using Microsoft.Extensions.Logging;
using IOException = Java.IO.IOException;
// ReSharper disable InconsistentNaming

namespace Maui.Serial.Platforms.Android.Drivers.CdcAcm;

public class CdcAcmUsbSerialPort : CommonUsbSerialPort
{
    #region Consts
    private const int SET_LINE_CODING = 0x20;
    private const int SET_CONTROL_LINE_STATE = 0x22;
    #endregion

    #region Fields
    private UsbInterface _controlInterface;
    private UsbInterface _dataInterface;
    private UsbEndpoint _controlEndpoint;

    private bool _rts;
    private bool _dtr;
    #endregion

    public CdcAcmUsbSerialPort(UsbManager manager, UsbDevice device, int portNumber, ILogger logger) : base(manager, device, portNumber, logger)
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
        if (device.InterfaceCount == 1)
        {
            OpenSingleInterface(device);
        }
        else
        {
            OpenInterface(device);
        }
    }

    protected override void SetParameters(UsbDeviceConnection connection, SerialPortParameters parameters)
    {
        byte stopBitsByte = parameters.StopBits switch
        {
            StopBits.One => 0,
            StopBits.OnePointFive => 1,
            StopBits.Two => 2,
            _ => throw new IllegalArgumentException($"Bad value for stopBits: {parameters.StopBits}")
        };

        byte parityBitesByte = parameters.Partity switch
        {
            Parity.None => 0,
            Parity.Odd => 1,
            Parity.Even => 2,
            Parity.Mark => 3,
            Parity.Space => 4,
            _ => throw new IllegalArgumentException($"Bad value for parity: {parameters.Partity}")
        };

        var baudRate = parameters.BaudRate;
        byte[] msg =
        {
            (byte)(baudRate & 0xff),
            (byte)((baudRate >> 8) & 0xff),
            (byte)((baudRate >> 16) & 0xff),
            (byte)((baudRate >> 24) & 0xff),
            stopBitsByte,
            parityBitesByte,
            (byte)parameters.DataBits
        };
        SendAcmControlMessage(SET_LINE_CODING, 0, msg);
    }

    private void SetDtrRts()
    {
        var value = (_rts ? 0x2 : 0) | (_dtr ? 0x1 : 0);
        SendAcmControlMessage(SET_CONTROL_LINE_STATE, value, null);
    }

    [DebuggerStepThrough]
    private void SendAcmControlMessage(int request, int value, byte[] buf) => UsbConnection.ControlTransfer((UsbAddressing)0x21, request, value, 0, buf, buf?.Length ?? 0, 5000);

    // ReSharper disable once CyclomaticComplexity
    private void OpenSingleInterface(UsbDevice device)
    {
        _controlInterface = GetInterfaceByIndex(device, 0);
        Logger?.LogDebug("Control iface={interface}", _controlInterface);

        _dataInterface = GetInterfaceByIndex(device, 0);
        Logger?.LogDebug("Control iface={interface}", _dataInterface);


        var endCount = _controlInterface.EndpointCount;
        if (endCount < 3)
        {
            Logger?.LogError("Not enough endpoints - need 3. count={endCount}", endCount);
            throw new IOException($"Insufficient number of endpoints({endCount})");
        }

        _controlEndpoint = null;
        for (var i = 0; i < endCount; ++i)
        {
            var ep = _controlInterface.GetEndpoint(i);
            switch (ep?.Direction)
            {
                case UsbAddressing.In when
                    (ep.Type == UsbAddressing.XferInterrupt):
                    Logger?.LogDebug("Found controlling endpoint");
                    _controlEndpoint = ep;
                    break;
                case UsbAddressing.In when
                    (ep.Type == UsbAddressing.XferBulk):
                    Logger?.LogDebug("Found reading endpoint");
                    SetReadEndPoint(ep);
                    break;
                case UsbAddressing.Out when
                    ep.Type == UsbAddressing.XferBulk:
                    Logger?.LogDebug("Found writing endpoint");
                    SetWriteEndPoint(ep);
                    break;
                case UsbAddressing.NumberMask:
                    break;
                case UsbAddressing.XferBulk:
                    break;
                case UsbAddressing.XferInterrupt:
                    break;
                case UsbAddressing.XferIsochronous:
                    break;
                case null:
                    break;
                default:
                    throw new InvalidDataException(ep.ToString());
            }


            if (_controlEndpoint is null ||
                ReadEndPoint is null ||
                WriteEndPoint is null)
            {
                continue;
            }

            Logger?.LogDebug("Found all required endpoints");
            return;
        }


        Logger?.LogError("Could not establish all endpoints");
        throw new IOException("Could not establish all endpoints");
    }

    private void OpenInterface(UsbDevice device)
    {
        Logger?.LogDebug("Claiming interfaces, count={count}", device.InterfaceCount);

        _controlInterface = GetInterfaceByIndex(device, 0);
        Logger?.LogDebug("Control iface={iface}", _controlInterface);
        _controlEndpoint = _controlInterface.GetEndpoint(0);
        Logger?.LogDebug("Control endpoint direction: {direction}", _controlEndpoint?.Direction);
        _dataInterface = GetInterfaceByIndex(device, 1);
        Logger?.LogDebug("Data iface={iface}", _dataInterface);
        SetReadEndPoint(_dataInterface.GetEndpoint(1));
        Logger?.LogDebug("Read endpoint direction: {direction}", ReadEndPoint?.Direction);
        SetWriteEndPoint(_dataInterface.GetEndpoint(0));
        Logger?.LogDebug("Write endpoint direction: {direction}", WriteEndPoint?.Direction);
    }
}