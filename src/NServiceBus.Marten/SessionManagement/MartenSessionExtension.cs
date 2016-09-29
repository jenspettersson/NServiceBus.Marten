using System;
using Marten;
using NServiceBus.Persistence;

namespace NServiceBus.Marten.SessionManagement
{
    public static class MartenSessionExtension
    {
        public static IDocumentSession MartenSession(this SynchronizedStorageSession session)
        {
            var synchronizedStorageSession = session as MartenSynchronizedStorageSession;
            if (synchronizedStorageSession != null)
            {
                return synchronizedStorageSession.Session;
            }

            throw new InvalidOperationException("It was not possible to retrieve a Marten session.");
        }
        
    }
}