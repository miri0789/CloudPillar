using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shared.Logger;

namespace CloudPillar.Agent
{
    public class AgentService :BackgroundService
    {
        private readonly ILoggerHandler _logger;

        public AgentService(ILoggerHandler logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
        _logger.Info("Worker is starting.");

        stoppingToken.Register(() => _logger.Info("Worker is stopping."));

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.Info("Worker is doing background work.");

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        _logger.Info("Worker has stopped.");

        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                 _logger.Info("Worker is starting at: {time}", DateTimeOffset.Now);

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
            _logger.Info("Worker is stopping.");

            // Your graceful shutdown logic here

            return base.StopAsync(cancellationToken);
        }

        // public override async Task PauseAsync(CancellationToken cancellationToken)
        // {
        //     _logger.Info("Worker is pausing.");

        //     // Your graceful pause logic here

        //     await base.PauseAsync(cancellationToken);
        // }

        // public override async Task ResumeAsync(CancellationToken cancellationToken)
        // {
        //     _logger.Info("Worker is resuming.");

        //     // Your graceful resume logic here

        //     await base.ResumeAsync(cancellationToken);
        // }
    }
}