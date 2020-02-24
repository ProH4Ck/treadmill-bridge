using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using TreadmillBridge.Services.VirtualTreadmill;

namespace TreadmillBridge.BackgroundServices
{
    public class TreadmillServerBackgroundService : BackgroundService
    {
        private readonly IVirtualTreadmillService _virtualTreadmillService;

        public TreadmillServerBackgroundService(IVirtualTreadmillService virtualTreadmillService)
        {
            _virtualTreadmillService = virtualTreadmillService;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _virtualTreadmillService.StartAsync(stoppingToken);
        }
    }
}
