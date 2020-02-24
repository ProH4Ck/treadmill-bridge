using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace TreadmillBridge.Services.BLE
{
    public interface IBLEService
    {
        Task<IEnumerable<DeviceInformation>> ScanAsync(CancellationToken cancellationToken);
    }
}
