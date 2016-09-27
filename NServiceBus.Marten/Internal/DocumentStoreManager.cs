using System;
using Marten;
using NServiceBus.Logging;
using NServiceBus.Marten.Sagas;
using NServiceBus.Persistence;
using NServiceBus.Settings;

namespace NServiceBus.Marten.Internal
{
    static class DocumentStoreManager
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(DocumentStoreManager));

        public static IDocumentStore GetDocumentStore<TStorageType>(ReadOnlySettings settings)
            where TStorageType : StorageType
        {
            return GetDocumentStoreFor<TStorageType>(settings);
        }

        internal static IDocumentStore GetDocumentStoreFor<TStorageType>(ReadOnlySettings settings)
           where TStorageType : StorageType
        {
            //Todo: different stores for specific storage types

            var docStoreInitializer = CreateDefaultDocumentStore(settings);
            
            if (docStoreInitializer == null)
            {
                throw new Exception($"Marten is configured as persistence for {typeof(TStorageType).Name} and no DocumentStore instance could be found.");
            }

            return docStoreInitializer;
        }

        private static IDocumentStore CreateDefaultDocumentStore(ReadOnlySettings settings)
        {
            //Todo: this should be a setting
            var connectionString = "host=localhost;database=nsb.persistence;username=postgres;password=admin";
            var documentStore = DocumentStore.For(_ =>
            {
                _.Connection(connectionString);

                _.Schema.For<SagaDocument>()
                        .Index(x => x.CorrelationProperty)
                        .Index(x => x.CorrelationPropertyValue);

                //Todo: Change to none
                _.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;
            });

            return documentStore;
        }
    }
}