using BoundedContext.Topic;
using ECommon.Components;
using ENode.Eventing;
using ENode.Kafka;

namespace BoundedContext.ProcessorHost.TopicProviders
{
    [Component]
    public class DomainEventTopicProvider : AbstractTopicProvider<IDomainEvent>
    {
        public DomainEventTopicProvider()
        {
        }
    }
}