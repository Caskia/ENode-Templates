using ECommon.Components;
using ECommon.Configurations;
using ECommon.Logging;
using ENode.AggregateSnapshot;
using ENode.Configurations;
using ENode.EventStore.MongoDb;
using ENode.Lock.Redis;
using Exceptionless;
using Jane;
using Jane.Configurations;
using Jane.Extensions;
using Jane.MongoDb.Serializers;
using Jane.Timing;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;
using JaneConfiguration = Jane.Configurations.Configuration;

namespace BoundedContext.ProcessorHost
{
    public class Bootstrap
    {
        private Assembly[] _bussinessAssemblies;
        private ENodeConfiguration _enodeConfiguration;
        private ILogger _logger;

        public Bootstrap()
        {
            Initialize();
        }

        public void Initialize()
        {
            InitializeENode();
        }

        public void Start()
        {
            try
            {
                _enodeConfiguration
                    .StartKafka()
                    .Start();
            }
            catch (Exception ex)
            {
                _logger.Error("Kafka start failed.", ex);
                throw;
            }
        }

        public void Stop()
        {
            try
            {
                _enodeConfiguration
                    .ShutdownKafka()
                    .Stop();
            }
            catch (Exception ex)
            {
                _logger.Error("Kafka stop failed.", ex);
                throw;
            }
        }

        private void InitializeENode()
        {
            _enodeConfiguration = InitializeJane()
                .CreateECommon()
                .CreateENode(new ConfigurationSetting()
                {
                    AggregateRootMaxInactiveSeconds = 3600 * 1,
                    EventMailBoxPersistenceMaxBatchSize = 500
                })
                .RegisterENodeComponents()
                .RegisterBusinessComponents(_bussinessAssemblies)
                .UseRedisLockService()
                .UseMongoDbEventStore()
                .UseMongoDbPublishedVersionStore()
                .UseMySqlAggregateSnapshotter()
                .UseKafka()
                .BuildENodeContainer()
                .InitializeBusinessAssemblies(_bussinessAssemblies);

            JaneConfiguration.Instance.CreateAutoMapperMappings();

            var enodeConfig = ObjectContainer.Resolve<Jane.ENode.IENodeConfiguration>();
            _enodeConfiguration
                .InitializeRedisLockService(new RedisOptions()
                {
                    ConnectionString = enodeConfig.LockServiceConnectionString,
                    DatabaseId = enodeConfig.LockServiceDatabaseId
                },
                "RedisLockPrefix")
                .InitializeMongoDbEventStore(new ENode.Configurations.MongoDbConfiguration()
                {
                    ConnectionString = enodeConfig.EventStoreConnectionString,
                    DatabaseName = enodeConfig.EventStoreDatabaseName
                },
                "BoundedContextEventStream")
                .InitializeMongoDbPublishedVersionStore(new ENode.Configurations.MongoDbConfiguration()
                {
                    ConnectionString = enodeConfig.EventStoreConnectionString,
                    DatabaseName = enodeConfig.EventStoreDatabaseName
                },
                "BoundedContextPublishedVersion")
                .InitializeMySqlAggregateSnapshotter(
                    enodeConfig.AggregateSnapshotConnectionString,
                    "BoundedContextAggregateSnapshot"
                );

            //Config Exceptionless
            if (JaneEnvironment.IsProduction() && !JaneConfiguration.Instance.Root["Exceptionless:ApiKey"].IsNullOrEmpty())
            {
                ExceptionlessClient.Default.Configuration.ApiKey = JaneConfiguration.Instance.Root["Exceptionless:ApiKey"];
                ExceptionlessClient.Default.Configuration.ServerUrl = JaneConfiguration.Instance.Root["Exceptionless:ServerUrl"];
            }

            _logger = ObjectContainer.Resolve<ILoggerFactory>().Create(typeof(Bootstrap).FullName);
            _logger.Info("ENode initialized.");
        }

        private JaneConfiguration InitializeJane()
        {
            _bussinessAssemblies = new[]
            {
                Assembly.Load("BoundedContext.CommandHandlers"),
                Assembly.Load("BoundedContext.Commands"),
                Assembly.Load("BoundedContext.Domain"),
                Assembly.Load("BoundedContext.MessagePublishers"),
                Assembly.Load("BoundedContext.Messages"),
                Assembly.Load("BoundedContext.ProcessManagers"),
                Assembly.Load("BoundedContext.QueryServices"),
                Assembly.Load("BoundedContext.ReadModel"),
                Assembly.Load("BoundedContext.Repositories.MongoDb"),
                Assembly.GetExecutingAssembly()
            };

            var autoMapperConfigurationAssemblies = new[]
            {
                Assembly.Load("BoundedContext.CommandHandlers"),
                Assembly.Load("BoundedContext.QueryServices"),
                Assembly.Load("BoundedContext.ReadModel")
            };

            return JaneConfiguration.Create()
                   .UseAutofac()
                   .RegisterCommonComponents()
                   .RegisterAssemblies(_bussinessAssemblies)
                   .UseLog4Net()
                   .UseClockProvider(ClockProviders.Utc)
                   .UseAutoMapper(autoMapperConfigurationAssemblies)
                   .UseMongoDb(() =>
                    {
                        var pack = new ConventionPack
                        {
                            new EnumRepresentationConvention(BsonType.String)
                        };
                        ConventionRegistry.Register("EnumStringConvention", pack, t => true);
                        BsonSerializer.RegisterSerializer(typeof(decimal), new DecimalSerializer(BsonType.Decimal128));
                        BsonSerializer.RegisterSerializer(typeof(decimal?), new NullableSerializer<decimal>(new DecimalSerializer(BsonType.Decimal128)));
                        BsonSerializer.RegisterSerializer(typeof(JObject), new JObjectSerializer());
                    })
                   .UseRedis()
                   .LoadKafkaConfiguration()
                   .LoadENodeConfiguration()
                   .RegisterUnhandledExceptionHandler();
        }
    }
}