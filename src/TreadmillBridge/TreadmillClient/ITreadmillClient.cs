using System.Threading;
using System.Threading.Tasks;

namespace TreadmillBridge.TreadmillClient
{
    public interface ITreadmillClient
    {
        Task InitializeAsync();
        Task StartTapeAsync(CancellationToken cancellationToken);
        Task StopTapeAsync(CancellationToken cancellationToken);
    }
}
