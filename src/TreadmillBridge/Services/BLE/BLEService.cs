using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace TreadmillBridge.Services.BLE
{
    public class BLEService : IBLEService
    {
        private readonly ILogger<BLEService> _logger;

        private Dictionary<string, DeviceInformation> _bleDevices;

        public BLEService(ILogger<BLEService> logger)
        {
            _logger = logger;
        }

        public async Task<IEnumerable<DeviceInformation>> ScanAsync(CancellationToken cancellationToken)
        {
            void DeviceWatcherRemoved(DeviceWatcher sender, DeviceInformationUpdate args)
            {
                _bleDevices.Remove(args.Id);
            }

            static void DeviceWatcherUpdated(DeviceWatcher sender, DeviceInformationUpdate args)
            {
            }

            void DeviceWatcherAdded(DeviceWatcher sender, DeviceInformation args)
            {
                _bleDevices.Add(args.Id, args);
            }

            _bleDevices = new Dictionary<string, DeviceInformation>();
            var requestedProperties = new[] { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };
            var deviceWatcher =
                DeviceInformation.CreateWatcher(
                    BluetoothLEDevice.GetDeviceSelectorFromPairingState(false),
                    requestedProperties,
                    DeviceInformationKind.AssociationEndpoint);
            deviceWatcher.Added += DeviceWatcherAdded;
            deviceWatcher.Updated += DeviceWatcherUpdated;
            deviceWatcher.Removed += DeviceWatcherRemoved;

            _logger.LogDebug("Starting scan");
            deviceWatcher.Start();
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            _logger.LogDebug("Finishing scan");
            deviceWatcher.Stop();

            return _bleDevices.Values.AsEnumerable();
        }
    }
}
