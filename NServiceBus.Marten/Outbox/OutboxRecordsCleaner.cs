using System;
using System.Threading;
using System.Threading.Tasks;
using Marten;

namespace NServiceBus.Marten.Outbox
{
    class OutboxRecordsCleaner
    {
        readonly IDocumentStore _documentStore;

        public OutboxRecordsCleaner(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public async Task RemoveEntriesOlderThan(DateTime dateTime, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var session = _documentStore.OpenSession())
            {
                session.DeleteWhere<OutboxRecord>(x => x.Dispatched && x.DispatchedAt < dateTime);
                await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}