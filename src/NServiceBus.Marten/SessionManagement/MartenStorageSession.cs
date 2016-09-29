using Marten;
using NServiceBus.Features;
using NServiceBus.Marten.Internal;
using NServiceBus.Persistence;

namespace NServiceBus.Marten.SessionManagement
{
    public class MartenStorageSession : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<MartenSynchronizedStorageAdapter>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<MartenSynchronizedStorage>(DependencyLifecycle.SingleInstance);

            IDocumentStore store = DocumentStoreManager.GetDocumentStore<StorageType.Sagas>(context.Settings);

            //Todo: Add Provided Session Behavior?

            //Todo: make sure this behavior is executed AFTER correct step in pipeline...
            context.Pipeline.Register("OpenMartenSession", new OpenSessionBehavior(store), "Makes sure that there is a Marten IDocumentSession available on the pipeline");
        }
    }
}