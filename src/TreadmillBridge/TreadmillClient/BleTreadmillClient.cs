using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace TreadmillBridge.TreadmillClient
{
    public abstract class BleTreadmillClient : ITreadmillClient
    {
        protected DeviceInformation Device;

        protected BleTreadmillClient(DeviceInformation device)
        {
            Device = device;
        }

        public abstract Task InitializeAsync();

        public abstract Task StartTapeAsync(CancellationToken cancellationToken);

        public abstract Task StopTapeAsync(CancellationToken cancellationToken);
    }
}
