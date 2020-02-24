using Windows.Devices.Enumeration;

namespace TreadmillBridge.TreadmillClient
{
    public interface ITreadmillClientFactory
    {
        ITreadmillClient CreateDomyosTreadmillClient(DeviceInformation device);
    }
}
