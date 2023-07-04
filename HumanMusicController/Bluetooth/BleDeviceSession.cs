using Microsoft.Extensions.Logging;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Bluetooth;

namespace HumanMusicController.Bluetooth
{
    public class BleDeviceSession
    {
        private readonly ILogger<BleDeviceSession> _logger;
        private List<GattCharacteristic> characteristics;
        private DeviceInformation _device;
        private int initDelay = 500;

        public BleDeviceSession(ILogger<BleDeviceSession> logger, DeviceInformation device)
        {
            _logger = logger;
            _device = device;
            characteristics = new List<GattCharacteristic>();
        }

        public GattCharacteristic GetCharacteristic(Guid characteristicsUuid)
        {
            return characteristics.Single(x => x.Uuid == characteristicsUuid);
        }

        public async Task setCharacteristicNotify(Guid serviceUuid, Guid characteristicsUuid, bool enable)
        {
            var bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(_device.Id);

            if (initDelay > 0)
                await Task.Delay(initDelay);

            var result = await bluetoothLeDevice.GetGattServicesForUuidAsync(serviceUuid);

            if (result.Status == GattCommunicationStatus.Success)
            {
                var service = result.Services[0];
                var result2 = await service.GetCharacteristicsForUuidAsync(characteristicsUuid);
                if (result2.Status == GattCommunicationStatus.Success)
                {
                    var characteristic = result2.Characteristics[0];
                    GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                            GattClientCharacteristicConfigurationDescriptorValue.Notify);
                    if (status == GattCommunicationStatus.Success)
                    {
                        this.characteristics.Add(characteristic);
                    }
                    else
                    {
                        throw new Exception($"Did not successfully communicated Notify. Status: {status}");
                    }
                }
                else
                {
                    throw new Exception($"Did not successfully get Characteristics. Status: {result2.Status}");
                }
            }
            else
            {
                throw new Exception($"No Services discovered. Status: {result.Status}");
            }
        }
    }
}