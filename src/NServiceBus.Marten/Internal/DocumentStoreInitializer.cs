using System;
using Marten;
using NServiceBus.Settings;

namespace NServiceBus.Marten.Internal
{
    class DocumentStoreInitializer
    {
        private Func<ReadOnlySettings, IDocumentStore> storeCreator;
        IDocumentStore docStore;
        bool isInitialized;

        internal DocumentStoreInitializer(Func<ReadOnlySettings, IDocumentStore> storeCreator)
        {
            this.storeCreator = storeCreator;
        }

        internal DocumentStoreInitializer(IDocumentStore store)
        {
            this.storeCreator = readOnlySettings => store;
        }

        internal IDocumentStore Init(ReadOnlySettings settings)
        {
            if (!isInitialized)
            {
                EnsureDocStoreCreated(settings);
                ApplyConventions(settings);
            }
            isInitialized = true;
            return docStore;
        }

        void ApplyConventions(ReadOnlySettings settings)
        {
            //Todo: Apply specific schema settings for features here?
        }

        internal void EnsureDocStoreCreated(ReadOnlySettings settings)
        {
            if (docStore == null)
            {
                docStore = storeCreator(settings);
            }
        }
    }
}