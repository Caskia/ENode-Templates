using ECommon.Components;
using ENode.Kafka;
using ENode.Messaging;

namespace BoundedContext.UnitTests.TopicProviders
{
    [Component]
    public class ApplicationMessageTopicProvider : AbstractTopicProvider<IApplicationMessage>
    {
        public ApplicationMessageTopicProvider()
        {
        }
    }
}