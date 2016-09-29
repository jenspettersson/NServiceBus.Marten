using System.Threading.Tasks;
using System.Transactions;
using NServiceBus.Extensibility;
using NServiceBus.Marten.Outbox;
using NServiceBus.Outbox;
using NServiceBus.Persistence;
using NServiceBus.Transport;

namespace NServiceBus.Marten.SessionManagement
{
    class MartenSynchronizedStorageAdapter  : ISynchronizedStorageAdapter
    {
        static readonly Task<CompletableSynchronizedStorageSession> EmptyResult = Task.FromResult((CompletableSynchronizedStorageSession)null);

        public Task<CompletableSynchronizedStorageSession> TryAdapt(OutboxTransaction transaction, ContextBag context)
        {
            var martenTransaction = transaction as MartenOutboxTransaction;
            if (martenTransaction != null)
            {
                CompletableSynchronizedStorageSession session = new MartenSynchronizedStorageSession(martenTransaction.Session, false);
                return Task.FromResult(session);
            }

            return EmptyResult;
        }

        public Task<CompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context)
        {
            Transaction ambientTransaction;
            if (transportTransaction.TryGet(out ambientTransaction))
            {
                var session = context.GetSession();
                CompletableSynchronizedStorageSession completableSynchronizedStorageSession = new MartenSynchronizedStorageSession(session, true);
                return Task.FromResult(completableSynchronizedStorageSession);
            }

            return EmptyResult;
        }
    }
}