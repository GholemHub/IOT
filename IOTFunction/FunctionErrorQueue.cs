using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Devices;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace IOTFunction
{
    public static class FunctionErrorQueue
    {

        [FunctionName("FunctionErrorQueue")]
        public static async Task Run([ServiceBusTrigger("%ServiceBusErrorQueue%", Connection = "ServiceBusConnectionString")] ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions, ILogger log, ExecutionContext context)
        {
            var messageBody = JsonConvert.DeserializeObject<EmergencyStopErrorMessage>(Encoding.UTF8.GetString(message.Body));
            log.LogInformation($"Recieved emergency stop message: {message.Body}");

            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString("HostName=iot-zajecia-ul.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=9d4Qw9xBXl0t7HLhA9ebzgeuoDl56amtf1rJOm57ps0=");

            log.LogInformation("Emergency stop call result:");
            CloudToDeviceMethod emergencyStopMethod = new CloudToDeviceMethod("EmergencyStop");
            emergencyStopMethod.ResponseTimeout = TimeSpan.FromSeconds(20);
            CloudToDeviceMethodResult emergencyStopMethodResult = await serviceClient.InvokeDeviceMethodAsync(messageBody.deviceId, emergencyStopMethod);
            log.LogInformation(emergencyStopMethodResult.Status.ToString());
            log.LogInformation(emergencyStopMethodResult.GetPayloadAsJson());
        }

        class EmergencyStopErrorMessage
        {
            public string deviceId { get; set; }
            public double errorSum { get; set; }
            public DateTime time { get; set; }
        }

    }
}
