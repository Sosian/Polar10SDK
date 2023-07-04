using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
using Microsoft.Extensions.Logging;
using HumanMusicController.Connectors;

namespace HumanMusicController.Bluetooth
{
    public class PolarH10Bluetooth : BleGattBase
    {
        private readonly ILogger<PolarH10Bluetooth> logger;
        Guid HR_MEASUREMENT_CHARACTERISTIC = new Guid("00002a37-0000-1000-8000-00805f9b34fb");
        Guid HR_SERVICE = new Guid("0000180D-0000-1000-8000-00805f9b34fb");

        private readonly BleDeviceSession deviceSession;
        private readonly IConnector connector;

        public PolarH10Bluetooth(ILogger<PolarH10Bluetooth> logger, BleDeviceSession deviceSession, IConnector connector)
        {
            this.connector = connector;
            this.logger = logger;
            this.deviceSession = deviceSession;
        }

        public async void Start()
        {
            await ConfigureServiceForNotificationsAsync();
        }

        private async Task ConfigureServiceForNotificationsAsync()
        {
            try
            {
                await deviceSession.setCharacteristicNotify(HR_SERVICE, HR_MEASUREMENT_CHARACTERISTIC, true);
                var characteristic = deviceSession.GetCharacteristic(HR_MEASUREMENT_CHARACTERISTIC);
                characteristic.ValueChanged += Characteristic_ValueChanged;
            }
            catch (Exception e)
            {
                logger.LogWarning("Error configuring HRP device", e);
                logger.LogWarning($"Exception: {e}");
                logger.LogWarning($"Exception Message: {e.Message}");
                logger.LogWarning("Bluetooth HRP device initialization failed");
            }
        }

        private void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            logger.LogDebug($"GattCharacteristic value changed, args = {args}");
            byte[] data = new byte[args.CharacteristicValue.Length];

            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(data);

            ProcessData(data, args.Timestamp);
        }

        private void ProcessData(byte[] data, DateTimeOffset timestamp)
        {
            var seperationCharacter = ",";
            logger.LogDebug($"Processing HRP payload, data = {string.Join(seperationCharacter, data)}");

            var hrFormat = (int)data[0] & 0x01;
            var sensorContact = ((int)data[0] & 0x06 >> 1) == 0x03;
            var contactSupported = ((int)data[0] & 0x04) != 0;
            var hasEnergyExpended = ((int)data[0] & 0x08 >> 3) != 0;
            var rrPresent = (int)data[0] & 0x10 >> 4;
            int heartRateMeasurementValue = (int)((hrFormat == 1) ? ((int)data[1] & 0xFF) + ((int)data[2] << 8) : data[1]) & ((hrFormat == 1) ? 0x0000FFFF : 0x000000FF);
            var offset = hrFormat + 2;
            var energy = 0;
            if (hasEnergyExpended)
            {
                energy = ((int)data[offset] & 0xFF) + ((int)data[offset + 1] & 0xFF << 8);
                offset += 2;
            }
            var rrs = new List<int>();
            var rrsMs = new List<int>();
            if (rrPresent == 1)
            {
                var len = data.Length;
                while (offset < len)
                {
                    var rrValue = ((int)data[offset] & 0xFF) + ((int)data[offset + 1] & 0xFF << 8);
                    offset += 2;
                    rrs.Add(rrValue);
                    rrsMs.Add(mapRr1024ToRrMs(rrValue));
                }
            }

            var finalExpendedEnergyValue = energy;
            var test = ($"heartRateMeasurementValue: {heartRateMeasurementValue}");
            logger.LogDebug($"Data = {test}");

            connector.ReceiveData(new HrPayload(heartRateMeasurementValue));
        }

        private int mapRr1024ToRrMs(int rrsRaw)
        {
            var rrsRawMapped = ((float)rrsRaw / 1024.0 * 1000.0);
            return (int)Math.Round(rrsRawMapped, 0);
        }
    }
}