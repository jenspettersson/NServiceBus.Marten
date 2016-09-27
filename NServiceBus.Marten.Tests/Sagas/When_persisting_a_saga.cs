using System;
using System.Threading.Tasks;
using FluentAssertions;
using Marten;
using NServiceBus.Extensibility;
using NServiceBus.Marten.Sagas;
using NServiceBus.Marten.SessionManagement;
using NServiceBus.Sagas;
using NUnit.Framework;

namespace NServiceBus.Marten.Tests.Sagas
{
    [TestFixture]
    public class When_persisting_a_saga : MartenPersistenceTestBase
    {
        [Test]
        public async Task Saga_should_be_persisted()
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

            
            await sagaPersister.Save(saga, new SagaCorrelationProperty("SomeId", saga.SomeId),  synchronizedStorageSession, context);
            await session.SaveChangesAsync().ConfigureAwait(false);

            var savedSaga = await sagaPersister.Get<SagaData>(saga.Id, synchronizedStorageSession, context);

            savedSaga.Id.Should().Be(saga.Id);
            savedSaga.SomeId.Should().Be(saga.SomeId);
            savedSaga.SomeName.Should().Be(saga.SomeName);
        }
    }

    class SagaData : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }

        public string SomeId { get; set; }
        public string SomeName { get; set; }
    }

    class SomeOtherSagaData : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }

        public string SomeId { get; set; }
        public string SomeName { get; set; }
    }
}