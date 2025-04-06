using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Consumer.Models;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Consumer.Services
{
    public class TcpListenerService : BackgroundService
    {
        private readonly ILogger<TcpListenerService> _logger;
        private readonly ConfigService _configService;
        private readonly QueueManager _queueManager;
        private readonly List<TcpListener> _listeners = new List<TcpListener>();

        public TcpListenerService(
            ILogger<TcpListenerService> logger,
            ConfigService configService,
            QueueManager queueManager)
        {
            _logger = logger;
            _configService = configService;
            _queueManager = queueManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // Create TCP listeners for each consumer thread
                int basePort = _configService.BasePort;
                int consumerCount = _configService.ConsumerCount;

                _logger.LogInformation($"Starting {consumerCount} TCP listeners starting at port {basePort}");

                // Create and start listeners
                for (int i = 0; i < consumerCount; i++)
                {
                    int port = basePort + i;
                    var listener = new TcpListener(IPAddress.Any, port);
                    listener.Start();
                    _listeners.Add(listener);

                    _logger.LogInformation($"TCP listener started on port {port}");

                    // Start accepting connections for this listener
                    _ = AcceptConnectionsAsync(listener, i, stoppingToken);
                }

                // Keep the service running
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TCP listener service");
            }
            finally
            {
                // Stop all listeners
                foreach (var listener in _listeners)
                {
                    listener.Stop();
                }
                _logger.LogInformation("TCP listener service stopped");
            }
        }

        private async Task AcceptConnectionsAsync(TcpListener listener, int consumerId, CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation($"Consumer {consumerId + 1} waiting for connections...");
                    
                    // Accept client connection
                    TcpClient client = await listener.AcceptTcpClientAsync(stoppingToken);
                    
                    // Process the connection in a separate task
                    _ = ProcessClientAsync(client, consumerId, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error accepting connections for consumer {consumerId + 1}");
            }
        }

        private async Task ProcessClientAsync(TcpClient client, int consumerId, CancellationToken stoppingToken)
        {
            Console.WriteLine($"Consumer {consumerId + 1} received connection from {client.Client.RemoteEndPoint}");
            _logger.LogInformation($"Consumer {consumerId + 1} received connection from {client.Client.RemoteEndPoint}");
            
            using (client)
            using (NetworkStream stream = client.GetStream())
            using (BinaryReader reader = new BinaryReader(stream))
            {
                try
                {
                    // Read metadata length
                    int metadataLength = reader.ReadInt32();
                    
                    // Read metadata bytes
                    byte[] metadataBytes = reader.ReadBytes(metadataLength);
                    string metadataJson = Encoding.UTF8.GetString(metadataBytes);
                    
                    // Deserialize metadata
                    VideoMetadata metadata = JsonSerializer.Deserialize<VideoMetadata>(metadataJson);
                    
                    // Read video data length
                    int videoLength = reader.ReadInt32();
                    
                    // Read video data
                    byte[] videoData = reader.ReadBytes(videoLength);
                    
                    Console.WriteLine($"Consumer {consumerId + 1} received video: {metadata.FileName}, size: {videoData.Length / 1024} KB");
                    _logger.LogInformation($"Consumer {consumerId + 1} received video: {metadata.FileName}, size: {videoData.Length / 1024} KB");
                    
                    // Create video upload object
                    var videoUpload = new VideoUpload
                    {
                        Metadata = metadata,
                        VideoData = videoData
                    };
                    
                    // Try to add to queue (leaky bucket implementation)
                    Console.WriteLine($"Current queue size before enqueue attempt: {_queueManager.GetQueueCount()}/{_queueManager.GetMaxQueueSize()}");
                    if (!_queueManager.TryEnqueue(videoUpload))
                    {
                        string dropMessage = $"*** DROPPED VIDEO: Consumer {consumerId + 1} dropped video due to full queue: {metadata.FileName} ***";
                        Console.WriteLine(dropMessage);
                        _logger.LogWarning(dropMessage);
                    }
                    else
                    {
                        Console.WriteLine($"Video successfully added to queue: {metadata.FileName}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing client connection for consumer {consumerId + 1}: {ex.Message}");
                    _logger.LogError(ex, $"Error processing client connection for consumer {consumerId + 1}");
                }
            }
        }
    }
}
