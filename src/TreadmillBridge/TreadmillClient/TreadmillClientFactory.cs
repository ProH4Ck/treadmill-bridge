using Microsoft.Extensions.Logging;
using TreadmillBridge.Services.VirtualTreadmill;
using Windows.Devices.Enumeration;

namespace TreadmillBridge.TreadmillClient
{
    public class TreadmillClientFactory : ITreadmillClientFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IVirtualTreadmillService _virtualTreadmillService;

        public TreadmillClientFactory(ILoggerFactory loggerFactory, IVirtualTreadmillService virtualTreadmillService)
        {
            _loggerFactory = loggerFactory;
            _virtualTreadmillService = virtualTreadmillService;
        }

        public ITreadmillClient CreateDomyosTreadmillClient(DeviceInformation device)
        {
            return new DomyosTreadmillClient(_loggerFactory.CreateLogger<DomyosTreadmillClient>(),
                _virtualTreadmillService, device);
        }
    }
}
