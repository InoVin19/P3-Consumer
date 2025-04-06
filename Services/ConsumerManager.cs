using System;
using System.Threading;
using System.Threading.Tasks;
using Consumer.Hubs;
using Consumer.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Consumer.Services
{
    public class ConsumerManager : BackgroundService
    {
        private readonly ILogger<ConsumerManager> _logger;
        private readonly QueueManager _queueManager;
        private readonly VideoStorageService _videoStorageService;
        private readonly ConfigService _configService;
        private Task[] _consumerTasks;

        public ConsumerManager(
            ILogger<ConsumerManager> logger,
            QueueManager queueManager,
            VideoStorageService videoStorageService,
            ConfigService configService)
        {
            _logger = logger;
            _queueManager = queueManager;
            _videoStorageService = videoStorageService;
            _configService = configService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // Get configuration
                int consumerCount = _configService.ConsumerCount;
                int queueLimit = _configService.QueueLimit;

                // Set queue limit
                _queueManager.SetMaxQueueSize(queueLimit);

                // Start consumer threads
                await StartConsumers(consumerCount, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in consumer manager");
            }
        }

        private async Task StartConsumers(int consumerCount, CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Starting {consumerCount} consumer threads with queue limit {_configService.QueueLimit}");

            _consumerTasks = new Task[consumerCount];

            for (int i = 0; i < consumerCount; i++)
            {
                int consumerId = i + 1;
                _logger.LogInformation($"Starting consumer thread {consumerId}");

                _consumerTasks[i] = Task.Run(async () =>
                {
                    try
                    {
                        await RunConsumerAsync(consumerId, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // Normal shutdown
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error in consumer {consumerId}");
                    }
                }, stoppingToken);
            }

            // Wait for all consumers to complete
            await Task.WhenAll(_consumerTasks);
        }

        private async Task RunConsumerAsync(int consumerId, CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Consumer {consumerId} started");

            while (!stoppingToken.IsCancellationRequested)
            {
                // Dequeue video from queue
                var videoUpload = await _queueManager.DequeueAsync(stoppingToken);

                if (videoUpload != null)
                {
                    // Set the ThreadId to track which consumer processed this video
                    videoUpload.ThreadId = consumerId;
                    
                    // Process video
                    await ProcessVideoAsync(consumerId, videoUpload, stoppingToken);
                }
            }
        }

        private async Task ProcessVideoAsync(int consumerId, VideoUpload videoUpload, CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation($"Consumer {consumerId} processing video: {videoUpload.Metadata.FileName}");

                // Simulate processing time (10 seconds) to allow queue to fill up
                await Task.Delay(10000, stoppingToken);
                
                // Set processed time
                videoUpload.ProcessedTime = DateTime.UtcNow;
                
                // Save video
                await _videoStorageService.SaveVideoAsync(videoUpload, consumerId, stoppingToken);

                _logger.LogInformation($"Consumer {consumerId} completed processing video: {videoUpload.Metadata.FileName}");
                
                // Send notification that video was processed
                await VideoHub.SendVideoProcessed(videoUpload.Metadata.FileName, consumerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing video: {videoUpload.Metadata.FileName}");
            }
        }
    }
}
