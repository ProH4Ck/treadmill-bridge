using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TreadmillBridge.Services.BLE;
using TreadmillBridge.TreadmillClient;
using Windows.Devices.Enumeration;

namespace TreadmillBridge.BackgroundServices
{
    public class TreadmillClientBackgroundService : BackgroundService
    {
        private readonly ILogger<TreadmillClientBackgroundService> _logger;
        private readonly IBLEService _bleService;
        private readonly ITreadmillClientFactory _treadmillClientFactory;

        public TreadmillClientBackgroundService(ILogger<TreadmillClientBackgroundService> logger,
            IBLEService bleService,
            ITreadmillClientFactory treadmillClientFactory)
        {
            _logger = logger;
            _bleService = bleService;
            _treadmillClientFactory = treadmillClientFactory;
        }

        private DeviceInformation _device;
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogDebug("Initializing Domyos client");

            var devices = await _bleService.ScanAsync(stoppingToken);

            foreach (var device in devices)
            {
                if (!device.Name.StartsWith("Domyos", StringComparison.InvariantCultureIgnoreCase))
                    continue;
                _device = device;
                break;
            }

            _logger.LogInformation("Connecting to {deviceName}", _device.Name);
            var treadmill = _treadmillClientFactory.CreateDomyosTreadmillClient(_device);
            _logger.LogDebug("Initializing {deviceName}", _device.Name);
            await treadmill.InitializeAsync();
            _logger.LogDebug("Starting {deviceName}", _device.Name);
            await treadmill.StartTapeAsync(stoppingToken);
        }
    }
}
