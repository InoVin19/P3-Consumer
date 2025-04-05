using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Consumer.Models;
using Microsoft.Extensions.Logging;

namespace Consumer.Services
{
    public class QueueManager
    {
        private readonly BlockingCollection<VideoUpload> _queue;
        private readonly ILogger<QueueManager> _logger;
        private readonly SemaphoreSlim _queueSemaphore;
        private int _maxQueueSize;

        public QueueManager(ILogger<QueueManager> logger)
        {
            _logger = logger;
            _maxQueueSize = 10; // Default, will be updated from config
            _queue = new BlockingCollection<VideoUpload>(_maxQueueSize);
            _queueSemaphore = new SemaphoreSlim(1, 1);
        }

        public void SetMaxQueueSize(int maxSize)
        {
            _maxQueueSize = maxSize;
            _logger.LogInformation($"Queue limit set to {maxSize}");
        }

        public bool TryEnqueue(VideoUpload videoUpload)
        {
            // Implement leaky bucket - if queue is full, drop the item
            if (_queue.Count >= _maxQueueSize)
            {
                _logger.LogWarning($"Queue full, dropping video: {videoUpload.Metadata.FileName}");
                return false;
            }

            try
            {
                _queue.Add(videoUpload);
                _logger.LogInformation($"Video added to queue: {videoUpload.Metadata.FileName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding video to queue: {videoUpload.Metadata.FileName}");
                return false;
            }
        }

        public async Task<VideoUpload> DequeueAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Try to take an item from the queue
                if (_queue.TryTake(out var videoUpload, 1000, cancellationToken))
                {
                    _logger.LogInformation($"Video dequeued for processing: {videoUpload.Metadata.FileName}");
                    return videoUpload;
                }
                
                return null;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dequeuing video");
                return null;
            }
        }

        public int GetQueueCount()
        {
            return _queue.Count;
        }

        public int GetMaxQueueSize()
        {
            return _maxQueueSize;
        }
    }
}
