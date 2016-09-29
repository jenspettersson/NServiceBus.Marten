using System;
using Marten;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Marten.Internal;
using NServiceBus.Persistence;
using NServiceBus.Settings;

namespace NServiceBus.Marten.Sagas
{
    public static class MartenSagaSettingsExtensions
    {
        /// <summary>
        ///     Configures the given document store to be used when storing sagas
        /// </summary>
        /// <param name="cfg">Object to attach to</param>
        /// <param name="documentStore">The document store to be used</param>
        public static PersistenceExtensions<MartenPersistence> UseDocumentStoreForSagas(this PersistenceExtensions<MartenPersistence> cfg, IDocumentStore documentStore)
        {
            DocumentStoreManager.SetDocumentStore<StorageType.Sagas>(cfg.GetSettings(), documentStore);
            return cfg;
        }

        /// <summary>
        ///     Configures the given document store to be used when storing sagas
        /// </summary>
        /// <param name="cfg">Object to attach to</param>
        /// <param name="storeCreator">A Func that will create the document store on NServiceBus initialization.</param>
        public static PersistenceExtensions<MartenPersistence> UseDocumentStoreForSagas(this PersistenceExtensions<MartenPersistence> cfg, Func<ReadOnlySettings, IDocumentStore> storeCreator)
        {
            DocumentStoreManager.SetDocumentStore<StorageType.Sagas>(cfg.GetSettings(), storeCreator);
            return cfg;
        }
    }
}