using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Arcus.WebApi.Tests.Integration.Logging.Controllers
{
    [ApiController]
    [Route(Route)]
    public class ArcusStockController : ControllerBase
    {
        private readonly ILogger<ArcusStockController> _logger;
        public const string Route = "arcus/stock";

        /// <summary>
        /// Initializes a new instance of the <see cref="ArcusStockController" /> class.
        /// </summary>
        public ArcusStockController(ILogger<ArcusStockController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            _logger.LogAzureKeyVaultDependency("https://my-vault.azure.net", "Sql-connection-string", isSuccessful: true, DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));

            SimulateSqlQueryWithMicrosoftTracking();
            await SimulateServiceBusWithMicrosoftTrackingAsync();
            await SimulateEventHubsWithMicrosoftTrackingAsync();

            return Ok(new[] { "Cabinets", "Tables", "Chairs" });
        }

        private static void SimulateSqlQueryWithMicrosoftTracking()
        {
            try
            {
                using (var connection = new SqlConnection("Data Source=(localdb)\\MSSQLLocalDB;Database=master"))
                {
                    connection.Open();
                    using (SqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM Orders";
                        command.ExecuteNonQuery();
                    }

                    connection.Close();
                }
            }
            catch
            {
                // Ignore:
                // We only want to simulate a SQL connection/command, no need to actually set this up.
                // A failure will still result in a dependency telemetry instance that we can assert on.
            }
        }

        private static async Task SimulateServiceBusWithMicrosoftTrackingAsync()
        {
            try
            {
                var message = new ServiceBusMessage();
                await using (var client = new ServiceBusClient("Endpoint=sb://something.servicebus.windows.net/;SharedAccessKeyName=something;SharedAccessKey=something=;EntityPath=something"))
                await using (ServiceBusSender sender = client.CreateSender("something"))
                {
                    await sender.SendMessageAsync(message);
                }
            }
            catch
            {
                // Ignore:
                // We only want to simulate a Service Bus connection, no need to actually set this up.
                // A failure will still result in a dependency telemetry instance that we can assert on.
            }
        }

        private static async Task SimulateEventHubsWithMicrosoftTrackingAsync()
        {
            try
            {
                var message = new EventData("something to send");
                await using (var sender = new EventHubProducerClient("Endpoint=sb://<NamespaceName>.servicebus.windows.net/;SharedAccessKeyName=<KeyName>;SharedAccessKey=<KeyValue>", "something"))
                {
                    await sender.SendAsync(new[] { message });
                }
            }
            catch
            {
                // Ignore:
                // We only want to simulate an EventHubs connection, no need to actually set this up.
                // A failure will still result in a dependency telemetry instance that we can assert on.
            }
        }
    }
}
