using System.Threading;
using System.Threading.Tasks;

namespace TreadmillBridge.Services.VirtualTreadmill
{
    public interface IVirtualTreadmillService
    {
        Task StartAsync(CancellationToken cancellationToken);
        void UpdatedSpeed(double speed);
        Task StopAsync(CancellationToken cancellationToken);
    }
}
