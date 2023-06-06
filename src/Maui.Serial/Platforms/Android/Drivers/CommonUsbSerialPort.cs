using System.Diagnostics;
using Android.Hardware.Usb;
using Maui.Serial.Platforms.Android.Usb;
using Microsoft.Extensions.Logging;

// ReSharper disable InconsistentNaming
namespace Maui.Serial.Platforms.Android.Drivers;

public abstract class CommonUsbSerialPort : UsbSerialPort
{
    private readonly object _readBufferLock = new();
    private readonly object _writeBufferLock = new();
    private byte[] _readBuffer = Array.Empty<byte>();
    private byte[] _writeBuffer = Array.Empty<byte>();

    protected CommonUsbSerialPort(UsbManager manager, UsbDevice device, int portNumber, ILogger logger) :
        base(manager, device, portNumber, logger)
    {
    }

    protected UsbEndpoint ReadEndPoint { get; private set; }
    protected UsbEndpoint WriteEndPoint { get; private set; }

    public override void Open()
    {
        if (IsOpen)
        {
            return;
        }

        var connection = OpenConnection();
        UsbConnection = connection;
        SetParameters(connection, Parameters);
        SetInterfaces(UsbDevice);

        lock (_readBufferLock)
        {
            _readBuffer = new byte[Parameters.ReadBufferSize];
        }

        lock (_writeBufferLock)
        {
            _writeBuffer = new byte[Parameters.WriteBufferSize];
        }
    }

    public override void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        CloseConnection();
    }

    public override int Read(byte[] dest) => ReadBufferFromDevice(dest.Length);

    public override int Write(byte[] src) => WriteBufferToDevice(src.Length);

    protected void SetReadEndPoint(UsbEndpoint usbEndpoint)
    {
        lock (_readBufferLock)
        {
            ReadEndPoint = usbEndpoint;
        }
    }

    protected void SetWriteEndPoint(UsbEndpoint usbEndpoint)
    {
        lock (_readBufferLock)
        {
            WriteEndPoint = usbEndpoint;
        }
    }

    protected virtual int ReadBufferFromDevice(int len)
    {
        var result = new List<byte>();

        var toRead = len;
        while (toRead > 0)
        {
            var buffer = new byte[len];
            var read = ReadFromDevice(buffer);
            if (read > 0)
            {
                toRead -= read;
                result.AddRange(buffer);
                continue;
            }

            break;
        }

        return CopyToReadBuffer(result.ToArray(), _readBuffer);
    }

    protected virtual int WriteBufferToDevice(int len)
    {
        int offset;
        for (offset = 0; offset < len;)
        {

            var buffer = CopyFromWriteBuffer(_writeBuffer, offset, len);
            var written = WriteToDevice(buffer);
            offset += written;

        }

        return offset;
    }

    protected virtual int ReadFromDevice(byte[] buffer)
    {
        var len = Math.Min(buffer.Length, Parameters.ReadBufferSize);
        lock (_readBufferLock)
        {
            len = UsbConnection.BulkTransfer(ReadEndPoint, _readBuffer, len, Parameters.ReadTimeout);
            if (len > 0)
            {
                Buffer.BlockCopy(_readBuffer, 0, buffer, 0, len);
            }
        }

        return len;
    }

    protected virtual int WriteToDevice(byte[] buffer)
    {
        var len = Math.Min(buffer.Length, Parameters.ReadBufferSize);
        lock (_writeBufferLock)
        {
            len = UsbConnection.BulkTransfer(WriteEndPoint, buffer, len, Parameters.WriteTimeout);
            if (len > 0)
            {
                Buffer.BlockCopy(_readBuffer, 0, buffer, 0, len);
            }
        }

        return len;
    }

    protected virtual int CopyToReadBuffer(byte[] source, byte[] destination)
    {
        Array.Copy(source, destination, source.Length);
        return source.Length;
    }

    protected virtual byte[] CopyFromWriteBuffer(byte[] source, int offset, int length)
    {
        var toWrite = length - offset;
        var buffer = new byte[toWrite];
        Buffer.BlockCopy(source, offset, buffer, 0, toWrite);
        return buffer;
    }

    protected virtual void CloseConnection()
    {
        UsbConnection.Close();
        UsbConnection = null;
    }

    protected (UsbInterface Interface, int Index) FindInterface(UsbDevice device, Func<UsbInterface, bool> filter)
    {
        for (var index = 0; index < device.InterfaceCount; index++)
        {
            var usbInterface = device.GetInterface(index);
            if (!filter(usbInterface))
            {
                continue;
            }

            if (ClaimInterface(usbInterface))
            {
                return (usbInterface, index);
            }

            throw new UsbSerialRuntimeException($"Error during claim usb interface {usbInterface}");
        }

        return (null, -1);
    }

    protected UsbInterface GetInterfaceByIndex(UsbDevice device, int index)
    {
        var usbInterface = device.GetInterface(index);

        if (ClaimInterface(usbInterface))
        {
            return usbInterface;
        }

        throw new UsbSerialRuntimeException($"Error during claim usb interface {usbInterface}");
    }

    [DebuggerStepThrough]
    protected virtual bool ClaimInterface(UsbInterface usbInterface) =>
        UsbConnection.ClaimInterface(usbInterface, true);

    [DebuggerStepThrough]
    protected abstract void SetInterfaces(UsbDevice device);

    [DebuggerStepThrough]
    protected abstract void SetParameters(UsbDeviceConnection connection, SerialPortParameters parameters);

    [DebuggerStepThrough]
    protected virtual int ControlTransfer(UsbAddressing requestType, int request, int value, int index,
        byte[] buffer = null, int length = -1, int timeout = -1) =>
        UsbConnection.ControlTransfer(requestType, request, value, index, buffer, length, timeout);
}
