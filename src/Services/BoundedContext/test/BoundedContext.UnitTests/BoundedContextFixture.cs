using Autofac;
using Autofac.Extensions.DependencyInjection;
using ECommon.Components;
using ENode.AggregateSnapshot;
using ENode.Configurations;
using ENode.EventStore.MongoDb;
using ENode.Lock.Redis;
using Jane.Configurations;
using Jane.ENode;
using Jane.MongoDb.Serializers;
using Jane.Timing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using ENodeConfiguration = ENode.Configurations.ENodeConfiguration;
using JaneConfiguration = Jane.Configurations.Configuration;

namespace BoundedContext.UnitTests
{
    public class BoundedContextFixture : IDisposable
    {
        private Assembly[] _bussinessAssemblies;

        public BoundedContextFixture()
        {
            if (ENodeConfiguration.Instance != null)
            {
                CleanupEnode();
            }

            InitializeENode();
        }

        public void Dispose()
        {
            CleanupEnode();
        }

        private void CleanupEnode()
        {
            Thread.Sleep(1000);
            ENodeConfiguration.Instance.ShutdownKafka();
            ENodeConfiguration.Instance.Stop();
        }

        private void ConfigureContainer(ContainerBuilder builder)
        {
            _bussinessAssemblies = new[]
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
                Assembly.Load("BoundedContext.ReadModel"),
                Assembly.Load("BoundedContext.MessagePublishers")
            };

            JaneConfiguration.Instance
                   .UseAutofac(builder)
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
                           BsonSerializer.RegisterSerializer(typeof(Dictionary<string, object>), new ObjectDictionarySerializer());
                           BsonSerializer.RegisterSerializer(typeof(decimal), new DecimalSerializer(BsonType.Decimal128));
                           BsonSerializer.RegisterSerializer(typeof(decimal?), new NullableSerializer<decimal>(new DecimalSerializer(BsonType.Decimal128)));
                       }
                       catch { }
                   })
                   .LoadKafkaConfiguration()
                   .LoadENodeConfiguration()
                   .RegisterUnhandledExceptionHandler()
                   .CreateECommon(builder)
                   .CreateENode(new ConfigurationSetting())
                   .RegisterENodeComponents()
                   .RegisterBusinessComponents(_bussinessAssemblies)
                   .UseRedisLockService()
                   .UseMongoDbEventStore()
                   .UseMongoDbPublishedVersionStore()
                   .UseMySqlAggregateSnapshotter()
                   .UseKafka();
        }

        private void ConfigureService(IServiceCollection services)
        {
        }

        private void InitializeENode()
        {
            JaneConfiguration.Create();

            var services = new ServiceCollection();
            ConfigureService(services);

            var serviceProviderFactory = new AutofacServiceProviderFactory();
            var containerBuilder = serviceProviderFactory.CreateBuilder(services);

            ConfigureContainer(containerBuilder);

            var serviceProvider = serviceProviderFactory.CreateServiceProvider(containerBuilder);
            serviceProvider.PopulateJaneENodeDIContainer();

            JaneConfiguration.Instance.CreateAutoMapperMappings();
            JaneConfiguration.Instance.RegisterBackgroundJob(_bussinessAssemblies);

            ENodeConfiguration.Instance.InitializeBusinessAssemblies(_bussinessAssemblies);

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
                )
                .StartKafka()
                .Start();
        }
    }
}