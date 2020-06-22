using ECommon.Components;
using ENode.Domain;
using ENode.Kafka;

namespace BoundedContext.UnitTests.TopicProviders
{
    [Component]
    public class DomainExceptionTopicProvider : AbstractTopicProvider<IDomainException>
    {
        public DomainExceptionTopicProvider()
        {
        }
    }
}