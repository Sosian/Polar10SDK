using Microsoft.Extensions.Logging;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace HumanMusicController.Bluetooth
{
    public class BluetoothDeviceFinder
    {
        private readonly ILogger<BluetoothDeviceFinder> _logger;

        public BluetoothDeviceFinder(ILogger<BluetoothDeviceFinder> logger)
        {
            _logger = logger;
        }

        public DeviceInformation GetDevice()
        {
            var foundDevices = GetDevicesInternal();

            _logger.LogDebug($"Found {foundDevices.Count} devices, picking the first");
            foreach (DeviceInformation device in foundDevices.ToList())
            {
                _logger.LogDebug($"{device.Name}: " +
                    $"id = {device.Id}, " +
                    $"default = {device.IsDefault}, " +
                    $"enabled = {device.IsEnabled},  " +
                    $"paired = {device.Pairing.IsPaired}");
            }

            return foundDevices[0];
        }

        private DeviceInformationCollection GetDevicesInternal()
        {
            var task = DeviceInformation.FindAllAsync(
                GattDeviceService.GetDeviceSelectorFromUuid(GattServiceUuids.HeartRate),
                new string[] { "System.Devices.ContainerId" });

            DeviceInformationCollection? foundDevices = null;
            try
            {
                while (true)
                {
                    _logger.LogDebug("Attempting to retrieve async result...");

                    Thread.Sleep(100);

                    var status = task.Status;

                    if (status == Windows.Foundation.AsyncStatus.Canceled || task.Status == Windows.Foundation.AsyncStatus.Error)
                        break;

                    if (status == Windows.Foundation.AsyncStatus.Completed)
                    {
                        foundDevices = task.GetResults();
                        break;
                    }
                }
            }
            finally
            {
                task.Close();
            }

            if (foundDevices == null)
                throw new Exception("No Devices found");

            return foundDevices;
        }
    }
}