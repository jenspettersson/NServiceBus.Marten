using NServiceBus.Features;

namespace NServiceBus.Marten.Sagas
{
    public class MartenSagaStorage : Feature
    {
        public MartenSagaStorage()
        {
            DependsOn<Features.Sagas>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<SagaPersister>(DependencyLifecycle.SingleInstance);
        }
    }
}