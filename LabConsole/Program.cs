using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Marten;
using NServiceBus.Persistence;

namespace LabConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var runner = new Runner();

            var endpoint = runner.Run().Result;

            Console.WriteLine("Running...");

            while (true)
            {
                Thread.Sleep(500);
                var id = Guid.NewGuid().ToString("N");
                Console.WriteLine("Press enter to import product");
                Console.ReadLine();
                endpoint.Publish(new AccommodationProductImported { Id = id });

                Thread.Sleep(500);
                Console.WriteLine("Press enter to import cost");
                Console.ReadLine();
                endpoint.Publish(new AccommodationCostImported { ProductId = id, Cost = 5.4m });

            }
        }
    }

    public class Runner
    {
        public async Task<IEndpointInstance> Run()
        {
            var endpointConfiguration = new EndpointConfiguration("postgres.lab");
            endpointConfiguration.SendFailedMessagesTo("postgres.lab.error");
            endpointConfiguration.UseSerialization<JsonSerializer>();
            endpointConfiguration.EnableInstallers();
            var persistenceExtensions = endpointConfiguration.UsePersistence<MartenPersistence, StorageType.Sagas>();

            //persistenceExtensions.ConnectionString(ConfigurationManager.ConnectionStrings["PostgreSql.Connection"].ConnectionString);


            endpointConfiguration.UsePersistence<InMemoryPersistence, StorageType.Timeouts>();
            endpointConfiguration.UsePersistence<InMemoryPersistence, StorageType.GatewayDeduplication>();
            endpointConfiguration.UsePersistence<InMemoryPersistence, StorageType.Outbox>();
            endpointConfiguration.UsePersistence<InMemoryPersistence, StorageType.Subscriptions>();

            var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
            transport.ConnectionStringName("NServiceBus.RabbitMQ.ConnectionString");

            return await Endpoint.Start(endpointConfiguration);
        }
    }


}
