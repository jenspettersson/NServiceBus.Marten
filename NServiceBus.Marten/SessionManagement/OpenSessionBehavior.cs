using System;
using System.Threading.Tasks;
using Marten;
using NServiceBus.Pipeline;

namespace NServiceBus.Marten.SessionManagement
{
    class OpenSessionBehavior : Behavior<IIncomingPhysicalMessageContext>
    {
        private readonly IDocumentStore _documentStore;

        public OpenSessionBehavior(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            using (var session = OpenSession(context))
            {
                context.Extensions.Set(session);
                await next().ConfigureAwait(false);
            }
        }

        private IDocumentSession OpenSession(IIncomingPhysicalMessageContext context)
        {
            //Todo: inject settings
            //Todo: use dirty tracked or lightweight with explicit updates?
            return _documentStore.DirtyTrackedSession();
        }
    }
}
