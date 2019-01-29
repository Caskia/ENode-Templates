using ECommon.Components;
using ECommon.IO;
using ECommon.Logging;
using ENode.AggregateSnapshot;
using ENode.Commanding;
using ENode.Configurations;
using ENode.EventStore.MongoDb;
using ENode.Lock.Redis;
using Jane.Configurations;
using Jane.MongoDb.Serializers;
using Jane.Timing;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using JaneConfiguration = Jane.Configurations.Configuration;

namespace BoundedContext.UnitTests
{
    public abstract class BoundedContextTestBase : IDisposable
    {
        protected ICommandService _commandService;
        protected Jane.IIdGenerator _idGenerator;
        protected ILogger _logger;
        private Assembly[] _bussinessAssemblies;
        private ENodeConfiguration _enodeConfiguration;

        public BoundedContextTestBase()
        {
            Initialize();
            _idGenerator = ObjectContainer.Resolve<Jane.IIdGenerator>();
        }

        public void Dispose()
        {
            CleanupEnode();
        }

        protected Task<AsyncTaskResult<CommandResult>> ExecuteCommandAsync(ICommand command)
        {
            return _commandService.ExecuteAsync(command, CommandReturnType.EventHandled);
        }

        protected void Initialize()
        {
            if (_enodeConfiguration != null)
            {
                CleanupEnode();
            }

            InitializeENode();

            _commandService = ObjectContainer.Resolve<ICommandService>();
        }

        private void CleanupEnode()
        {
            Thread.Sleep(1000);
            _enodeConfiguration.ShutdownKafka();
            _enodeConfiguration.Stop();
            _logger.Info("ENode shutdown.");
        }

        private void InitializeENode()
        {
            _enodeConfiguration = InitializeJane()
               .CreateECommon()
               .CreateENode(new ConfigurationSetting())
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
            JaneConfiguration.Instance.RegisterBackgroundJob(_bussinessAssemblies);

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
                )
                .StartKafka()
                .Start();

            _logger = ObjectContainer.Resolve<ILoggerFactory>().Create(typeof(BoundedContextTestBase).FullName);
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
                Assembly.Load("BoundedContext.ReadModel"),
                Assembly.Load("BoundedContext.MessagePublishers")
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
                       try
                       {
                           BsonSerializer.RegisterSerializer(typeof(decimal), new DecimalSerializer(BsonType.Decimal128));
                           BsonSerializer.RegisterSerializer(typeof(decimal?), new NullableSerializer<decimal>(new DecimalSerializer(BsonType.Decimal128)));
                           BsonSerializer.RegisterSerializer(typeof(JObject), new JObjectSerializer());
                       }
                       catch { }
                   })
                   .LoadKafkaConfiguration()
                   .LoadENodeConfiguration()
                   .RegisterUnhandledExceptionHandler();
        }
    }
}