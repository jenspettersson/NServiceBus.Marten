using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Marten.Services;
using NServiceBus.Extensibility;
using NServiceBus.Marten.Outbox;
using NServiceBus.Outbox;
using NUnit.Framework;

namespace NServiceBus.Marten.Tests.Outbox
{
    [TestFixture]
    public class When_adding_outbox_messages : MartenPersistenceTestBase
    {
        string testEndpointName = "TestEndpoint";

        [Test]
        public async Task Should_throw_if_trying_to_insert_same_messageid_concurrently()
        {
            var persister = new OutboxPersister(store, testEndpointName);

            var exception = await Catch<Exception>(async () =>
            {
                using (var transaction = await persister.BeginTransaction(new ContextBag()))
                {
                    await persister.Store(new OutboxMessage("MySpecialId", new TransportOperation[0]), transaction, new ContextBag());
                    await persister.Store(new OutboxMessage("MySpecialId", new TransportOperation[0]), transaction, new ContextBag());
                    await transaction.Commit();
                }
            });

            Assert.NotNull(exception);
        }

        [Test]
        public async Task Should_throw_if__trying_to_insert_same_messageid()
        {
            var persister = new OutboxPersister(store, testEndpointName);

            using (var transaction = await persister.BeginTransaction(new ContextBag()))
            {
                await persister.Store(new OutboxMessage("MySpecialId", new TransportOperation[0]), transaction, new ContextBag());

                await transaction.Commit();
            }

            var exception = await Catch<Exception>(async () =>
            {
                using (var transaction = await persister.BeginTransaction(new ContextBag()))
                {
                    await persister.Store(new OutboxMessage("MySpecialId", new TransportOperation[0]), transaction, new ContextBag());

                    await transaction.Commit();
                }
            });
            Assert.NotNull(exception);
        }

        [Test]
        public async Task Should_save_with_not_dispatched()
        {
            var persister = new OutboxPersister(store, testEndpointName);

            var id = Guid.NewGuid().ToString("N");
            var message = new OutboxMessage(id, new[]
            {
                new TransportOperation(id, new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>())
            });

            using (var transaction = await persister.BeginTransaction(new ContextBag()))
            {
                await persister.Store(message, transaction, new ContextBag());

                await transaction.Commit();
            }

            var result = await persister.Get(id, new ContextBag());

            var operation = result.TransportOperations.Single();

            Assert.AreEqual(id, operation.MessageId);
        }

        [Test]
        public async Task Should_update_dispatched_flag()
        {
            var persister = new OutboxPersister(store, testEndpointName);

            var id = Guid.NewGuid().ToString("N");
            var message = new OutboxMessage(id, new[]
            {
                new TransportOperation(id, new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>())
            });

            using (var transaction = await persister.BeginTransaction(new ContextBag()))
            {
                await persister.Store(message, transaction, new ContextBag());

                await transaction.Commit();
            }
            await persister.SetAsDispatched(id, new ContextBag());
            
            using (var s = store.OpenSession())
            {
                var result = s.Query<OutboxRecord>()
                    .SingleOrDefault(o => o.MessageId == id);

                Assert.NotNull(result);
                Assert.True(result.Dispatched);
            }
        }

        [TestCase("Outbox/")]
        [TestCase("Outbox/TestEndpoint/")]
        public async Task Should_get_messages_with_old_and_new_recordId_format(string outboxRecordIdPrefix)
        {
            var persister = new OutboxPersister(store, testEndpointName);

            var messageId = Guid.NewGuid().ToString();

            //manually store an OutboxRecord to control the OutboxRecordId format
            using (var session = OpenSession())
            {
                var fullDocumentId = outboxRecordIdPrefix + messageId;
                var newRecord = new OutboxRecord
                {
                    Id = fullDocumentId,
                    MessageId = messageId,
                    Dispatched = false,
                    TransportOperations = new[]
                    {
                        new OutboxRecord.OutboxOperation
                        {
                            Message = new byte[1024*5],
                            Headers = new Dictionary<string, string>(),
                            MessageId = messageId,
                            Options = new Dictionary<string, string>()
                        }
                    }
                };
                session.Store(newRecord);

                await session.SaveChangesAsync();
            }

            var result = await persister.Get(messageId, new ContextBag());

            Assert.NotNull(result);
            Assert.AreEqual(messageId, result.MessageId);
        }

        [TestCase("Outbox/")]
        [TestCase("Outbox/TestEndpoint/")]
        public async Task Should_set_messages_as_dispatched_with_old_and_new_recordId_format(string outboxRecordIdPrefix)
        {
            var persister = new OutboxPersister(store, testEndpointName);

            var messageId = Guid.NewGuid().ToString();

            //manually store an OutboxRecord to control the OutboxRecordId format
            using (var session = OpenSession())
            {
                session.Store(new OutboxRecord
                {
                    Id = outboxRecordIdPrefix + messageId,
                    MessageId = messageId,
                    Dispatched = false,
                    TransportOperations = new[]
                    {
                        new OutboxRecord.OutboxOperation
                        {
                            Message = new byte[1024*5],
                            Headers = new Dictionary<string, string>(),
                            MessageId = messageId,
                            Options = new Dictionary<string, string>()
                        }
                    }
                });

                await session.SaveChangesAsync();
            }

            await persister.SetAsDispatched(messageId, new ContextBag());

            using (var session = OpenSession())
            {
                var result = await session.LoadAsync<OutboxRecord>(outboxRecordIdPrefix + messageId);

                Assert.NotNull(result);
                Assert.True(result.Dispatched);
            }

        }
    }
}