using System.Threading.Tasks;
using Marten;
using NServiceBus.Outbox;

namespace NServiceBus.Marten.SessionManagement
{
    class MartenOutboxTransaction : OutboxTransaction
    {
        public IDocumentSession Session { get; private set; }

        public MartenOutboxTransaction(IDocumentSession session)
        {
            Session = session;
        }

        public void Dispose()
        {
            Session.Dispose();
            Session = null;
        }

        public Task Commit()
        {
            return Session.SaveChangesAsync();
        }
    }
}