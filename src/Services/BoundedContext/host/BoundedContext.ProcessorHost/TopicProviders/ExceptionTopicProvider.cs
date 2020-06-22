using ECommon.Components;
using ENode.Domain;
using ENode.Kafka;

namespace BoundedContext.ProcessorHost.TopicProviders
{
    [Component]
    public class ExceptionTopicProvider : AbstractTopicProvider<IDomainException>
    {
        public ExceptionTopicProvider()
        {
        }
    }
}