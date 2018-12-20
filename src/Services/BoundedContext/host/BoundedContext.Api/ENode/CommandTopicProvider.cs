using ECommon.Components;
using ENode.Commanding;
using ENode.Kafka;

namespace BoundedContext.Api
{
    [Component]
    public class CommandTopicProvider : AbstractTopicProvider<ICommand>
    {
        public CommandTopicProvider()
        {
        }
    }
}