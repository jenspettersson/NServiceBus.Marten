using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;

namespace LabConsole
{
    public class ImportAccommodationSaga : Saga<ImportAccommodationSagaData>,
        IAmStartedByMessages<AccommodationProductImported>,
        IHandleMessages<AccommodationCostImported>
    {
        private static readonly ILog _log = LogManager.GetLogger<ImportAccommodationSaga>();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ImportAccommodationSagaData> mapper)
        {
            mapper.ConfigureMapping<AccommodationProductImported>(message => message.Id)
                .ToSaga(sagaData => sagaData.AccommodationProductId);

            mapper.ConfigureMapping<AccommodationCostImported>(message => message.ProductId)
                .ToSaga(sagaData => sagaData.AccommodationProductId);
        }

        public async Task Handle(AccommodationProductImported message, IMessageHandlerContext context)
        {
            _log.Info($"Product imported: {message.Id}");
            Data.HasImportedProduct = true;

            await Task.FromResult(true);
        }

        public async Task Handle(AccommodationCostImported message, IMessageHandlerContext context)
        {
            _log.Info($"Cost imported for product: {message.ProductId}");

            Data.HasImportedCosts = true;
            MarkAsComplete();

            await Task.FromResult(true);
        }
    }

    public class AccommodationCostImported : IEvent
    {
        public string ProductId { get; set; }
        public decimal Cost { get; set; }
    }

    public class AccommodationProductImported : IEvent
    {
        public string Id { get; set; }
    }

    public class ImportAccommodationSagaData : IContainSagaData
    {
        public virtual Guid Id { get; set; }
        public virtual string Originator { get; set; }
        public virtual string OriginalMessageId { get; set; }

        public virtual string AccommodationProductId { get; set; }

        public virtual bool HasImportedProduct { get; set; }
        public virtual bool HasImportedCosts { get; set; }
    }
}