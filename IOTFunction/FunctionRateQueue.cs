using System;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;

namespace IOTFunction
{
    public static class FunctionRateQueue
    {
        [FunctionName("FunctionDecreaseRateQueue")]
        public static async Task Run([ServiceBusTrigger("%ServiceBusDecreaseRateQueue%", Connection = "ServiceBusConnectionString")] ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions, ILogger log, ExecutionContext context)
        {
            var messageBody = JsonConvert.DeserializeObject<DecreaseRateMessage>(Encoding.UTF8.GetString(message.Body));
            log.LogInformation($"Recieved decrease production rate message: {message.Body}");
            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString("HostName=iot-zajecia-ul.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=9d4Qw9xBXl0t7HLhA9ebzgeuoDl56amtf1rJOm57ps0=");

            log.LogInformation("DecreaseProductRate call result:");
            CloudToDeviceMethod emergencyStopMethod = new CloudToDeviceMethod("DecreaseProductRate");
            emergencyStopMethod.ResponseTimeout = TimeSpan.FromSeconds(20);
            CloudToDeviceMethodResult emergencyStopMethodResult = await serviceClient.InvokeDeviceMethodAsync(messageBody.deviceId, emergencyStopMethod);
            log.LogInformation(emergencyStopMethodResult.Status.ToString());
            log.LogInformation(emergencyStopMethodResult.GetPayloadAsJson());
        }

        class DecreaseRateMessage
        {
            public string deviceId { get; set; }
            public DateTime time { get; set; }
        }
    }
}
