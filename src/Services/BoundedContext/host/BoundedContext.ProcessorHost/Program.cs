using Autofac;
using Autofac.Extensions.DependencyInjection;
using ECommon.Components;
using ECommon.Configurations;
using ENode.AggregateSnapshot;
using ENode.Configurations;
using ENode.EventStore.MongoDb;
using ENode.Lock.Redis;
using Exceptionless;
using Jane;
using Jane.Configurations;
using Jane.ENode;
using Jane.Extensions;
using Jane.MongoDb.Serializers;
using Jane.Timing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using ENodeConfiguration = ENode.Configurations.ENodeConfiguration;
using JaneConfiguration = Jane.Configurations.Configuration;

namespace BoundedContext.ProcessorHost
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var bussinessAssemblies = new[]
            {
                Assembly.Load("BoundedContext.CommandHandlers"),
                Assembly.Load("BoundedContext.Commands"),
                Assembly.Load("BoundedContext.Domain"),
                Assembly.Load("BoundedContext.Messages"),
                Assembly.Load("BoundedContext.Repositories.MongoDb"),
                Assembly.Load("BoundedContext.ReadModel"),
                Assembly.Load("BoundedContext.QueryServices"),
                Assembly.Load("BoundedContext.ProcessManagers"),
                Assembly.Load("BoundedContext.MessagePublishers"),
                Assembly.Load("BoundedContext.BackgroundJobs"),
                Assembly.GetExecutingAssembly()
            };

            var autoMapperConfigurationAssemblies = new[]
            {
                Assembly.Load("BoundedContext.CommandHandlers"),
                Assembly.Load("BoundedContext.QueryServices"),
                Assembly.Load("BoundedContext.ReadModel")
            };

            var host = new HostBuilder()
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureServices(services =>
                {
                    JaneConfiguration.Create();

                    services.AddHostedService<HostedService>();
                })
                .ConfigureContainer<ContainerBuilder>((context, builder) =>
                {
                    JaneConfiguration.Instance
                        .UseAutofac(builder)
                        .RegisterCommonComponents()
                        .RegisterAssemblies(bussinessAssemblies)
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
                            BsonSerializer.RegisterSerializer(typeof(Dictionary<string, object>), new ObjectDictionarySerializer());
                            BsonSerializer.RegisterSerializer(typeof(decimal), new DecimalSerializer(BsonType.Decimal128));
                            BsonSerializer.RegisterSerializer(typeof(decimal?), new NullableSerializer<decimal>(new DecimalSerializer(BsonType.Decimal128)));
                        })
                        .UseRedis()
                        .LoadKafkaConfiguration()
                        .LoadENodeConfiguration()
                        .RegisterUnhandledExceptionHandler()
                        .CreateECommon(builder)
                        .CreateENode(new ConfigurationSetting()
                        {
                            AggregateRootMaxInactiveSeconds = 3600 * 1,
                            EventMailBoxPersistenceMaxBatchSize = 500
                        })
                        .RegisterENodeComponents()
                        .RegisterBusinessComponents(bussinessAssemblies)
                        .UseRedisLockService()
                        .UseMongoDbEventStore()
                        .UseMongoDbPublishedVersionStore()
                        .UseMySqlAggregateSnapshotter()
                        .UseKafka();
                })
                .Build();

            host.Services.PopulateJaneENodeDIContainer();

            JaneConfiguration.Instance.CreateAutoMapperMappings();

            ENodeConfiguration.Instance
                .InitializeBusinessAssemblies(bussinessAssemblies);

            var enodeConfig = ObjectContainer.Resolve<IENodeConfiguration>();
            ENodeConfiguration.Instance
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

            using (host)
            {
                await host.StartAsync();

                await host.WaitForShutdownAsync();
            }
        }
    }
}