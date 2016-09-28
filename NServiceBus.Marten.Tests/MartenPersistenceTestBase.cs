using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Marten;
using NServiceBus.Marten.Timeouts;
using NUnit.Framework;

namespace NServiceBus.Marten.Tests
{
    public class MartenPersistenceTestBase
    {
        private List<IDocumentSession> _sessions = new List<IDocumentSession>();

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            store = DocumentStore.For(_ =>
            {
                _.Connection("host=localhost;database=marten.nservicebus.tests;username=postgres;password=admin");
                _.AutoCreateSchemaObjects = AutoCreate.All;

                _.Schema.For<TimeoutDocument>().UseOptimisticConcurrency(true);
            });
        }

        [SetUp]
        public virtual void SetUp()
        {
            store.Advanced.Clean.CompletelyRemoveAll();
        }

        [TearDown]
        public void TearDown()
        {
            _sessions.ForEach(s => s.Dispose());
            _sessions.Clear();
        }

        protected internal IDocumentSession OpenSession()
        {
            var documentSession = store.OpenSession();
            
            _sessions.Add(documentSession);
            return documentSession;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            store.Dispose();
        }

        protected static Task<Exception> Catch(Func<Task> action)
        {
            return Catch<Exception>(action);
        }

        /// <summary>
        ///     This helper is necessary because RavenTestBase doesn't like Assert.Throws, Assert.That... with async void methods.
        /// </summary>
        protected static async Task<TException> Catch<TException>(Func<Task> action) where TException : Exception
        {
            try
            {
                await action();
                return default(TException);
            }
            catch (TException ex)
            {
                return ex;
            }
        }

        protected IDocumentStore store;
    }
}