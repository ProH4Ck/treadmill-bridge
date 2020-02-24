using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using TreadmillBridge.Helpers;
using TreadmillBridge.Services.VirtualTreadmill;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace TreadmillBridge.TreadmillClient
{
    public class DomyosTreadmillClient : BleTreadmillClient
    {
        private readonly ILogger<DomyosTreadmillClient> _logger;
        private readonly IVirtualTreadmillService _virtualTreadmillService;

        private readonly Guid _gattCommunicationChannelServiceId = new Guid("49535343-fe7d-4ae5-8fa9-9fafd205e455");
        private readonly Guid _gattWriteCharacteristicId = new Guid("49535343-8841-43f4-a8d4-ecbe34729bb3");
        private readonly Guid _gattNotifyCharacteristicId = new Guid("49535343-1e4d-4bd9-ba61-23c647249616");

        private GattCharacteristic _gattWriteCharacteristic;
        private GattCharacteristic _gattNotifyCharacteristic;

        private bool _isTapeRunning;

        public DomyosTreadmillClient(ILogger<DomyosTreadmillClient> logger,
            IVirtualTreadmillService virtualTreadmillService,
            DeviceInformation device) 
            : base(device)
        {
            _logger = logger;
            _virtualTreadmillService = virtualTreadmillService;
        }

        public override async Task InitializeAsync()
        {
            await GetCharacteristicsAsync();

            await _gattNotifyCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify);

            await _gattWriteCharacteristic.WriteValueAsync(initData1);
            await _gattWriteCharacteristic.WriteValueAsync(initData2);

            await _gattWriteCharacteristic.WriteValueAsync(initDataStart);
            await _gattWriteCharacteristic.WriteValueAsync(initDataStart2);
            await _gattWriteCharacteristic.WriteValueAsync(initDataStart3);
            await _gattWriteCharacteristic.WriteValueAsync(initDataStart4);
            await _gattWriteCharacteristic.WriteValueAsync(initDataStart5);

            await _gattWriteCharacteristic.WriteValueAsync(initDataStart6);
            await _gattWriteCharacteristic.WriteValueAsync(initDataStart7);

            await _gattWriteCharacteristic.WriteValueAsync(initDataStart8);
            await _gattWriteCharacteristic.WriteValueAsync(initDataStart9);
        }

        public override async Task StartTapeAsync(CancellationToken cancellationToken)
        {
            await SendStartCommandAsync();

            _gattNotifyCharacteristic.ValueChanged += GattNotifyCharacteristicOnValueChanged;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (_isTapeRunning)
                    await _gattWriteCharacteristic.WriteValueAsync(noOpData);
                await Task.Delay(200, cancellationToken);
            }

            await SendStopCommandAsync();
        }

        public override Task StopTapeAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("To stop tape request token cancellation on StartTapeAsync");
        }

        private async Task GetCharacteristicsAsync()
        {
            var bleDevice = await BluetoothLEDevice.FromIdAsync(Device.Id);
            var serviceRequest = await bleDevice.GetGattServicesForUuidAsync(_gattCommunicationChannelServiceId);
            if (serviceRequest.Status != GattCommunicationStatus.Success)
                throw new Exception($"Cannot get service with UUID '{_gattCommunicationChannelServiceId}'");
            var service = serviceRequest.Services[0];
            var characteristicsRequest = await service.GetCharacteristicsAsync();
            if (characteristicsRequest.Status != GattCommunicationStatus.Success)
                throw new Exception("Cannot get characteristics");
            _gattWriteCharacteristic = characteristicsRequest.Characteristics.SingleOrDefault(c => c.Uuid.Equals(_gattWriteCharacteristicId));
            _gattNotifyCharacteristic = characteristicsRequest.Characteristics.SingleOrDefault(c => c.Uuid.Equals(_gattNotifyCharacteristicId));
        }

        private async Task SendStartCommandAsync()
        {
            await _gattWriteCharacteristic.WriteValueAsync(initDataStart10);
            await _gattWriteCharacteristic.WriteValueAsync(initDataStart11);

            await _gattWriteCharacteristic.WriteValueAsync(initDataStart12);
            await _gattWriteCharacteristic.WriteValueAsync(initDataStart13);

            _isTapeRunning = true;
        }

        private async Task SendStopCommandAsync()
        {
            await _gattWriteCharacteristic.WriteValueAsync(initDataF0C800B8);

            _isTapeRunning = false;
        }

        private byte[] _lastPacket;
#pragma warning disable 1998
        private async void GattNotifyCharacteristicOnValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var currentPacket = args.CharacteristicValue.ToArray();
            if (_lastPacket != null && currentPacket.SequenceEqual(_lastPacket))
                return;

            _lastPacket = currentPacket;
            if (currentPacket.Length != 26)
                return;

            var speed = GetSpeedFromPacket(currentPacket);
            var incline = GetInclinationFromPacket(currentPacket);
            //var isStartPressed = GetIsStartPressedFromPacket(currentPacket);
            //var isStopPressed = GetIsStopPressedFromPacket(currentPacket);

#if DEBUG            
            Debug.WriteLine(args.CharacteristicValue.ToArray().HexDump());
#endif

            _logger.LogDebug("Current speed: {speed} kph", speed);
            _logger.LogDebug("Current incline: {incline} %", incline);

            //TODO: When STOP is pressed command are disabled and tape doesn't restart... uncomment when fix is found
            //_logger.LogDebug("START pressed: {startPressed}", isStartPressed);
            //_logger.LogDebug("STOP pressed: {stopPressed}", isStopPressed);
            //
            //if (isStopPressed && _isTapeRunning)
            //    await SendStopCommandAsync();

            //if (isStartPressed && !_isTapeRunning)
            //    await SendStartCommandAsync();


            _virtualTreadmillService.UpdatedSpeed(speed);
        }

        private static double GetSpeedFromPacket(byte[] packet)
        {
            var convertedData = (int)packet[7];
            var data = convertedData / 10.0d;
            return data;
        }

        private static double GetInclinationFromPacket(byte[] packet)
        {
            var convertedData = (packet[2] << 8) | packet[3];
            var data = (convertedData - 1000) / 10.0d;
            if (data < 0) return 0;
            return data;
        }

        private static bool GetIsStartPressedFromPacket(byte[] packet)
        {
            var ba = new BitArray(new[] { packet[25] });
            return ba[1];
        }

        private static bool GetIsStopPressedFromPacket(byte[] packet)
        {
            var ba = new BitArray(new [] {packet[25]});
            return ba[0] && ba[5];
        }

        // set speed and incline to 0
        private IBuffer initData1 = new byte[] { 0xf0, 0xc8, 0x01, 0xb9 }.AsBuffer();
        private IBuffer initData2 = new byte[] { 0xf0, 0xc9, 0xb9 }.AsBuffer();

        private IBuffer noOpData = new byte[] { 0xf0, 0xac, 0x9c }.AsBuffer();

        // stop tape
        private IBuffer initDataF0C800B8 = new byte[] { 0xf0, 0xc8, 0x00, 0xb8 }.AsBuffer();


        // main startup sequence
        private IBuffer initDataStart = new byte[] { 0xf0, 0xa3, 0x93 }.AsBuffer();
        private IBuffer initDataStart2 = new byte[] { 0xf0, 0xa4, 0x94 }.AsBuffer();
        private IBuffer initDataStart3 = new byte[] { 0xf0, 0xa5, 0x95 }.AsBuffer();
        private IBuffer initDataStart4 = new byte[] { 0xf0, 0xab, 0x9b }.AsBuffer();
        private IBuffer initDataStart5 = new byte[] { 0xf0, 0xc4, 0x03, 0xb7 }.AsBuffer();
        private IBuffer initDataStart6 = new byte[]
        {
                0xf0, 0xad, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x01, 0xff
        }.AsBuffer();
        private IBuffer initDataStart7 = new byte[] { 0xff, 0xff, 0x8b }.AsBuffer(); // power on bt icon
        private IBuffer initDataStart8 = new byte[]
        {
                0xf0, 0xcb, 0x02, 0x00, 0x08, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x01, 0x00
        }.AsBuffer();
        private IBuffer initDataStart9 = new byte[] { 0x00, 0x01, 0xff, 0xff, 0xff, 0xff, 0xb6 }.AsBuffer(); // power on bt word
        private IBuffer initDataStart10 = new byte[]
        {
                0xf0, 0xad, 0xff, 0xff, 0x00, 0x05, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0x00, 0x00, 0xff, 0xff, 0xff, 0x01, 0xff
        }.AsBuffer();
        private IBuffer initDataStart11 = new byte[] { 0xff, 0xff, 0x94 }.AsBuffer(); // start tape
        private IBuffer initDataStart12 = new byte[]
        {
                0xf0, 0xcb, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0x01, 0x00, 0x14, 0x01, 0xff, 0xff
        }.AsBuffer();
        private IBuffer initDataStart13 = new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xbd }.AsBuffer();
    }
}
