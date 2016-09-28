using System.Collections.Generic;
using FakeItEasy;
using Marten;
using NServiceBus.Marten.Internal;
using NServiceBus.Persistence;
using NServiceBus.Settings;
using NUnit.Framework;

namespace NServiceBus.Marten.Tests.Persistence
{
    [TestFixture]
    public class DocumentStoreManagerTests
    {
        [Test]
        public void Should_set_specific_document_store()
        {
            var settings = new SettingsHolder();
            DocumentStoreManager.SetDocumentStore<StorageType.Sagas>(settings, FakeStore("Sagas"));
            DocumentStoreManager.SetDocumentStore<StorageType.Timeouts>(settings, FakeStore("Timeouts"));
            DocumentStoreManager.SetDefaultStore(settings, FakeStore("Default"));
            var readOnly = settings as ReadOnlySettings;

            Assert.AreEqual(_fakeStores["Sagas"], DocumentStoreManager.GetDocumentStore<StorageType.Sagas>(readOnly));
            Assert.AreEqual(_fakeStores["Timeouts"], DocumentStoreManager.GetDocumentStore<StorageType.Timeouts>(readOnly));
        }
        
        private readonly Dictionary<string, IDocumentStore> _fakeStores = new Dictionary<string, IDocumentStore>();

        private IDocumentStore FakeStore(string identifier)
        {
            var documentStore = A.Fake<IDocumentStore>();
            _fakeStores.Add(identifier, documentStore);
            return documentStore;
        }
    }
}