using System.Timers;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Devices.Enumeration.Pnp;
using Windows.Storage.Streams;
using Microsoft.Extensions.Logging;

namespace PolarH10
{
    public class PolarH10Bluetooth
    {
        private readonly ILogger<PolarH10Bluetooth> _logger;
        Guid BODY_SENSOR_LOCATION = new Guid("00002a38-0000-1000-8000-00805f9b34fb");
        Guid HR_MEASUREMENT = new Guid("00002a37-0000-1000-8000-00805f9b34fb");
        Guid HR_SERVICE = new Guid("0000180D-0000-1000-8000-00805f9b34fb");

        private DeviceInformation _device;
        private String deviceContainerId = "";

        private GattDeviceService? service;
        private GattCharacteristic? characteristic;
        private PnpObjectWatcher? watcher;
        private const GattClientCharacteristicConfigurationDescriptorValue CHARACTERISTIC_NOTIFICATION_TYPE = GattClientCharacteristicConfigurationDescriptorValue.Notify;
        // Heart Rate devices typically have only one Heart Rate Measurement characteristic.
        // Make sure to check your device's documentation to find out how many characteristics your specific device has.
        private int characteristicIndex = 0;

        private int initDelay = 500;

        private static TimeSpan START_TIMEOUT = new TimeSpan(0, 0, 10);
        private static TimeSpan RUN_TIMEOUT = new TimeSpan(0, 0, 30);
        private System.Timers.Timer timeoutTimer = new System.Timers.Timer(1000);
        private DateTime lastReceivedDate;

        public PolarH10Bluetooth (ILogger<PolarH10Bluetooth> logger, DeviceInformation device)
        {
            _logger = logger;
            _device = device;
            Running = false;

            heartRateSmoothing = new int?[1];

            TotalPackets = 0;
            CorruptedPackets = 0;
            HeartBeats = 0;

            timeoutTimer.Elapsed += timeoutTimer_Elapsed;
        }

        public int TotalPackets { get; protected set; }
        public int CorruptedPackets { get; protected set; }

        // Processed data
        public int HeartBeats { get; protected set; }

        public byte? MinHeartRate { get; protected set; }
        public byte? MaxHeartRate { get; protected set; }

        private int?[] heartRateSmoothing;
        public double SmoothedHeartRate { get; protected set; }

        public bool Running { get; protected set; }

        public async void Start()
        {
                

            if (Running)
                return;

#if LogDebug
            _logger.LogDebug("Starting HRP");
#endif

            lastReceivedDate = DateTime.Now;

            timeoutTimer.Start();
            Running = true;

            await ConfigureServiceForNotificationsAsync();
        }

        private async Task ConfigureServiceForNotificationsAsync()
        {          
            try
            {
                _logger.LogDebug($"Getting GattDeviceService {_device.Name} with id {_device.Id}");
                service = await GattDeviceService.FromIdAsync(_device.Id);
                if (initDelay > 0)
                    await Task.Delay(initDelay);

                if (service != null)
                {
                    _logger.LogDebug($"GattDeviceService instatiated successfully");

                    _logger.LogDebug($"GattSession status = {service.Session.SessionStatus}, " +
                    $"mantain connection = {service.Session.MaintainConnection}, " +
                    $"can mantain connection = {service.Session.MaintainConnection}");
                }
                else
                {
                    _logger.LogDebug($"Failed to instantiate GattDeviceService");
                }

                // List all the characteristics of the device
                _logger.LogDebug("Getting all GattCharacteristic...");
                GattCharacteristicsResult allResult = await service?.GetCharacteristicsAsync();
                _logger.LogDebug($"GattCharacteristicsResult status {allResult.Status}");
                foreach (GattCharacteristic allCharacteristic in allResult.Characteristics)
                {
                    _logger.LogDebug($"GattCharacteristic {allCharacteristic.Uuid}: " +
                        $"description = {allCharacteristic.UserDescription}, " +
                        $"protection level = {allCharacteristic.ProtectionLevel}");
                }

                // Obtain the characteristic for which notifications are to be received
                _logger.LogDebug($"Getting HeartRateMeasurement GattCharacteristic {characteristicIndex}");
                GattCharacteristicsResult result = await service?.GetCharacteristicsForUuidAsync(GattCharacteristicUuids.HeartRateMeasurement);

                _logger.LogDebug($"GattCharacteristicsResult status {result.Status}");
                foreach (GattCharacteristic hrCharacteristic in result.Characteristics)
                {
                    _logger.LogDebug($"GattCharacteristic {hrCharacteristic.Uuid}: " +
                        $"description = {hrCharacteristic.UserDescription}, " +
                        $"protection level = {hrCharacteristic.ProtectionLevel}");
                }

                characteristic = result.Characteristics[characteristicIndex];

                // While encryption is not required by all devices, if encryption is supported by the device,
                // it can be enabled by setting the ProtectionLevel property of the Characteristic object.
                // All subsequent operations on the characteristic will work over an encrypted link.
                _logger.LogDebug("Setting EncryptionRequired protection level on GattCharacteristic");
                characteristic.ProtectionLevel = GattProtectionLevel.EncryptionRequired;

                // Register the event handler for receiving notifications
                if (initDelay > 0)
                    await Task.Delay(initDelay);
                _logger.LogDebug("Registering event handler onction level on GattCharacteristic");
                characteristic.ValueChanged += Characteristic_ValueChanged;

                // Set the Client Characteristic Configuration Descriptor to enable the device to send notifications
                // when the Characteristic value changes
                _logger.LogDebug("Setting GattCharacteristic configuration descriptor to enable notifications");

                GattCommunicationStatus status =
                    await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                    CHARACTERISTIC_NOTIFICATION_TYPE);

                if (status == GattCommunicationStatus.Unreachable)
                {
                    _logger.LogDebug("Device unreachable");
                    // Register a PnpObjectWatcher to detect when a connection to the device is established,
                    // such that the application can retry device configuration.
                    StartDeviceConnectionWatcher();
                }
                else if (status == GattCommunicationStatus.Success)
                {
                    _logger.LogDebug("Configuration successfull");

                    _logger.LogDebug($"GattSession status = {service?.Session.SessionStatus}, " +
                    $"mantain connection = {service?.Session.MaintainConnection}, " +
                    $"can mantain connection = {service?.Session.MaintainConnection}");
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning("Error configuring HRP device", e);
                _logger.LogWarning($"Exception: {e}");
                _logger.LogWarning($"Exception Message: {e.Message}");

                Stop();
                _logger.LogWarning("Bluetooth HRP device initialization failed");
            }
        }

        private void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            _logger.LogDebug($"GattCharacteristic value changed, args = {args}");
            byte[] data = new byte[args.CharacteristicValue.Length];

            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(data);

            ProcessData(data, args.Timestamp);
        }

        private void ProcessData(byte[] data, DateTimeOffset timestamp)
        {
            var seperationCharacter = ",";
            _logger.LogDebug($"Processing HRP payload, data = {string.Join(seperationCharacter, data)}");

            NewMethodology(data);

            ProcessPackets();
        }

        private void NewMethodology(byte[] data)
        {
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


            // The Heart Rate Bluetooth profile can also contain sensor contact status information,
            // and R-Wave interval measurements, which can also be processed here. 
            // For the purpose of this sample, we don't need to interpret that data.
            string seperationCharacter = ",";
            var test = ($"hrFormat: {hrFormat}, sensorContact: {sensorContact}, contactSupported: {contactSupported}, hasEnergyExpended: {hasEnergyExpended}, rrPresent: {rrPresent}, rrs: {string.Join(seperationCharacter, rrs)}, rrsMs: {string.Join(seperationCharacter, rrsMs)}");
            _logger.LogDebug($"Data = {test}");
        }

        private int mapRr1024ToRrMs (int rrsRaw)
        {
            var rrsRawMapped = ((float)rrsRaw / 1024.0 * 1000.0);
            return (int)Math.Round(rrsRawMapped, 0);
        }

        private void ProcessPackets()
        {
            _logger.LogDebug("Processing HRP packets");

            // Smoothed values computation
//             if (heartRateSmoothing[0] == null)
//             {
//                 for (int i = 0; i < heartRateSmoothing.Length; i++)
//                 {
//                     heartRateSmoothing[i] = lastPacket.HeartRate;
//                 }

//                 SmoothedHeartRate = lastPacket.HeartRate;
//             }
//             else
//             {
//                 int?[] shiftedArray = new int?[heartRateSmoothing.Length];
//                 Array.Copy(heartRateSmoothing, 0, shiftedArray, 1, heartRateSmoothing.Length - 1);
//                 heartRateSmoothing = shiftedArray;
//                 heartRateSmoothing[0] = lastPacket.HeartRate;

//                 double d = 0;
//                 for (int i = 0; i < heartRateSmoothing.Length; i++)
//                 {
//                     d += heartRateSmoothing[i] ?? 0;
//                 }
//                 SmoothedHeartRate = d / heartRateSmoothing.Length;
//             }

//             // Computation across multiple packets
//             if (MinHeartRate == null && lastPacket.HeartRate > 30)
//                 MinHeartRate = (byte)lastPacket.HeartRate;
//             else if (lastPacket.HeartRate < MinHeartRate && lastPacket.HeartRate > 30)
//                 MinHeartRate = (byte)lastPacket.HeartRate;

//             if (MaxHeartRate == null && lastPacket.HeartRate < 240)
//                 MaxHeartRate = (byte)lastPacket.HeartRate;
//             else if (lastPacket.HeartRate > MaxHeartRate && lastPacket.HeartRate < 240)
//                 MaxHeartRate = (byte)lastPacket.HeartRate;

//             if (secondLastPacket == null)
//             {
//                 HeartBeats = 1;
//                 return;
//             }

//             ++HeartBeats;

//             lastReceivedDate = DateTime.Now;

// #if LogDebug
//             _logger.LogDebug($"Firing PacketProcessed event, packet = {LastPacket}");
// #endif
//             PacketProcessedEventArgs args = new PacketProcessedEventArgs(LastPacket);
        }

        private void StartDeviceConnectionWatcher()
        {
            watcher = PnpObject.CreateWatcher(PnpObjectType.DeviceContainer,
                new string[] { "System.Devices.Connected" }, String.Empty);

            _logger.LogDebug("Registering device connection watcher updated event handler");
            watcher.Updated += DeviceConnection_Updated;

            _logger.LogDebug("Starting device connection watcher");
            watcher.Start();
        }

        private async void DeviceConnection_Updated(PnpObjectWatcher sender, PnpObjectUpdate args)
        {
            _logger.LogDebug($"Device connection updated, args = {args}");

            var connectedProperty = args.Properties["System.Devices.Connected"];

            _logger.LogDebug($"Connected property, args = {connectedProperty}");

            bool isConnected = false;
            if ((deviceContainerId == args.Id) && Boolean.TryParse(connectedProperty.ToString(), out isConnected) &&
                isConnected)
            {
                var status = await characteristic?.WriteClientCharacteristicConfigurationDescriptorAsync(
                    CHARACTERISTIC_NOTIFICATION_TYPE);

                if (status == GattCommunicationStatus.Success)
                {
                    _logger.LogDebug("Stopping device connection watcher");

                    // Once the Client Characteristic Configuration Descriptor is set, the watcher is no longer required
                    watcher?.Stop();
                    watcher = null;
                    _logger.LogDebug("Configuration successfull");
                }
            }
        }

        void timeoutTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            TimeSpan timeout;
            if (Running)
                timeout = RUN_TIMEOUT;
            else
                timeout = START_TIMEOUT;

            TimeSpan diff = DateTime.Now - lastReceivedDate;
            if (diff > timeout)
            {
                if (Running)
                    _logger.LogDebug("Communication timeout elapsed");
                else
                    _logger.LogDebug("Start timeout elapsed");

                Stop();
                // FireTimeout("Bluetooth HRP device not transmitting");
            }
        }

        public void Stop()
        {
            if (Running)
            {
                _logger.LogDebug("Stopping HRP");

                Running = false;

                _logger.LogDebug("Stopping timeout timer");
                timeoutTimer.Stop();

                if (characteristic != null)
                {
                    _logger.LogDebug("Clearing GattCharacteristic");
                    characteristic.ValueChanged -= Characteristic_ValueChanged;
                    characteristic = null;
                }

                if (watcher != null)
                {
                    _logger.LogDebug("Clearing device changed watcher");
                    watcher.Stop();
                    watcher = null;
                }

                if (service != null)
                {
                    _logger.LogDebug("Clearing GattDeviceService");
                    service.Dispose();
                    service = null;
                }

                _logger.LogDebug("Resetting counters");
                DoReset();
            }
        }

        private void DoReset()
        {
            TotalPackets = 0;
            CorruptedPackets = 0;
            HeartBeats = 0;
            MinHeartRate = null;
            MaxHeartRate = null;

            for (int i = 0; i < heartRateSmoothing.Length; i++)
            {
                heartRateSmoothing[i] = null;
            }
            SmoothedHeartRate = 0;
        }

        public void Reset()
        {
            _logger.LogDebug("Resetting HRP");

            Stop();
            Start();
        }

        public void Dispose()
        {
            if (characteristic != null)
            {
                characteristic.ValueChanged -= Characteristic_ValueChanged;
                characteristic = null;
            }

            if (watcher != null)
            {
                watcher.Stop();
                watcher = null;
            }

            if (service != null)
            {
                service.Dispose();
                service = null;
            }
        }
    }
}
