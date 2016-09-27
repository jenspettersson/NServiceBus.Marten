using System;
using System.Linq;
using System.Threading.Tasks;
using Marten;
using NServiceBus.Extensibility;
using NServiceBus.Marten.SessionManagement;
using NServiceBus.Persistence;
using NServiceBus.Sagas;

namespace NServiceBus.Marten.Sagas
{
    public class SagaPersister : ISagaPersister
    {
        public async Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
        {
            var documentSession = session.MartenSession();

            if (sagaData == null)
            {
                return;
            }
            
            var sagaDocument = new SagaDocument {Id = sagaData.Id, SagaData = sagaData, CorrelationProperty = correlationProperty.Name, CorrelationPropertyValue = correlationProperty.Value.ToString()};

            documentSession.Store(sagaDocument);
        }

        public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            //As we currently use _documentStore.DirtyTrackedSession(); we don't need to do anything here
            return Task.FromResult(0);
        }

        public async Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            var documentSession = session.MartenSession();
            var sagaDocument = await documentSession.LoadAsync<SagaDocument>(sagaId);
            if (sagaDocument == null)
                return default(TSagaData);

            return (TSagaData) sagaDocument.SagaData;
        }

        public async Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            var documentSession = session.MartenSession();

            var sagaDocument = await documentSession.Query<SagaDocument>()
                .Where(x => x.CorrelationProperty == propertyName)
                .Where(x => x.CorrelationPropertyValue == propertyValue.ToString())
                .FirstOrDefaultAsync();

            if (sagaDocument == null)
                return default(TSagaData);

            return (TSagaData) sagaDocument.SagaData;
        }

        public Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var documentSession = session.MartenSession();
            
            documentSession.Delete<SagaDocument>(sagaData.Id);
            
            return Task.CompletedTask;
        }
    }
}