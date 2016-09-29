using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;

namespace NServiceBus.Marten.SessionManagement
{
    class MartenSynchronizedStorage : ISynchronizedStorage
    {
        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
        {
            var session = contextBag.GetSession();
            var synchronizedStorageSession = new MartenSynchronizedStorageSession(session, true);
            return Task.FromResult((CompletableSynchronizedStorageSession) synchronizedStorageSession);
        }
    }
}