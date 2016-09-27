using NServiceBus.Features;
using NServiceBus.Marten.Sagas;
using NServiceBus.Marten.SessionManagement;
using NServiceBus.Persistence;

namespace NServiceBus.Marten
{
    public class MartenPersistence : PersistenceDefinition
    {
        public MartenPersistence()
        {
            Defaults(s =>
            {
                s.EnableFeatureByDefault<MartenStorageSession>();
            });

            Supports<StorageType.Sagas>(s => s.EnableFeatureByDefault<MartenSagaStorage>());
        }
    }
}
