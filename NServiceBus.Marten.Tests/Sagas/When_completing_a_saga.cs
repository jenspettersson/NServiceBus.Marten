using System;
using System.Threading.Tasks;
using FluentAssertions;
using Marten;
using NServiceBus.Marten.Sagas;
using NServiceBus.Marten.SessionManagement;
using NServiceBus.Sagas;
using NUnit.Framework;

namespace NServiceBus.Marten.Tests.Sagas
{
    [TestFixture]
    public class When_completing_a_saga : MartenPersistenceTestBase
    {
        [Test]
        public async Task The_saga_data_should_be_removed()
        {
            var saga = new SagaData
            {
                Id = Guid.NewGuid(),
                SomeId = "test-id",
                SomeName = "Test Name"
            };

            IDocumentSession session;
            var context = this.CreateContextWithSessionPresent(out session);

            var sagaPersister = new SagaPersister();
            var synchronizedStorageSession = new MartenSynchronizedStorageSession(session, true);
            
            await sagaPersister.Save(saga, new SagaCorrelationProperty("SomeId", saga.SomeId), synchronizedStorageSession, context);
            await session.SaveChangesAsync().ConfigureAwait(false);

            await sagaPersister.Complete(saga, synchronizedStorageSession, context);
            await session.SaveChangesAsync().ConfigureAwait(false);

            var savedSaga = await sagaPersister.Get<SagaData>(saga.Id, synchronizedStorageSession, context);
            savedSaga.Should().BeNull();
        }
    }
}