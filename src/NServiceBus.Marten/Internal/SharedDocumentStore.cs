using NServiceBus.Features;

namespace NServiceBus.Marten.Internal
{
    public class SharedDocumentStore : Feature
    {
        public SharedDocumentStore()
        {
            Defaults(_ => _.Set<SingleSharedDocumentStore>(new SingleSharedDocumentStore()));
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            
        }
    }
}