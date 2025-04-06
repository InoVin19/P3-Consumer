using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Consumer.Models;
using Consumer.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Consumer.Services
{
    public class VideoStorageService
    {
        private readonly string _storagePath;
        private readonly ILogger<VideoStorageService> _logger;
        private readonly IHubContext<VideoHub> _hubContext;
        private readonly ConcurrentDictionary<string, VideoUpload> _videoCache;
        private readonly int _consumerCount;

        public VideoStorageService(
            ConfigService configService, 
            ILogger<VideoStorageService> logger,
            IHubContext<VideoHub> hubContext)
        {
            _storagePath = configService.VideoStoragePath;
            _consumerCount = configService.ConsumerCount;
            _logger = logger;
            _hubContext = hubContext;
            _videoCache = new ConcurrentDictionary<string, VideoUpload>();
            
            // Create storage directory if it doesn't exist
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
                _logger.LogInformation($"Created video storage directory: {_storagePath}");
            }
            
            // Create subdirectories for each consumer thread
            EnsureConsumerDirectories();
        }
        
        private void EnsureConsumerDirectories()
        {
            // Create subdirectories for each consumer thread based on the configured count
            for (int i = 1; i <= _consumerCount; i++)
            {
                string consumerPath = Path.Combine(_storagePath, $"Consumer{i}");
                if (!Directory.Exists(consumerPath))
                {
                    Directory.CreateDirectory(consumerPath);
                    _logger.LogInformation($"Created consumer directory: {consumerPath}");
                }
            }
        }

        public async Task<VideoUpload> SaveVideoAsync(VideoUpload videoUpload, int consumerId = 0, CancellationToken cancellationToken = default)
        {
            try
            {
                // Ensure the ThreadId is set and valid
                if (videoUpload.ThreadId < 1 || videoUpload.ThreadId > _consumerCount)
                {
                    videoUpload.ThreadId = 1; // Default to thread 1 if invalid
                    _logger.LogWarning($"Invalid ThreadId detected. Defaulting to 1 for video: {videoUpload.Id}");
                }
                
                // Get the consumer-specific directory
                string consumerPath = Path.Combine(_storagePath, $"Consumer{videoUpload.ThreadId}");
                
                // Ensure the directory exists
                if (!Directory.Exists(consumerPath))
                {
                    Directory.CreateDirectory(consumerPath);
                    _logger.LogInformation($"Created consumer directory on-demand: {consumerPath}");
                }
                
                // Generate unique filename
                string fileName = $"{videoUpload.Id}_{videoUpload.Metadata?.FileName ?? "unknown"}";
                string filePath = Path.Combine(consumerPath, fileName);
                
                // Save video data to file
                await File.WriteAllBytesAsync(filePath, videoUpload.VideoData, cancellationToken);
                
                // Update video upload with storage path
                videoUpload.StoragePath = filePath;
                videoUpload.IsProcessed = true;
                
                // Add to cache
                _videoCache.TryAdd(videoUpload.Id.ToString(), videoUpload);
                
                // Notify clients of new video
                await _hubContext.Clients.All.SendAsync("VideoUploaded", new
                {
                    Id = videoUpload.Id,
                    FileName = videoUpload.Metadata?.FileName,
                    FileSize = videoUpload.Metadata?.FileSize,
                    ContentType = videoUpload.Metadata?.ContentType,
                    UploadTime = videoUpload.UploadTime,
                    ThreadId = videoUpload.ThreadId
                });
                
                _logger.LogInformation($"Video saved: {videoUpload.Metadata?.FileName ?? "unknown"} by Consumer {videoUpload.ThreadId}");
                return videoUpload;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving video: {videoUpload.Metadata?.FileName ?? "unknown"}");
                throw;
            }
        }

        public async Task<byte[]> GetVideoDataAsync(string videoId)
        {
            try
            {
                if (_videoCache.TryGetValue(videoId, out var videoUpload))
                {
                    if (File.Exists(videoUpload.StoragePath))
                    {
                        return await File.ReadAllBytesAsync(videoUpload.StoragePath);
                    }
                }
                
                _logger.LogWarning($"Video not found: {videoId}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving video: {videoId}");
                return null;
            }
        }

        public List<VideoUpload> GetAllVideos()
        {
            return _videoCache.Values.ToList();
        }
    }
}
