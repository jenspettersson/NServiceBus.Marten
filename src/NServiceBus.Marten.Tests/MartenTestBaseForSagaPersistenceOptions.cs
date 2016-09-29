using Marten;
using NServiceBus.Extensibility;

namespace NServiceBus.Marten.Tests
{
    static class MartenTestBaseForSagaPersistenceOptions
    {
        public static ContextBag CreateContextWithSessionPresent(this MartenPersistenceTestBase testBase, out IDocumentSession session)
        {
            var context = new ContextBag();
            session = testBase.OpenSession();
            context.Set(session);
            return context;
        }
    }
}