using System.Threading.Tasks;
using NServiceBus.Features;
using NServiceBus.Marten.Internal;
using NServiceBus.Persistence;

namespace NServiceBus.Marten.Timeouts
{
    public class MartenTimoutStorage : Feature
    {
        public MartenTimoutStorage()
        {
            DependsOn<TimeoutManager>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var store = DocumentStoreManager.GetDocumentStore<StorageType.Timeouts>(context.Settings);

            context.Container.ConfigureComponent(() => new TimeoutPersister(store), DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent(() => new QueryTimeouts(store, context.Settings.EndpointName()), DependencyLifecycle.SingleInstance);  // Needs to be SingleInstance because it contains cleanup state

            context.Container.ConfigureComponent<QueryCanceller>(DependencyLifecycle.InstancePerCall);
            context.RegisterStartupTask(b => b.Build<QueryCanceller>());
        }

        class QueryCanceller : FeatureStartupTask
        {
            public QueryCanceller(QueryTimeouts queryTimeouts)
            {
                _queryTimeouts = queryTimeouts;
            }

            protected override Task OnStart(IMessageSession session)
            {
                return Task.FromResult(0);
            }

            protected override Task OnStop(IMessageSession session)
            {
                _queryTimeouts.Shutdown();
                return Task.FromResult(0);
            }

            QueryTimeouts _queryTimeouts;
        }
    }
}