﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Marten;
using NServiceBus.Logging;
using NServiceBus.Marten.Sagas;
using NServiceBus.Marten.Timeouts;
using NServiceBus.Persistence;
using NServiceBus.Settings;

namespace NServiceBus.Marten.Internal
{
    public static class DocumentStoreManager
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(DocumentStoreManager));

        const string defaultDocStoreSettingsKey = "MartenDocumentStore";
        static Dictionary<Type, string> featureSettingsKeys;
        static Dictionary<Type, string> connStrKeys;

        static DocumentStoreManager()
        {
            featureSettingsKeys = new Dictionary<Type, string>
            {
                //{typeof(StorageType.GatewayDeduplication), "RavenDbDocumentStore/GatewayDeduplication"},
                //{typeof(StorageType.Subscriptions), "RavenDbDocumentStore/Subscription"},
                {typeof(StorageType.Outbox), "RavenDbDocumentStore/Outbox"},
                {typeof(StorageType.Sagas), "RavenDbDocumentStore/Saga"},
                {typeof(StorageType.Timeouts), "RavenDbDocumentStore/Timeouts"}
            };

            connStrKeys = new Dictionary<Type, string>()
            {
                //{typeof(StorageType.GatewayDeduplication), "NServiceBus/Persistence/Marten/GatewayDeduplication"},
                //{typeof(StorageType.Subscriptions), "NServiceBus/Persistence/Marten/Subscription"},
                {typeof(StorageType.Outbox), "NServiceBus/Persistence/Marten/Outbox"},
                {typeof(StorageType.Sagas), "NServiceBus/Persistence/Marten/Saga"},
                {typeof(StorageType.Timeouts), "NServiceBus/Persistence/Marten/Timeout"}
            };
        }

        public static void SetDocumentStore<TStorageType>(SettingsHolder settings, IDocumentStore documentStore)
        {
            if (documentStore == null)
            {
                throw new ArgumentNullException(nameof(documentStore));
            }
            SetDocumentStoreInternal(settings, typeof(TStorageType), s => documentStore);
        }

        public static void SetDocumentStore<TStorageType>(SettingsHolder settings, Func<ReadOnlySettings, IDocumentStore> storeCreator)
            where TStorageType : StorageType
        {
            if (storeCreator == null)
            {
                throw new ArgumentNullException(nameof(storeCreator));
            }
            SetDocumentStoreInternal(settings, typeof(TStorageType), storeCreator);
        }

        private static void SetDocumentStoreInternal(SettingsHolder settings, Type storageType, Func<ReadOnlySettings, IDocumentStore> storeCreator)
        {
            var initContext = new DocumentStoreInitializer(storeCreator);
            settings.Set(featureSettingsKeys[storageType], initContext);
        }

        public static void SetDefaultStore(SettingsHolder settings, IDocumentStore documentStore)
        {
            if (documentStore == null)
            {
                throw new ArgumentNullException(nameof(documentStore));
            }
            SetDefaultStore(settings, s => documentStore);
        }

        public static void SetDefaultStore(SettingsHolder settings, Func<ReadOnlySettings, IDocumentStore> storeCreator)
        {
            if (storeCreator == null)
            {
                throw new ArgumentNullException(nameof(storeCreator));
            }
            var initContext = new DocumentStoreInitializer(storeCreator);
            settings.Set(defaultDocStoreSettingsKey, initContext);
        }

        public static IDocumentStore GetDocumentStore<TStorageType>(ReadOnlySettings settings)
            where TStorageType : StorageType
        {
            return GetUninitializedDocumentStore<TStorageType>(settings).Init(settings);
        }

        internal static DocumentStoreInitializer GetUninitializedDocumentStore<TStorageType>(ReadOnlySettings settings)
            where TStorageType : StorageType
        {
            // First try to get a document store specific to a storage type (Subscriptions, Gateway, etc.)
            var docStoreInitializer = settings.GetOrDefault<DocumentStoreInitializer>(featureSettingsKeys[typeof(TStorageType)]);

            // Next, if a connection string name exists for the storage type, create based on that
            if (docStoreInitializer == null)
            {
                docStoreInitializer = CreateStoreByConnectionStringName(connStrKeys[typeof(TStorageType)]);
            }

            // Next try finding a shared DocumentStore
            if (docStoreInitializer == null)
            {
                docStoreInitializer = settings.GetOrDefault<DocumentStoreInitializer>(defaultDocStoreSettingsKey);
            }

            // Otherwise, we need to create it ourselves, but do so only once.
            if (docStoreInitializer == null)
            {
                // The holder is known to be non-null since we set it in SharedDocumentStore feature ctor
                var holder = settings.Get<SingleSharedDocumentStore>();
                if (holder.Initializer == null)
                {
                    holder.Initializer = CreateDefaultDocumentStore(settings);
                }

                docStoreInitializer = holder.Initializer;
            }

            if (docStoreInitializer == null)
            {
                throw new Exception($"Marten is configured as persistence for {typeof(TStorageType).Name} and no DocumentStore instance could be found.");
            }

            return docStoreInitializer;
        }

        private static DocumentStoreInitializer CreateDefaultDocumentStore(ReadOnlySettings settings)
        {
            var p = settings.GetOrDefault<ConnectionParameters>(MartenSettingsExtensions.DefaultConnectionParameters);
            if (p != null)
            {
                var storeByParams = DocumentStore.For(p.ConnectionString);
                
                return new DocumentStoreInitializer(storeByParams);
            }

            var initContext = CreateStoreByConnectionStringName("NServiceBus/Persistence/Marten", "NServiceBus/Persistence");

            if (initContext != null)
            {
                return initContext;
            }

            return null;
        }

        static DocumentStoreInitializer CreateStoreByConnectionStringName(params string[] connectionStringNames)
        {
            var connectionStringName = GetFirstNonEmptyConnectionString(connectionStringNames);
            if (!string.IsNullOrWhiteSpace(connectionStringName))
            {
                var docStore = DocumentStore.For(connectionStringName);

                return new DocumentStoreInitializer(docStore);
            }
            return null;
        }

        static string GetFirstNonEmptyConnectionString(params string[] connectionStringNames)
        {
            try
            {
                var foundConnectionStringNames = connectionStringNames.Where(name => ConfigurationManager.ConnectionStrings[name] != null).ToArray();
                var firstFound = foundConnectionStringNames.FirstOrDefault();
                
                if (foundConnectionStringNames.Length > 1)
                {
                    Logger.Warn($"Multiple possible RavenDB connection strings found. Using connection string `{firstFound}`.");
                }

                if (firstFound != null)
                {
                    return ConfigurationManager.ConnectionStrings[firstFound].ConnectionString;
                }

                return null;
            }
            catch (ConfigurationErrorsException)
            {
                return null;
            }
        }
    }
}