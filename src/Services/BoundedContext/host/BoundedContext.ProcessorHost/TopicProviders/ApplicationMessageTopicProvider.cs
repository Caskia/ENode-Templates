using ECommon.Components;
using ENode.Kafka;
using ENode.Messaging;

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