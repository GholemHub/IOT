using Opc.UaFx;
using Opc.UaFx.Client;
using Projekt.VirtualDevice;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices;
using System.Resources;


class Program
{

    private static async Task Main(string[] args)
    {

        using (var opcClient = new OpcClient("opc.tcp://localhost:4840/"))
        {
            try
            {
                opcClient.Connect()
                Dictionary<string, string> OpcToIoTHubIds = new Dictionary<string, string>();
                OpcToIoTHubIds.Add("ns=2;s=Device 1", "device-1");
                OpcToIoTHubIds.Add("ns=2;s=Device 2", "device-2");
                OpcToIoTHubIds.Add("ns=2;s=Device 3", "device-3");
                OpcToIoTHubIds.Add("ns=2;s=Device 4", "device-4");

                Dictionary<VirtualDevice, OpcDeviceData> iotHubDeviceToOpcDeviceData = new Dictionary<VirtualDevice, OpcDeviceData>();
                foreach (var opcIdToIotId in OpcToIoTHubIds)
                {
                    DeviceClient deviceClient = DeviceClient.CreateFromConnectionString("HostName=iot-zajecia-ul.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=9d4Qw9xBXl0t7HLhA9ebzgeuoDl56amtf1rJOm57ps0=", opcIdToIotId.Value);
                    await deviceClient.OpenAsync();
                    VirtualDevice vDevice = new VirtualDevice(deviceClient, opcClient);
                    OpcDeviceData deviceData = new OpcDeviceData(opcIdToIotId.Key, opcIdToIotId.Value);

                    readOpcDeviceData(opcClient, deviceData);

                    Console.WriteLine("OPCUA Device \"{0}\" is connected to IoT device \"{1}\"", opcIdToIotId.Key, opcIdToIotId.Value);

                    await vDevice.SetTwinDataAsync(deviceData.DeviceError, deviceData.ProductionRate, deviceData.LastMaitananceDate.Date, deviceData.LastErrorDate);
                    await vDevice.InitializeHandlers(opcIdToIotId.Key);

                    if (deviceData.DeviceError > 0) sendDeviceErrorReport(vDevice, deviceData);

                    iotHubDeviceToOpcDeviceData.Add(vDevice, deviceData);
                }

                while (true)
                {
                    foreach (var device in iotHubDeviceToOpcDeviceData)
                    {
                        int prevErrorCode = device.Value.DeviceError;
                        int prevProdRate = device.Value.ProductionRate;
                        readOpcDeviceData(opcClient, device.Value);
                        if (device.Value.ProductionStatus == 1)
                        {
                            await device.Key.SendMessage(device.Value.getTelemetryJSON());
                            if (device.Value.DeviceError > 0 && device.Value.DeviceError != prevErrorCode)
                            {
                                sendDeviceErrorReport(device.Key, device.Value);
                            }
                            if (device.Value.ProductionRate != prevProdRate)
                            {
                                await device.Key.UpdateTwinProductionRateAsync(device.Value.DeviceError, device.Value.ProductionRate);
                            }
                        }
                    }
                    Console.WriteLine();
                    await Task.Delay(5000);
                }
                opcClient.Disconnect();
            }
            catch (OpcException opcex)
            {
                Console.WriteLine(opcex.Message);
            }
        }


    }

    private async static void sendDeviceErrorReport(VirtualDevice virtualDevice, OpcDeviceData deviceData)
    {
        Console.WriteLine("Error from device {0} was reported", deviceData.IoTDeviceId);
        await virtualDevice.SendMessage(deviceData.getErrorsJSON());
        await virtualDevice.UpdateTwinErrorDataAsync(deviceData.DeviceError);
    }

    private static void readOpcDeviceData(OpcClient opcClient, OpcDeviceData deviceData)
    {
        deviceData.ProductionStatus = (int)opcClient.ReadNode(deviceData.nodeId + "/ProductionStatus").Value;
        deviceData.WorkorderId = (string)opcClient.ReadNode(deviceData.nodeId + "/WorkorderId").Value;
        deviceData.ProductionRate = (int)opcClient.ReadNode(deviceData.nodeId + "/ProductionRate").Value;
        deviceData.GoodCount = (long)opcClient.ReadNode(deviceData.nodeId + "/GoodCount").Value;
        deviceData.BadCount = (long)opcClient.ReadNode(deviceData.nodeId + "/BadCount").Value;
        deviceData.Temperature = (double)opcClient.ReadNode(deviceData.nodeId + "/Temperature").Value;
        deviceData.DeviceError = (int)opcClient.ReadNode(deviceData.nodeId + "/DeviceError").Value;
    }

}

