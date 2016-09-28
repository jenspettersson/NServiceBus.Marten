using System;
using System.Linq;
using System.Threading.Tasks;
using Marten;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;

namespace NServiceBus.Marten.Outbox
{
    class OutboxPersister : IOutboxStorage
    {
        private readonly IDocumentStore _documentStore;
        private readonly string _endpointName;
        TransportOperation[] emptyTransportOperations = new TransportOperation[0];

        public OutboxPersister(IDocumentStore documentStore, string endpointName)
        {
            _documentStore = documentStore;
            _endpointName = endpointName;
        }

        public async Task<OutboxMessage> Get(string messageId, ContextBag context)
        {
            OutboxRecord result;

            using (var session = _documentStore.DirtyTrackedSession())
            {
                var possibleIds = GetPossibleOutboxDocumentIds(messageId);
                var docs = await session.LoadManyAsync<OutboxRecord>(possibleIds).ConfigureAwait(false);
                result = docs.FirstOrDefault(o => o != null);
            }

            if (result == null)
            {
                return default(OutboxMessage);
            }

            if (result.Dispatched || result.TransportOperations.Length == 0)
            {
                return new OutboxMessage(result.MessageId, emptyTransportOperations);
            }

            var transportOperations = new TransportOperation[result.TransportOperations.Length];
            var index = 0;
            foreach (var op in result.TransportOperations)
            {
                transportOperations[index] = new TransportOperation(op.MessageId, op.Options, op.Message, op.Headers);
                index++;
            }

            return new OutboxMessage(result.MessageId, transportOperations);
        }

        public Task Store(OutboxMessage message, OutboxTransaction transaction, ContextBag context)
        {
            var session = ((MartenOutboxTransaction)transaction).Session;

            var operations = new OutboxRecord.OutboxOperation[message.TransportOperations.Length];

            var index = 0;
            foreach (var transportOperation in message.TransportOperations)
            {
                operations[index] = new OutboxRecord.OutboxOperation
                {
                    Message = transportOperation.Body,
                    Headers = transportOperation.Headers,
                    MessageId = transportOperation.MessageId,
                    Options = transportOperation.Options
                };
                index++;
            }

            session.Store(new OutboxRecord
            {
                Id = GetOutboxRecordId(message.MessageId),
                MessageId = message.MessageId,
                Dispatched = false,
                TransportOperations = operations
            });

            return Task.FromResult(0);
        }

        public async Task SetAsDispatched(string messageId, ContextBag context)
        {
            using (var session = _documentStore.DirtyTrackedSession())
            {
                //Note: Marten doesn't have an equivivalent to session.Advanced.UseOptimisticConcurrency = true;
                //will set it on the schema level be enough?

                var docs = await session.LoadManyAsync<OutboxRecord>(GetPossibleOutboxDocumentIds(messageId)).ConfigureAwait(false);
                var outboxMessage = docs.FirstOrDefault(o => o != null);
                if (outboxMessage == null || outboxMessage.Dispatched)
                {
                    return;
                }

                outboxMessage.Dispatched = true;
                outboxMessage.DispatchedAt = DateTime.UtcNow;

                await session.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public Task<OutboxTransaction> BeginTransaction(ContextBag context)
        {
            var session = _documentStore.DirtyTrackedSession();

            //Note: Marten doesn't have an equivivalent to session.Advanced.UseOptimisticConcurrency = true;
            //will set it on the schema level be enough?

            context.Set(session);
            var transaction = new MartenOutboxTransaction(session);
            return Task.FromResult<OutboxTransaction>(transaction);
        }

        string[] GetPossibleOutboxDocumentIds(string messageId)
        {
            return new[]
            {
                GetOutboxRecordId(messageId),
                $"Outbox/{messageId}"
            };
        }

        string GetOutboxRecordId(string messageId) => $"Outbox/{_endpointName}/{messageId}";
    }
}