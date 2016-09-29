using System;
using Marten;
using NServiceBus.Extensibility;

namespace NServiceBus.Marten
{
    static class ContextBagExtensions
    {
        internal static IDocumentSession GetSession(this ContextBag contextBag)
        {
            IDocumentSession session;
            if (contextBag.TryGet(out session))
            {
                return session;
            }

            throw new Exception("IDocumentSession could not be retrieved for the incoming message pipeline.");
        }
    }
}