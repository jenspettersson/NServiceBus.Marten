using System;
using Marten;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Marten.Internal;
using NServiceBus.Persistence;
using NServiceBus.Settings;

namespace NServiceBus.Marten.Timeouts
{
    public static class MartenTimeoutSettingsExtensions
    {
        /// <summary>
        ///     Configures the given document store to be used when storing timeouts
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="documentStore">The document store to use</param>
        public static PersistenceExtensions<MartenPersistence> UseDocumentStoreForTimeouts(this PersistenceExtensions<MartenPersistence> cfg, IDocumentStore documentStore)
        {
            DocumentStoreManager.SetDocumentStore<StorageType.Timeouts>(cfg.GetSettings(), documentStore);
            return cfg;
        }

        /// <summary>
        ///     Configures the given document store to be used when storing timeouts
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="storeCreator">A Func that will create the document store on NServiceBus initialization.</param>
        public static PersistenceExtensions<MartenPersistence> UseDocumentStoreForTimeouts(this PersistenceExtensions<MartenPersistence> cfg, Func<ReadOnlySettings, IDocumentStore> storeCreator)
        {
            DocumentStoreManager.SetDocumentStore<StorageType.Timeouts>(cfg.GetSettings(), storeCreator);
            return cfg;
        }
    }
}