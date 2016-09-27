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
    public class When_fetching_a_saga_by_correlation_property : MartenPersistenceTestBase
    {
        [Test]
        public async Task Correct_saga_should_be_returned()
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
            
            var correlationProperty = new SagaCorrelationProperty("SomeId", saga.SomeId);
            await sagaPersister.Save(saga, correlationProperty, synchronizedStorageSession, context);
            await session.SaveChangesAsync().ConfigureAwait(false);

            var savedSaga = await sagaPersister.Get<SagaData>(correlationProperty.Name, correlationProperty.Value, synchronizedStorageSession, context);

            savedSaga.Id.Should().Be(saga.Id);
            savedSaga.SomeId.Should().Be(saga.SomeId);
            savedSaga.SomeName.Should().Be(saga.SomeName);
        }

        [Test]
        public async Task Should_query_by_saga_type()
        {
            var otherSaga = new SomeOtherSagaData
            {
                Id = Guid.NewGuid(),
                SomeId = "test-id",
                SomeName = "Test Name"
            };

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

            var correlationProperty = new SagaCorrelationProperty("SomeId", saga.SomeId);
            await sagaPersister.Save(otherSaga, correlationProperty, synchronizedStorageSession, context);
            await sagaPersister.Save(saga, correlationProperty, synchronizedStorageSession, context);
            await session.SaveChangesAsync().ConfigureAwait(false);

            var savedSaga = await sagaPersister.Get<SagaData>(correlationProperty.Name, correlationProperty.Value, synchronizedStorageSession, context);

            savedSaga.Id.Should().Be(saga.Id);
        }
    }
}