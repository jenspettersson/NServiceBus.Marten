using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using NServiceBus.Extensibility;
using NServiceBus.Marten.Timeouts;
using NServiceBus.Timeout.Core;
using NUnit.Framework;

namespace NServiceBus.Marten.Tests.Timeouts
{
    [TestFixture]
    public class When_removing_timeouts_from_storage : MartenPersistenceTestBase
    {
        [Test]
        public async Task Remove_WhenNoTimeoutRemoved_ShouldReturnFalse()
        {
            var persister = new TimeoutPersister(store);
            await persister.Add(new TimeoutData(), new ContextBag());

            var result = await persister.TryRemove(Guid.NewGuid().ToString(), new ContextBag());

            Assert.IsFalse(result);
        }

        [Test]
        public async Task Remove_WhenTimeoutRemoved_ShouldReturnTrue()
        {
            var persister = new TimeoutPersister(store);
            var timeoutData = new TimeoutData();
            await persister.Add(timeoutData, new ContextBag());

            var result = await persister.TryRemove(timeoutData.Id, new ContextBag());

            Assert.IsTrue(result);
        }
    }

    static class RandomProvider
    {
        static int seed = Environment.TickCount;

        static ThreadLocal<Random> randomWrapper = new ThreadLocal<Random>(() =>
            new Random(Interlocked.Increment(ref seed))
            );

        public static Random GetThreadRandom()
        {
            return randomWrapper.Value;
        }
    }
}
