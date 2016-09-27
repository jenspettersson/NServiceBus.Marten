using System;

namespace NServiceBus.Marten.Sagas
{
    public class SagaDocument
    {
        public Guid Id { get; set; }
        public IContainSagaData SagaData { get; set; }
        public string CorrelationProperty { get; set; }
        public string CorrelationPropertyValue { get; set; }
    }
}