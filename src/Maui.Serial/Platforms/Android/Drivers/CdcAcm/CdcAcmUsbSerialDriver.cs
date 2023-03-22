using System.Diagnostics;
using Android.Hardware.Usb;
using Maui.Serial.Platforms.Android.Usb;
using Microsoft.Extensions.Logging;

namespace Maui.Serial.Platforms.Android.Drivers.CdcAcm;

[UsbDeviceDriver(UsbId.VENDOR_ARDUINO,
    new[]
    {
        UsbId.ARDUINO_UNO, UsbId.ARDUINO_UNO_R3, UsbId.ARDUINO_MEGA_2560, UsbId.ARDUINO_MEGA_2560_R3,
        UsbId.ARDUINO_SERIAL_ADAPTER, UsbId.ARDUINO_SERIAL_ADAPTER_R3, UsbId.ARDUINO_MEGA_ADK, UsbId.ARDUINO_LEONARDO,
        UsbId.ARDUINO_MICRO,
    })]
[UsbDeviceDriver(UsbId.VENDOR_VAN_OOIJEN_TECH, new[] { UsbId.VAN_OOIJEN_TECH_TEENSYDUINO_SERIAL })]
[UsbDeviceDriver(UsbId.VENDOR_ATMEL, new[] { UsbId.ATMEL_LUFA_CDC_DEMO_APP })]
[UsbDeviceDriver(UsbId.VENDOR_ELATEC,
    new[] { UsbId.ELATEC_TWN3_CDC, UsbId.ELATEC_TWN4_MIFARE_NFC, UsbId.ELATEC_TWN4_CDC, })]
[UsbDeviceDriver(UsbId.VENDOR_LEAFLABS, new[] { UsbId.LEAFLABS_MAPLE })]
public class CdcmAcmUsbSerialDriver : CommonUsbSerialDriver
{
    public CdcmAcmUsbSerialDriver(UsbManager manager, UsbDevice device, ILogger logger) :
        base(manager, device, logger)
    {
    }

    [DebuggerStepThrough]
    protected override UsbSerialPort GetPort(UsbManager manager, UsbDevice device, int port, ILogger logger) =>
        new CdcAcmUsbSerialPort(manager, device, port, logger);
}

