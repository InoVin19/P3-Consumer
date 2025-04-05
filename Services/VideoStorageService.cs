using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public VideoStorageService(
            ConfigService configService, 
            ILogger<VideoStorageService> logger,
            IHubContext<VideoHub> hubContext)
        {
            _storagePath = configService.VideoStoragePath;
            _logger = logger;
            _hubContext = hubContext;
            _videoCache = new ConcurrentDictionary<string, VideoUpload>();
            
            // Create storage directory if it doesn't exist
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
                _logger.LogInformation($"Created video storage directory: {_storagePath}");
            }
        }

        public async Task<VideoUpload> SaveVideoAsync(VideoUpload videoUpload)
        {
            try
            {
                // Generate unique filename
                string fileName = $"{videoUpload.Id}_{videoUpload.Metadata.FileName}";
                string filePath = Path.Combine(_storagePath, fileName);
                
                // Save video data to file
                await File.WriteAllBytesAsync(filePath, videoUpload.VideoData);
                
                // Update video upload with storage path
                videoUpload.StoragePath = filePath;
                videoUpload.IsProcessed = true;
                
                // Add to cache
                _videoCache.TryAdd(videoUpload.Id.ToString(), videoUpload);
                
                // Calculate a thread ID based on the hash of the GUID (1 or 2)
                int threadId = (videoUpload.Id.GetHashCode() % 2) + 1;
                
                // Notify clients of new video
                await _hubContext.Clients.All.SendAsync("VideoUploaded", new
                {
                    Id = videoUpload.Id,
                    FileName = videoUpload.Metadata.FileName,
                    FileSize = videoUpload.Metadata.FileSize,
                    ContentType = videoUpload.Metadata.ContentType,
                    UploadTime = videoUpload.UploadTime,
                    ThreadId = threadId
                });
                
                _logger.LogInformation($"Video saved: {videoUpload.Metadata.FileName}");
                return videoUpload;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving video: {videoUpload.Metadata.FileName}");
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
