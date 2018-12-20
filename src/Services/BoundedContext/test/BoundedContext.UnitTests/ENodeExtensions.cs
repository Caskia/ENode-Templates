﻿using BoundedContext.Topic;
using ECommon.Components;
using ENode.Commanding;
using ENode.Configurations;
using ENode.Eventing;
using ENode.Infrastructure;
using ENode.Kafka;
using ENode.Kafka.Consumers;
using ENode.Kafka.Producers;
using Jane.Utils;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace BoundedContext.UnitTests
{
    public static class ENodeExtensions
    {
        private static ApplicationMessageConsumer _applicationMessageConsumer;
        private static ApplicationMessagePublisher _applicationMessagePublisher;
        private static CommandConsumer _commandConsumer;
        private static CommandResultProcessor _commandResultProcessor;
        private static CommandService _commandService;
        private static DomainEventConsumer _eventConsumer;
        private static DomainEventPublisher _eventPublisher;
        private static PublishableExceptionConsumer _publishableExceptionConsumer;
        private static PublishableExceptionPublisher _publishableExceptionPublisher;

        public static ENodeConfiguration ShutdownKafka(this ENodeConfiguration enodeConfiguration)
        {
            _commandService.Shutdown();
            _eventPublisher.Shutdown();
            _applicationMessagePublisher.Shutdown();
            _publishableExceptionPublisher.Shutdown();

            _commandConsumer.Shutdown();
            _eventConsumer.Shutdown();
            _applicationMessageConsumer.Shutdown();
            _publishableExceptionConsumer.Shutdown();

            return enodeConfiguration;
        }

        public static ENodeConfiguration StartKafka(this ENodeConfiguration enodeConfiguration)
        {
            var kafkaConfig = ObjectContainer.Resolve<Jane.ENode.IKafkaConfiguration>();

            var producerSetting = new ProducerSetting
            {
                BrokerEndPoints = kafkaConfig.BrokerEndPoints
            };
            var commandConsumerSetting = new ConsumerSetting
            {
                BrokerEndPoints = kafkaConfig.BrokerEndPoints,
                GroupName = "BoundedContextCommandConsumerGroup"
            };
            var eventConsumerSetting = new ConsumerSetting
            {
                BrokerEndPoints = kafkaConfig.BrokerEndPoints,
                GroupName = "BoundedContextDomainEventConsumerGroup"
            };
            var applicationMessageConsumerSetting = new ConsumerSetting
            {
                BrokerEndPoints = kafkaConfig.BrokerEndPoints,
                GroupName = "BoundedContextApplicationMessageConsumerGroup"
            };
            var publishableExceptionConsumerSetting = new ConsumerSetting
            {
                BrokerEndPoints = kafkaConfig.BrokerEndPoints,
                GroupName = "BoundedContextPublishableExceptionConsumerGroup"
            };

            _commandConsumer = new CommandConsumer()
                .InitializeKafka(commandConsumerSetting)
                .Subscribe(new List<string>()
                {
                    BoundedContextTopics.BoundedContextDomainCommandTopic
                });
            _eventConsumer = new DomainEventConsumer()
                .InitializeKafka(eventConsumerSetting)
                .Subscribe(new List<string>()
                {
                    BoundedContextTopics.BoundedContextDomainDomainEventTopic
                });
            _applicationMessageConsumer = new ApplicationMessageConsumer()
                .InitializeKafka(applicationMessageConsumerSetting)
                .Subscribe(new List<string>()
                {
                    BoundedContextTopics.BoundedContextDomainApplicationMessageTopic
                });
            _publishableExceptionConsumer = new PublishableExceptionConsumer()
                .InitializeKafka(publishableExceptionConsumerSetting)
                .Subscribe(new List<string>()
                {
                    BoundedContextTopics.BoundedContextDomainExceptionTopic
                });

            _commandResultProcessor = new CommandResultProcessor()
                .Initialize(new IPEndPoint(SocketUtils.GetLocalIPV4(), 9003));
            _commandService.InitializeKafka(producerSetting, _commandResultProcessor);
            _eventPublisher.InitializeKafka(producerSetting);
            _applicationMessagePublisher.InitializeKafka(producerSetting);
            _publishableExceptionPublisher.InitializeKafka(producerSetting);

            _commandConsumer.Start();
            _eventConsumer.Start();
            _applicationMessageConsumer.Start();
            _publishableExceptionConsumer.Start();

            _commandResultProcessor.Start();
            _commandService.Start();
            _eventPublisher.Start();
            _applicationMessagePublisher.Start();
            _publishableExceptionPublisher.Start();

            return enodeConfiguration;
        }

        public static ENodeConfiguration UseKafka(this ENodeConfiguration enodeConfiguration)
        {
            var assemblies = new[] { Assembly.GetExecutingAssembly() };
            enodeConfiguration.RegisterTopicProviders(assemblies);

            var configuration = enodeConfiguration.GetCommonConfiguration();

            _commandService = new CommandService();
            configuration.SetDefault<ICommandService, CommandService>(_commandService);

            _eventPublisher = new DomainEventPublisher();
            configuration.SetDefault<IMessagePublisher<DomainEventStreamMessage>, DomainEventPublisher>(_eventPublisher);

            _applicationMessagePublisher = new ApplicationMessagePublisher();
            configuration.SetDefault<IMessagePublisher<IApplicationMessage>, ApplicationMessagePublisher>(_applicationMessagePublisher);

            _publishableExceptionPublisher = new PublishableExceptionPublisher();
            configuration.SetDefault<IMessagePublisher<IPublishableException>, PublishableExceptionPublisher>(_publishableExceptionPublisher);
            return enodeConfiguration;
        }
    }
}