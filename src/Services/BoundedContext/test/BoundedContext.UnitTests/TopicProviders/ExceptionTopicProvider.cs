using ECommon.Components;
using ENode.Infrastructure;
using ENode.Kafka;

namespace BoundedContext.UnitTests.TopicProviders
{
    [Component]
    public class ExceptionTopicProvider : AbstractTopicProvider<IPublishableException>
    {
        public ExceptionTopicProvider()
        {
        }
    }
}