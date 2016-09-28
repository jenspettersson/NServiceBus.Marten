using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Marten;
using NServiceBus;
using NServiceBus.InMemory.Outbox;
using NServiceBus.Marten;
using NServiceBus.Marten.Outbox;
using NServiceBus.Marten.Sagas;
using NServiceBus.Marten.Timeouts;
using NServiceBus.Persistence;

namespace LabConsole
{
    class Program
    {
        private static IEndpointInstance _endpoint;

        static void Main(string[] args)
        {
            var runner = new Runner();

            _endpoint = runner.Run().Result;

            Console.WriteLine("Running...");

            while (true)
            {
                Thread.Sleep(1000);
                Console.Write("Select 1/2: ");
                var selection = Console.ReadLine();

                switch (selection)
                {
                    case "1":
                        TestSaga();
                        break;
                    case "2":
                        TestTimeout();
                        break;
                }
            }
        }

        private static void TestTimeout()
        {
            Console.WriteLine("Sending FailMiserably command");
            _endpoint.SendLocal(new FailMiserably("TESTING"));
        }

        private static void TestSaga()
        {
            Thread.Sleep(500);
            var id = Guid.NewGuid().ToString("N");
            Console.WriteLine("Press enter to import product");
            Console.ReadLine();
            _endpoint.Publish(new AccommodationProductImported {Id = id});

            Thread.Sleep(500);
            Console.WriteLine("Press enter to import cost");
            Console.ReadLine();
            _endpoint.Publish(new AccommodationCostImported {ProductId = id, Cost = 5.4m});
        }
    }

    public class Runner
    {
        public async Task<IEndpointInstance> Run()
        {
            var sharedDocumentStore = CreateSharedDocumentStore();
            var sagaDocumentStore = CreateSagaDocumentStore();
            var timeoutsDocumentStore = CreateTimeoutsDocumentStore();

            var endpointConfiguration = new EndpointConfiguration("postgres.lab");
            endpointConfiguration.SendFailedMessagesTo("postgres.lab.error");
            endpointConfiguration.UseSerialization<JsonSerializer>();
            endpointConfiguration.EnableInstallers();

            endpointConfiguration.EnableOutbox();
            endpointConfiguration.SetTimeToKeepDeduplicationData(TimeSpan.FromSeconds(15));
            endpointConfiguration.SetFrequencyToRunDeduplicationDataCleanup(TimeSpan.FromSeconds(30));

            var persistenceExtensions = endpointConfiguration.UsePersistence<MartenPersistence>();
            persistenceExtensions.SetDefaultDocumentStore(sharedDocumentStore);

            //persistenceExtensions.UseDocumentStoreForSagas(sagaDocumentStore);
            //persistenceExtensions.UseDocumentStoreForTimeouts(timeoutsDocumentStore);
            

            endpointConfiguration.UsePersistence<InMemoryPersistence, StorageType.GatewayDeduplication>();
            //endpointConfiguration.UsePersistence<InMemoryPersistence, StorageType.Outbox>();
            endpointConfiguration.UsePersistence<InMemoryPersistence, StorageType.Subscriptions>();

            var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
            transport.ConnectionStringName("NServiceBus.RabbitMQ.ConnectionString");

            var recoverabilitySettings = endpointConfiguration.Recoverability();
            recoverabilitySettings.Immediate(x => x.NumberOfRetries(0));
            recoverabilitySettings.Delayed(x => x.NumberOfRetries(2).TimeIncrease(TimeSpan.FromSeconds(5)));

            return await Endpoint.Start(endpointConfiguration);
        }

        private static DocumentStore CreateSharedDocumentStore()
        {
            var sagaConnectionString = "host=localhost;database=nsb.persistence;username=postgres;password=admin";
            var sagaDocumentStore = DocumentStore.For(_ =>
            {
                _.Connection(sagaConnectionString);

                _.Schema.For<SagaDocument>()
                    .Index(x => x.CorrelationProperty)
                    .Index(x => x.CorrelationPropertyValue)
                    .Index(x => x.Type);

                _.Schema.For<TimeoutDocument>().UseOptimisticConcurrency(true);

                //Todo: Change to none
                _.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;
            });
            return sagaDocumentStore;
        }

        private static DocumentStore CreateSagaDocumentStore()
        {
            var sagaConnectionString = "host=localhost;database=nsb.persistence.sagas;username=postgres;password=admin";
            var sagaDocumentStore = DocumentStore.For(_ =>
            {
                _.Connection(sagaConnectionString);

                _.Schema.For<SagaDocument>()
                    .Index(x => x.CorrelationProperty)
                    .Index(x => x.CorrelationPropertyValue)
                    .Index(x => x.Type);

                //_.Schema.For<TimeoutDocument>().UseOptimisticConcurrency(true);

                //Todo: Change to none
                _.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;
            });
            return sagaDocumentStore;
        }

        private static DocumentStore CreateTimeoutsDocumentStore()
        {
            var timeoutsConnectionString = "host=localhost;database=nsb.persistence.timeouts;username=postgres;password=admin";

            var timeoutsDocumentStore = DocumentStore.For(_ =>
            {
                _.Connection(timeoutsConnectionString);
                _.Schema.For<TimeoutDocument>().UseOptimisticConcurrency(true);

                //Todo: Change to none
                _.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;
            });
            return timeoutsDocumentStore;
        }
    }


}
