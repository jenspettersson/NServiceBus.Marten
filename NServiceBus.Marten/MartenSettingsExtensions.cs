using System;
using Marten;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Marten.Internal;
using NServiceBus.Settings;

namespace NServiceBus.Marten
{
    public static class MartenSettingsExtensions
    {
        internal const string DefaultConnectionParameters = "RavenDbConnectionParameters";

        /// <summary>
        ///     Configures the storages to use the given document store supplied
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="documentStore">Document store managed by me as a user</param>
        /// <returns></returns>
        public static PersistenceExtensions<MartenPersistence> SetDefaultDocumentStore(this PersistenceExtensions<MartenPersistence> cfg, IDocumentStore documentStore)
        {
            DocumentStoreManager.SetDefaultStore(cfg.GetSettings(), documentStore);
            return cfg;
        }

        /// <summary>
        ///     Configures the storages to use the given document store supplied
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="storeCreator">A Func that will create the document store on NServiceBus initialization.</param>
        /// <returns></returns>
        public static PersistenceExtensions<MartenPersistence> SetDefaultDocumentStore(this PersistenceExtensions<MartenPersistence> cfg, Func<ReadOnlySettings, IDocumentStore> storeCreator)
        {
            DocumentStoreManager.SetDefaultStore(cfg.GetSettings(), storeCreator);
            return cfg;
        }

        /// <summary>
        ///     Configures the persisters to connection to the server specified
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="connectionParameters">Connection details</param>
        /// <returns></returns>
        public static PersistenceExtensions<MartenPersistence> SetDefaultDocumentStore(this PersistenceExtensions<MartenPersistence> cfg, ConnectionParameters connectionParameters)
        {
            if (connectionParameters == null)
            {
                throw new ArgumentNullException(nameof(connectionParameters));
            }
            cfg.GetSettings().Set(DefaultConnectionParameters, connectionParameters);
            // This will be registered with RavenUserInstaller once we initialize the document store object internally
            return cfg;
        }
    }
}