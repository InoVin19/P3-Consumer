using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Consumer.Hubs;
using Consumer.Models;
using Microsoft.Extensions.Logging;

namespace Consumer.Services
{
    public class QueueManager
    {
        private ConcurrentQueue<VideoUpload> _queue;
        private readonly ILogger<QueueManager> _logger;
        private readonly SemaphoreSlim _queueSemaphore;
        private int _maxQueueSize;

        public QueueManager(ILogger<QueueManager> logger)
        {
            _logger = logger;
            _maxQueueSize = 10; // Default, will be updated from config
            _queue = new ConcurrentQueue<VideoUpload>();
            _queueSemaphore = new SemaphoreSlim(1, 1);
            
            LogMessage("QueueManager initialized with default queue limit of 10", LogLevel.Information);
        }

        public void SetMaxQueueSize(int maxSize)
        {
            _maxQueueSize = maxSize;
            LogMessage($"*** QUEUE LIMIT EXPLICITLY SET TO {maxSize} ***", LogLevel.Information);
            
            // Send initial queue update
            _ = VideoHub.SendQueueUpdate(0, _maxQueueSize);
        }

        // Helper method to log both to console and logger
        private void LogMessage(string message, LogLevel level)
        {
            Console.WriteLine(message);
            
            switch (level)
            {
                case LogLevel.Information:
                    _logger.LogInformation(message);
                    break;
                case LogLevel.Warning:
                    _logger.LogWarning(message);
                    break;
                case LogLevel.Error:
                    _logger.LogError(message);
                    break;
                default:
                    _logger.LogInformation(message);
                    break;
            }
        }

        public bool TryEnqueue(VideoUpload videoUpload)
        {
            try
            {
                // Acquire semaphore to ensure thread safety when checking queue size
                _queueSemaphore.Wait();
                
                try
                {
                    // Check current queue size against limit
                    int currentCount = _queue.Count;
                    LogMessage($"Current queue size: {currentCount}/{_maxQueueSize}", LogLevel.Information);
                    
                    // Implement leaky bucket - if queue is full, drop the item
                    if (currentCount >= _maxQueueSize)
                    {
                        LogMessage($"*** QUEUE FULL: {currentCount}/{_maxQueueSize} - Dropping video: {videoUpload.Metadata.FileName} ***", LogLevel.Warning);
                        
                        // Send notification that video was dropped
                        _ = VideoHub.SendVideoDropped(videoUpload.Metadata.FileName);
                        
                        return false;
                    }
                    
                    // Add to queue
                    _queue.Enqueue(videoUpload);
                    LogMessage($"Video added to queue: {videoUpload.Metadata.FileName}, new queue size: {_queue.Count}/{_maxQueueSize}", LogLevel.Information);
                    
                    // Send queue update notification
                    _ = VideoHub.SendQueueUpdate(_queue.Count, _maxQueueSize);
                    
                    return true;
                }
                finally
                {
                    // Release semaphore
                    _queueSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error adding video to queue: {videoUpload.Metadata.FileName}";
                Console.WriteLine($"{errorMessage}: {ex.Message}");
                _logger.LogError(ex, errorMessage);
                return false;
            }
        }

        public async Task<VideoUpload> DequeueAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Try to dequeue an item
                while (!cancellationToken.IsCancellationRequested)
                {
                    bool semaphoreAcquired = false;
                    try
                    {
                        // Acquire semaphore to ensure thread safety
                        await _queueSemaphore.WaitAsync(cancellationToken);
                        semaphoreAcquired = true;
                        
                        // Check if queue is empty
                        if (_queue.IsEmpty)
                        {
                            // Release semaphore and wait before trying again
                            _queueSemaphore.Release();
                            semaphoreAcquired = false;
                            await Task.Delay(1000, cancellationToken);
                            continue;
                        }
                        
                        // Try to dequeue
                        if (_queue.TryDequeue(out var videoUpload))
                        {
                            LogMessage($"Video dequeued for processing: {videoUpload.Metadata.FileName}, new queue size: {_queue.Count}/{_maxQueueSize}", LogLevel.Information);
                            
                            // Send queue update notification
                            _ = VideoHub.SendQueueUpdate(_queue.Count, _maxQueueSize);
                            
                            // Release semaphore before returning
                            _queueSemaphore.Release();
                            semaphoreAcquired = false;
                            
                            return videoUpload;
                        }
                    }
                    finally
                    {
                        // Release semaphore if still acquired
                        if (semaphoreAcquired)
                        {
                            _queueSemaphore.Release();
                        }
                    }
                }
                
                return null;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                string errorMessage = "Error dequeuing video";
                Console.WriteLine($"{errorMessage}: {ex.Message}");
                _logger.LogError(ex, errorMessage);
                return null;
            }
        }

        public int GetQueueCount()
        {
            return _queue.Count;
        }

        public int GetMaxQueueSize()
        {
            Console.WriteLine($"GetMaxQueueSize called, returning: {_maxQueueSize}");
            return _maxQueueSize;
        }
    }
}
