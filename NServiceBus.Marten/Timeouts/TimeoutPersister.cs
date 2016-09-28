using System;
using System.Threading.Tasks;
using Marten;
using Marten.Services;
using NServiceBus.Extensibility;
using NServiceBus.Timeout.Core;

namespace NServiceBus.Marten.Timeouts
{
    public class TimeoutPersister : IPersistTimeouts
    {
        private readonly IDocumentStore _documentStore;

        public TimeoutPersister(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public async Task Add(TimeoutData timeout, ContextBag context)
        {
            using (var session = _documentStore.OpenSession())
            {
                var timeoutData = new TimeoutDocument(Guid.NewGuid().ToString("N"), timeout);
                session.Store(timeoutData);
                await session.SaveChangesAsync().ConfigureAwait(false);
                timeout.Id = timeoutData.Id;
            }
        }

        public async Task<bool> TryRemove(string timeoutId, ContextBag context)
        {
            using (var session = _documentStore.OpenSession())
            {
                var timeout = await session.LoadAsync<TimeoutDocument>(timeoutId).ConfigureAwait(false);
                if (timeout == null)
                {
                    return false;
                }

                session.Delete(timeout);

                try
                {
                    await session.SaveChangesAsync().ConfigureAwait(false);
                }
                catch (Exception)
                {
                    return false;
                }

                return true;
            }
        }

        public async Task<Timeout.Core.TimeoutData> Peek(string timeoutId, ContextBag context)
        {
            using (var session = _documentStore.OpenSession())
            {
                var timeoutData = await session.LoadAsync<TimeoutDocument>(timeoutId).ConfigureAwait(false);

                return timeoutData?.ToCoreTimeoutData();
            }
        }

        public async Task RemoveTimeoutBy(Guid sagaId, ContextBag context)
        {
            using (var session = _documentStore.OpenSession())
            {
                session.DeleteWhere<TimeoutDocument>(x => x.SagaId == sagaId);
                await session.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }
}