using ECommon.Components;
using ENode.Infrastructure;
using ENode.Kafka;

namespace BoundedContext.ProcessorHost.TopicProviders
{
    [Component]
    public class ApplicationMessageTopicProvider : AbstractTopicProvider<IApplicationMessage>
    {
        public ApplicationMessageTopicProvider()
        {
        }
    }
}