using ECommon.Components;
using ENode.Commanding;
using ENode.Kafka;

namespace BoundedContext.ProcessorHost.TopicProviders
{
    [Component]
    public class CommandTopicProvider : AbstractTopicProvider<ICommand>
    {
        public CommandTopicProvider()
        {
        }
    }
}