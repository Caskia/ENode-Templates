using ECommon.Components;
using ENode.Commanding;
using ENode.Configurations;
using ENode.Kafka;
using ENode.Kafka.Producers;
using Jane.Utils;
using System.Net;
using System.Reflection;

namespace BoundedContext.Api
{
    public static class ENodeExtensions
    {
        private static CommandResultProcessor _commandResultProcessor;
        private static CommandService _commandService;

        public static ENodeConfiguration ShutdownKafka(this ENodeConfiguration enodeConfiguration)
        {
            _commandService.Shutdown();
            _commandResultProcessor.Shutdown();

            return enodeConfiguration;
        }

        public static ENodeConfiguration StartKafka(this ENodeConfiguration enodeConfiguration)
        {
            var kafkaConfig = ObjectContainer.Resolve<Jane.ENode.IKafkaConfiguration>();

            var producerSetting = new ProducerSetting
            {
                BrokerEndPoints = kafkaConfig.BrokerEndPoints
            };

            _commandResultProcessor = new CommandResultProcessor()
                .Initialize(new IPEndPoint(SocketUtils.GetLocalIPV4(), 9003));
            _commandService.InitializeKafka(producerSetting, _commandResultProcessor);

            _commandResultProcessor.Start();
            _commandService.Start();

            return enodeConfiguration;
        }

        public static ENodeConfiguration UseKafka(this ENodeConfiguration enodeConfiguration)
        {
            var assemblies = new[] { Assembly.GetExecutingAssembly() };
            enodeConfiguration.RegisterTopicProviders(assemblies);

            var configuration = enodeConfiguration.GetCommonConfiguration();

            _commandService = new CommandService();
            configuration.SetDefault<ICommandService, CommandService>(_commandService);

            return enodeConfiguration;
        }
    }
}