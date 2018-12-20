﻿using ECommon.Components;
using ENode.Infrastructure;
using ENode.Kafka;

namespace BoundedContext.ProcessorHost.TopicProviders
{
    [Component]
    public class ExceptionTopicProvider : AbstractTopicProvider<IPublishableException>
    {
        public ExceptionTopicProvider()
        {
        }
    }
}