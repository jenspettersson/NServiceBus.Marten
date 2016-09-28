using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Marten.Timeouts;
using NServiceBus.Support;
using NServiceBus.Timeout.Core;
using NUnit.Framework;

namespace NServiceBus.Marten.Tests.Timeouts
{
    [TestFixture]
    public class When_fetching_timeouts_from_storage : MartenPersistenceTestBase
    {
        private TimeoutPersister _persister;
        private QueryTimeouts _query;

        public override void SetUp()
        {
            base.SetUp();

            _persister = new TimeoutPersister(store);
            _query = new QueryTimeouts(store, "MyTestEndpoint");
        }

        [Test]
        public async Task Should_return_the_complete_list_of_timeouts()
        {
            const int numberOfTimeoutsToAdd = 10;
            var context = new ContextBag();
            for (var i = 0; i < numberOfTimeoutsToAdd; i++)
            {
                await _persister.Add(new TimeoutData
                {
                    Time = DateTime.UtcNow.AddHours(-1),
                    Destination = "timeouts@" + RuntimeEnvironment.MachineName,
                    SagaId = Guid.NewGuid(),
                    State = new byte[]
                    {
                        0,
                        0,
                        133
                    },
                    Headers = new Dictionary<string, string>
                    {
                        {"Bar", "34234"},
                        {"Foo", "aString1"},
                        {"Super", "aString2"}
                    },
                    OwningTimeoutManager = "MyTestEndpoint"
                }, context);
            }

            Assert.AreEqual(numberOfTimeoutsToAdd, (await _query.GetNextChunk(DateTime.UtcNow.AddYears(-3))).DueTimeouts.Length);
        }

        [Test]
        public async Task Should_return_the_next_time_of_retrieval()
        {
            _query.CleanupGapFromTimeslice = TimeSpan.FromSeconds(1);
            _query.TriggerCleanupEvery = TimeSpan.Zero;

            var nextTime = DateTime.UtcNow.AddHours(1);
            var context = new ContextBag();

            await _persister.Add(new TimeoutData
            {
                Time = nextTime,
                Destination = "timeouts@" + RuntimeEnvironment.MachineName,
                SagaId = Guid.NewGuid(),
                State = new byte[]
                {
                    0,
                    0,
                    133
                },
                Headers = new Dictionary<string, string>
                {
                    {"Bar", "34234"},
                    {"Foo", "aString1"},
                    {"Super", "aString2"}
                },
                OwningTimeoutManager = "MyTestEndpoint"
            }, context);
            
            var nextTimeToRunQuery = (await _query.GetNextChunk(DateTime.UtcNow.AddYears(-3))).NextTimeToQuery;

            Assert.IsTrue((nextTime - nextTimeToRunQuery).TotalSeconds < 1);
        }
    }
}