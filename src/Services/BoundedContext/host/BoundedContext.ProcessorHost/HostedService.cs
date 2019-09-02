using ECommon.Logging;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using ENodeConfiguration = ENode.Configurations.ENodeConfiguration;

namespace BoundedContext.ProcessorHost
{
    internal class HostedService : IHostedService
    {
        private readonly ILogger _logger;

        public HostedService(
            ILoggerFactory loggerFactory
            )
        {
            _logger = loggerFactory.Create(typeof(HostedService).FullName);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                ENodeConfiguration.Instance
                    .StartKafka()
                    .Start();
            }
            catch (Exception ex)
            {
                _logger.Error("Kafka start failed.", ex);
                throw;
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                ENodeConfiguration.Instance
                    .ShutdownKafka()
                    .Stop();
            }
            catch (Exception ex)
            {
                _logger.Error("Kafka stop failed.", ex);
                throw;
            }

            return Task.CompletedTask;
        }
    }
}