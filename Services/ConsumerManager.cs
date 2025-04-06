using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Consumer.Models;
using Microsoft.Extensions.Logging;

namespace Consumer.Services
{
    public class ConsumerManager
    {
        private readonly ILogger<ConsumerManager> _logger;
        private readonly QueueManager _queueManager;
        private readonly VideoStorageService _videoStorageService;
        private readonly List<Task> _consumerTasks = new List<Task>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public ConsumerManager(
            ILogger<ConsumerManager> logger,
            QueueManager queueManager,
            VideoStorageService videoStorageService)
        {
            _logger = logger;
            _queueManager = queueManager;
            _videoStorageService = videoStorageService;
        }

        public void StartConsumers(int consumerCount, int queueLimit)
        {
            _logger.LogInformation($"Starting {consumerCount} consumer threads with queue limit {queueLimit}");
            
            // Set queue limit
            _queueManager.SetMaxQueueSize(queueLimit);
            
            // Start consumer threads
            for (int i = 0; i < consumerCount; i++)
            {
                int consumerId = i + 1;
                _logger.LogInformation($"Starting consumer thread {consumerId}");
                
                var task = Task.Run(() => RunConsumerAsync(consumerId, _cancellationTokenSource.Token));
                _consumerTasks.Add(task);
            }
        }

        private async Task RunConsumerAsync(int consumerId, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Consumer {consumerId} started");
            
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Try to dequeue a video
                    var videoUpload = await _queueManager.DequeueAsync(cancellationToken);
                    
                    if (videoUpload != null)
                    {
                        _logger.LogInformation($"Consumer {consumerId} processing video: {videoUpload.Metadata?.FileName ?? "Unknown"}");
                        
                        try
                        {
                            // Set the ThreadId to track which consumer processed this video
                            videoUpload.ThreadId = consumerId;
                            
                            // Process and save the video
                            await ProcessVideoAsync(consumerId, videoUpload, cancellationToken);
                            _logger.LogInformation($"Consumer {consumerId} completed processing video: {videoUpload.Metadata?.FileName ?? "Unknown"}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Consumer {consumerId} error processing video: {videoUpload.Metadata?.FileName ?? "Unknown"}");
                        }
                    }
                    else
                    {
                        // No videos in queue, wait a bit
                        await Task.Delay(100, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in consumer {consumerId}");
            }
            
            _logger.LogInformation($"Consumer {consumerId} stopped");
        }

        private async Task ProcessVideoAsync(int consumerId, VideoUpload videoUpload, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Consumer {consumerId} processing video: {videoUpload.Metadata.FileName}");

                // Simulate processing time (10 seconds) to allow queue to fill up
                await Task.Delay(10000, cancellationToken);
                
                // Save video
                await _videoStorageService.SaveVideoAsync(videoUpload);
                _logger.LogInformation($"Consumer {consumerId} completed processing video: {videoUpload.Metadata.FileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing video: {videoUpload.Metadata.FileName}");
            }
        }

        public void StopConsumers()
        {
            _logger.LogInformation("Stopping all consumer threads");
            _cancellationTokenSource.Cancel();
        }
    }
}
