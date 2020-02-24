using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace TreadmillBridge.Services.VirtualTreadmill
{
    public class VirtualTreadmillService : IVirtualTreadmillService
    {
        private static readonly Guid FitnessMachineServiceUuid = new Guid("00001826-0000-1000-8000-00805F9B34FB");
        private static readonly Guid FitnessMachineFeatureCharacteristicUuid =
            new Guid("00002ACC-0000-1000-8000-00805F9B34FB");
        private static readonly Guid TreadmillDataCharacteristicUuid =
            new Guid("00002ACD-0000-1000-8000-00805F9B34FB");

        private GattServiceProvider _gattServiceProviderFitnessMachine;
        private GattLocalCharacteristic _treadmillDataCharacteristic;

        private double _currentSpeed;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // FitnessMachine service
            var gattServiceFitnessMachineRequest = await GattServiceProvider.CreateAsync(FitnessMachineServiceUuid);

            if (gattServiceFitnessMachineRequest.Error != BluetoothError.Success)
            {
                throw new Exception("Cannot create Gatt Service Provider"); 
            }

            _gattServiceProviderFitnessMachine = gattServiceFitnessMachineRequest.ServiceProvider;

            // Fitness Machine Feature characteristic 
            var fitnessMachineFeatureData = new byte[]
            {
                0x08, 0x00, 0x00, 0x00
            };
            var targetSettingsData = new byte[]
            {
                0x00, 0x00, 0x00, 0x00
            };
            var fitnessMachineFeatureDataFull = fitnessMachineFeatureData.Concat(targetSettingsData).ToArray();
            var fitnessMachineFeatureCharacteristicRequest = new GattLocalCharacteristicParameters
            {
                CharacteristicProperties = GattCharacteristicProperties.Read,
                StaticValue = fitnessMachineFeatureDataFull.AsBuffer(),
                ReadProtectionLevel = GattProtectionLevel.Plain,
            };
            var characteristicResult = await _gattServiceProviderFitnessMachine.Service.CreateCharacteristicAsync(
                FitnessMachineFeatureCharacteristicUuid, fitnessMachineFeatureCharacteristicRequest);
            if (characteristicResult.Error != BluetoothError.Success)
            {
                throw new Exception("Cannot create Characteristic"); 
            }
            
            // Treadmill Data characteristic
            var treadmillDataCharacteristicRequest = new GattLocalCharacteristicParameters
            {
                CharacteristicProperties = GattCharacteristicProperties.Read | GattCharacteristicProperties.Notify,
                ReadProtectionLevel = GattProtectionLevel.Plain
            };
            characteristicResult = await _gattServiceProviderFitnessMachine.Service.CreateCharacteristicAsync(TreadmillDataCharacteristicUuid, treadmillDataCharacteristicRequest);
            if (characteristicResult.Error != BluetoothError.Success)
            {
                throw new Exception("Cannot create Characteristic"); 
            }

            _treadmillDataCharacteristic = characteristicResult.Characteristic;
            _treadmillDataCharacteristic.ReadRequested += TreadmillDataCharacteristic_ReadRequestedAsync;

            // Advertising
            byte[] flagsData =
            {
                0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };
            byte[] fitnessMachineTypeData =
            {
                0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };
            var serviceData = flagsData.Concat(fitnessMachineTypeData).ToArray();
            var advParameters = new GattServiceProviderAdvertisingParameters
            {
                IsDiscoverable = true,
                IsConnectable = true,
                ServiceData = serviceData.AsBuffer()
            };

            // Go
            _gattServiceProviderFitnessMachine.StartAdvertising(advParameters);

            while (true)
            {
                await Task.Delay(1000, cancellationToken);
                await _treadmillDataCharacteristic.NotifyValueAsync(GetTreadmillDataPackage(_currentSpeed));
            }
        }

        public void UpdatedSpeed(double speed)
        {
            _currentSpeed = speed;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _gattServiceProviderFitnessMachine.StopAdvertising();
            await Task.Delay(100, cancellationToken);
        }

        private async void TreadmillDataCharacteristic_ReadRequestedAsync(GattLocalCharacteristic sender,
            GattReadRequestedEventArgs args)
        {
            using (args.GetDeferral())
            {
                var request = await args.GetRequestAsync();
                request.RespondWithValue(GetTreadmillDataPackage(_currentSpeed));
            }
        }

        private static IBuffer GetTreadmillDataPackage(double speed)
        {
            // normalize speed
            var normalizedSpeed = (uint) Math.Round(speed * 100, 2);
            var speedBytes = BitConverter.GetBytes(normalizedSpeed);

            // flags: 00010000 00000000
            var flags = new byte[] {0x08, 0x00};

            // instant speed
            var instantSpeed = speedBytes;

            // incline (not handled)
            var incline = new byte[] {0x00, 0x00};

            // ramp angle (auto calculated)
            var rampAngle = new byte[] {0xFF, 0x7F};

            return flags.Concat(instantSpeed).Concat(incline).Concat(rampAngle).ToArray().AsBuffer();
        }
    }
}
