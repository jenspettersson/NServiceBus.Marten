using System.Threading.Tasks;
using Marten;
using NServiceBus.Persistence;

namespace NServiceBus.Marten.SessionManagement
{
    internal class MartenSynchronizedStorageSession : CompletableSynchronizedStorageSession
    {
        private readonly bool _ownsSession;
        public IDocumentSession Session { get; }

        public MartenSynchronizedStorageSession(IDocumentSession session, bool ownsSession)
        {
            _ownsSession = ownsSession;
            Session = session;
        }

        public void Dispose()
        {
            if (_ownsSession)
            {
                Session.Dispose();
            }
        }

        public async Task CompleteAsync()
        {
            if (_ownsSession)
            {
                await Session.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }
}