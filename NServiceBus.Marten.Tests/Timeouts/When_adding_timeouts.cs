using System;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Marten.Timeouts;
using NServiceBus.Timeout.Core;
using NUnit.Framework;

namespace NServiceBus.Marten.Tests.Timeouts
{
    [TestFixture]
    public class When_adding_timeouts : MartenPersistenceTestBase
    {
        [Test]
        public async Task Add_WhenNoIdProvided_ShouldSetDbGeneratedTimeoutId()
        {
            var persister = new TimeoutPersister(store);
            var timeout = new TimeoutData { Id = null };

            await persister.Add(timeout, new ContextBag());
            Assert.IsNotNull(timeout.Id);

            var result = await persister.Peek(timeout.Id, new ContextBag());
            Assert.IsNotNull(result);
        }

        [Test]
        public async Task Add_WhenIdProvided_ShouldOverrideGivenId()
        {
            var persister = new TimeoutPersister(store);

            var timeoutId = Guid.NewGuid().ToString();
            var timeout = new TimeoutData { Id = timeoutId };

            await persister.Add(timeout, new ContextBag());
            Assert.AreNotEqual(timeoutId, timeout.Id);

            var result = await persister.Peek(timeoutId, new ContextBag());
            Assert.IsNull(result);
        }
    }
}