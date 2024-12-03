using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Storage.Queues;

namespace TimerTriggerApp
{
    public class TimerFunctions
    {
        private readonly ILogger<TimerFunctions> _logger;
        private const string QueueName = "products-queue";
        private const string ConnectionString = "AzureWebJobsStorage"; 

        public TimerFunctions(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TimerFunctions>();
        }

        [Function("AddToQueue")]
        public async Task AddToQueue([TimerTrigger("*/10 * * * * *")] TimerInfo myTimer)
        {
            var queueClient = new QueueClient(ConnectionString, QueueName);
            await queueClient.CreateIfNotExistsAsync();

            var productData = $"Product-{Guid.NewGuid()}";
            await queueClient.SendMessageAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(productData)));

            _logger.LogInformation($"Added to queue: {productData} at {DateTime.Now}");
        }

        [Function("ProcessQueue")]
        public async Task ProcessQueue([TimerTrigger("*/15 * * * * *")] TimerInfo myTimer)
        {
            var queueClient = new QueueClient(ConnectionString, QueueName);

            if (!await queueClient.ExistsAsync())
            {
                _logger.LogWarning($"Queue '{QueueName}' does not exist.");
                return;
            }

            var message = await queueClient.ReceiveMessageAsync();
            if (message.Value != null)
            {
                var productData = Encoding.UTF8.GetString(Convert.FromBase64String(message.Value.MessageText));
                _logger.LogInformation($"Processed from queue: {productData}");

                await queueClient.DeleteMessageAsync(message.Value.MessageId, message.Value.PopReceipt);
                _logger.LogInformation($"Deleted message: {productData}");
            }
            else
            {
                _logger.LogInformation("Queue is empty, no messages to process.");
            }
        }
    }
}
