using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CloudPillar.Agent.Handlers;
using Shared.Logger;

namespace CloudPillar.Agent.Sevices
{
    public class AgentService :BackgroundService
    {
        private readonly ILoggerHandler _logger;
        private readonly IStateMachineHandler _stateMachineHandler;

        public AgentService(ILoggerHandler logger, IStateMachineHandler stateMachineHandler)
        {
        _stateMachineHandler = stateMachineHandler ?? throw new ArgumentNullException(nameof(stateMachineHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Info("CloudPilar.Agent is starting.");

            stoppingToken.Register(() => _logger.Info("CloudPilar.Agent is stopping."));

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.Info("CloudPilar.Agent is doing background work.");

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }

            _logger.Info("CloudPilar.Agent has stopped.");
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                 _logger.Info("CloudPilar.Agent is starting at: {time}", DateTimeOffset.Now);

            // Your setup logic here

            return base.StartAsync(cancellationToken);
            }
            catch (System.Exception)
            {
                
                throw;
            }
           
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Info("CloudPilar.Agent is stopping.");

            // Your graceful shutdown logic here

            return base.StopAsync(cancellationToken);
        }
    }
}