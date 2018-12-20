using ECommon.Components;
using ENode.Eventing;
using ENode.Kafka;

namespace BoundedContext.UnitTests.TopicProviders
{
    [Component]
    public class DomainEventTopicProvider : AbstractTopicProvider<IDomainEvent>
    {
        public DomainEventTopicProvider()
        {
        }
    }
}